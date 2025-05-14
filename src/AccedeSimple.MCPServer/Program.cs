using System.ComponentModel;
using AccedeSimple.Domain;
using ModelContextProtocol.Server;
using Microsoft.Extensions.AI;
using Azure.Identity;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

builder.Services.AddChatClient(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new ChatClientBuilder(
        new AzureOpenAIClient(
            new Uri(Environment.GetEnvironmentVariable("AOAI_ENDPOINT")),
            new DefaultAzureCredential())
            .GetChatClient("gpt-4o-mini")
            .AsIChatClient())
        .UseFunctionInvocation()
        .UseOpenTelemetry()
        .UseLogging(loggerFactory)
        .Build();
});


var app = builder.Build();

app.MapMcp();

app.Run();

[McpServerToolType]
public sealed class FlightTool
{
    [McpServerTool, Description("Returns a list of available trip options based on the provided parameters")]
    public static async ValueTask<List<TripOption>> SearchTripOptions(
        [FromServices] IChatClient chatClient, 
        TripParameters parameters, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Simulate an external API call
            var res = await chatClient.GetResponseAsync<List<TripOption>>(
                    new ChatMessage(
                        role: ChatRole.User,
                        content: "Please provide a list of available trip options based on the provided parameters. Make sure that the parameters conform to the user's request. Make sure to fill in all the required fields."));

            res.TryGetResult(out var flights);

            return flights ?? [];
        }
        catch //(Exception ex)
        {
            //logger.LogError(ex, "Error loading flights from JSON file");
            return [];
        }
    }
}
