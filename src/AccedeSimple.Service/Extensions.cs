#pragma warning disable
using Azure.Identity;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using AccedeSimple.Service.ProcessSteps;
using Microsoft.SemanticKernel;
using AccedeSimple.Domain;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

public static class Extensions
{


    // public static void MapEndpoints(this WebApplication app, string basePath, Action<RouteGroupBuilder> configure)
    // {
    //     var group = app.MapGroup(basePath);
    //     configure(group);
    // }

    public static IServiceCollection AddTravelProcess(
        this IServiceCollection services)
    {
        // Add process steps
        services.AddTransient<TravelPlanningStep>();
        services.AddTransient<ApprovalStep>();
        services.AddTransient<ReceiptProcessingStep>();
        services.AddTransient<ExpenseReportStep>();

        // Configure workflows
        services.AddTransient<KernelProcess>(sp =>
        {
            var processBuilder = new ProcessBuilder("TravelProcess");

            // Define steps
            var planStep = processBuilder.AddStepFromType<TravelPlanningStep>("TravelPlanningStep");
            var approvalStep = processBuilder.AddStepFromType<ApprovalStep>("ApprovalStep");
            var receiptStep = processBuilder.AddStepFromType<ReceiptProcessingStep>("ReceiptProcessingStep");
            var expenseReportStep = processBuilder.AddStepFromType<ExpenseReportStep>("ExpenseReportStep");

            // Start travel planning when a new request comes in
            processBuilder
                .OnInputEvent(nameof(TravelPlanningStep.PlanTripAsync))
                .SendEventTo(new(planStep, nameof(TravelPlanningStep.PlanTripAsync), "userInput"));

            // When user selects an option, request approval from admin
            // NOTE: I think this needs to have a different name to avoid confusion with the previous event
            processBuilder
                .OnInputEvent(nameof(TravelPlanningStep.CreateTripRequestAsync))
                .SendEventTo(new(planStep, nameof(TravelPlanningStep.CreateTripRequestAsync), "userInput"));

            // Handle admin approval
            processBuilder
                .OnInputEvent(nameof(ApprovalStep.HandleApprovalResponseAsync))
                .SendEventTo(new (approvalStep, nameof(ApprovalStep.HandleApprovalResponseAsync)));

            // Process receipts
            processBuilder
                .OnInputEvent(nameof(ReceiptProcessingStep.ProcessReceiptsAsync))
                .SendEventTo(new(receiptStep, nameof(ReceiptProcessingStep.ProcessReceiptsAsync), "userInput"));

            // Generate expense report
            processBuilder
                .OnInputEvent(nameof(ExpenseReportStep.GenerateExpenseReportAsync))
                .SendEventTo(new(expenseReportStep, nameof(ExpenseReportStep.GenerateExpenseReportAsync)));

            return processBuilder.Build();
        });

        return services;
    }


    public static IServiceCollection AddMcpClient(this IServiceCollection services)
    {
        services.AddTransient<IMcpClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            McpClientOptions mcpClientOptions = new()
            {
                ClientInfo = new (){
                    Name = "AspNetCoreSseClient",
                    Version = "1.0.0"
                }
            };

            var serviceName = "mcpserver";
            var name = $"services__{serviceName}__http__0";
            var url = Environment.GetEnvironmentVariable(name) + "/sse";

            var clientTransport = new SseClientTransport(new (){
                Name = "AspNetCoreSse",
                Endpoint = new Uri(url)
            },loggerFactory);

            // Not ideal pattern but should be enough to get it working.
            var mcpClient = McpClientFactory.CreateAsync(clientTransport, mcpClientOptions, loggerFactory).GetAwaiter().GetResult();

            return mcpClient;
        });

        return services;        
    }

    public static IServiceCollection AddChatClient(this IServiceCollection services, string modelName)
    {
        services.AddChatClient(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var azureOpenAIClient = sp.GetRequiredService<AzureOpenAIClient>();
            return new ChatClientBuilder(
                azureOpenAIClient
                    .GetChatClient(modelName)
                    .AsIChatClient())
                .UseFunctionInvocation()
                .UseOpenTelemetry()
                .UseLogging(loggerFactory)
                .Build();
        });

        return services;
    }

}
#pragma warning restore