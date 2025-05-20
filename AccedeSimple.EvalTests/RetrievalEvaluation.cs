using AccedeSimple.Service.Services;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using System.Diagnostics;


namespace AccedeSimple.EvalTests
{


    [TestClass]
    public sealed class RetrievalEvaluation
    {
        static IServiceProvider serviceProvider;

        static RetrievalEvaluation()
        {
            var services = new ServiceCollection();

            var builder = new ConfigurationBuilder()
                .AddUserSecrets<RetrievalEvaluation>()
                .Build();

            var credential = new ChainedTokenCredential(new AzureCliCredential(), new DefaultAzureCredential());

            services.AddLogging();

            services.TryAddSingleton(sp =>
            {
                var endpoint = builder["AzureOpenAI:Endpoint"] ?? throw new Exception("AzureOpenAI:Endpoint not specified");
                return new AzureOpenAIClient(
                    new Uri(endpoint),
                    credential);
            });

            services.TryAddSingleton<IChatClient>(sp =>
            {
                var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
                return azureOpenAIClient.GetChatClient("gpt-4o-mini").AsIChatClient();
            });

            services.TryAddSingleton(sp =>
            {
                var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
                var chatClient = azureOpenAIClient.GetChatClient("gpt-4o").AsIChatClient();
                return new ChatConfiguration(chatClient);
            });

            services.AddEmbeddingGenerator(modelName: "text-embedding-3-small");
            services.AddSqliteCollection<int, Document>("Documents", "Data Source=documents.db");
            services.AddTransient<SearchService>();
            services.AddTransient<IngestionService>();

            serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public async Task TestRetrieval()
        {
            // Build the vector store and ingest the documents
            var IngestionService = serviceProvider.GetRequiredService<IngestionService>();
            await IngestionService.IngestAsync(Path.Combine(AppContext.BaseDirectory, "docs"));

            var searchService = serviceProvider.GetRequiredService<SearchService>();

            // Search the vector store for relevant chunks
            var userQuery = "What expenses cannot be reimbursed?";

            List<Document> docs = [];
            await foreach (var doc in searchService.SearchAsync(userQuery))
            {
                docs.Add(doc);
            }

            // Run the retrieval evaluation on the results
            var chatConfig = serviceProvider.GetRequiredService<ChatConfiguration>();

            var reportStorePath = Path.Combine(AppContext.BaseDirectory, "EvalData");
            if (!Directory.Exists(reportStorePath))
            {
                Directory.CreateDirectory(reportStorePath);
            }

            var retrievalEvaluator = new RetrievalEvaluator();
            var reportStore = DiskBasedReportingConfiguration.Create(
                reportStorePath,
                [new RetrievalEvaluator()],
                chatConfig,
                enableResponseCaching: false);

            var scenarioRun = await reportStore.CreateScenarioRunAsync("RAG Chunk Retrieval", "Reimbursable Expenses");

            var evalResult = await scenarioRun.EvaluateAsync(
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userQuery),
                new ChatResponse(),
                [new RetrievalEvaluatorContext(docs.Select(r => r.Text!))]
            );

            // flush results to disk
            await scenarioRun.DisposeAsync();

            // check some of the results
            var retrievalMetric = evalResult.Get<NumericMetric>(RetrievalEvaluator.RetrievalMetricName);
            Assert.IsNotNull(retrievalMetric);
            Assert.IsNotNull(retrievalMetric.Interpretation);
            Assert.IsFalse(retrievalMetric.Interpretation.Failed);

            // generate Report
            var resultStore = reportStore.ResultStore;
            List<ScenarioRunResult> results = [];

            await foreach (string executionName in
                resultStore.GetLatestExecutionNamesAsync(1).ConfigureAwait(false))
            {
                await foreach (ScenarioRunResult result in
                    resultStore.ReadResultsAsync(executionName).ConfigureAwait(false))
                {
                    results.Add(result);
                }
            }

            var reportFile = Path.Combine(AppContext.BaseDirectory, "eval-report.html");
            var reportWriter = new HtmlReportWriter(reportFile);
            await reportWriter.WriteReportAsync(results);

            // Open the generated report in the default browser.
            _ = Process.Start(
                new ProcessStartInfo
                {
                    FileName = reportFile,
                    UseShellExecute = true
                });
        }

    }
}
