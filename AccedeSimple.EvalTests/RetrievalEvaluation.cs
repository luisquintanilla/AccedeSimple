using AccedeSimple.Service.Services;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.VectorData;
using OpenAI.Chat;


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
            var IngestionService = serviceProvider.GetRequiredService<IngestionService>();
            await IngestionService.IngestAsync(Path.Combine(AppContext.BaseDirectory, "docs"));

            var searchService = serviceProvider.GetRequiredService<SearchService>();

            var userQuery = "What expenses cannot be reimbursed?";

            List<Document> results = [];
            await foreach (var result in searchService.SearchAsync(userQuery))
            {
                results.Add(result);
            }

            var chatConfig = serviceProvider.GetRequiredService<ChatConfiguration>();

            var retrievalEvaluator = new RetrievalEvaluator();
            var evalResult = await retrievalEvaluator.EvaluateAsync(
                new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, userQuery), 
                new ChatResponse(), 
                chatConfig, 
                [new RetrievalEvaluatorContext(results.Where(r => r.Text is not null).Select(r => r.Text!))]
            );

            var retrievalMetric = evalResult.Get<NumericMetric>(RetrievalEvaluator.RetrievalMetricName);
            Assert.IsNotNull(retrievalMetric);
            Assert.IsNotNull(retrievalMetric.Interpretation);
            Assert.IsFalse(retrievalMetric.Interpretation.Failed);
        }
    }
}
