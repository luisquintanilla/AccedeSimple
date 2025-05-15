#pragma warning disable
using System.Text.Json;
using AccedeSimple.Domain;
using AccedeSimple.Service.ProcessSteps;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace AccedeSimple.Service.Services;

public class ProcessService
{
    private readonly Kernel _kernel;
    private readonly KernelProcess _process;
    private readonly MessageService _messageService;

    private readonly IChatClient _chatClient;
    private readonly UserSettings _userSettings;
    private readonly HttpClient _httpClient;

    public ProcessService(
        Kernel kernel, 
        KernelProcess process, 
        MessageService messageService, 
        IChatClient chatClient,
        IOptions<UserSettings> userSettings,
        IHttpClientFactory httpClientFactory)
    {
        _kernel = kernel;
        _process = process;
        _messageService = messageService;
        _chatClient = chatClient;
        _userSettings = userSettings.Value;
        _httpClient = httpClientFactory.CreateClient("LocalGuide");
    }

    public async Task ActAsync(UserIntent userIntent, ChatItem userInput)
    {
        switch (userIntent)
        {
            case UserIntent.General:
                // Handle general inquiries
                var response = await _chatClient.GetResponseAsync(userInput.ToChatMessage());
                await _messageService.AddMessageAsync(new AssistantResponse(response.Text), _userSettings.UserId);
                break;
            case UserIntent.AskLocalGuide:
                // Handle local guide inquiries
                var builder = new UriBuilder(_httpClient.BaseAddress)
                {
                    Path = "/attractions",
                    Query = $"query={userInput.Text}"
                };
                var fullUri = builder.Uri;
                var localGuideRequest = await _httpClient.PostAsync(builder.Uri, null);
                var body = await localGuideRequest.Content.ReadAsStringAsync();
                await _messageService.AddMessageAsync(new AssistantResponse(body), _userSettings.UserId);
                break;
            case UserIntent.StartTravelPlanning:
                await _process.StartAsync(_kernel, new KernelProcessEvent { Id = nameof(TravelPlanningStep.PlanTripAsync), Data = userInput });
                break;
            case UserIntent.StartTripApproval:
                await _process.StartAsync(_kernel, new KernelProcessEvent { Id = nameof(TravelPlanningStep.CreateTripRequestAsync), Data = userInput });
                break;
            case UserIntent.ProcessReceipts:
                await _process.StartAsync(_kernel, new KernelProcessEvent { Id = nameof(ReceiptProcessingStep.ProcessReceiptsAsync), Data = userInput });
                break;
            case UserIntent.GenerateExpenseReport:
                await _process.StartAsync(_kernel, new KernelProcessEvent { Id = nameof(ExpenseReportStep.GenerateExpenseReportAsync) });
                break;
            default:
                await _messageService.AddMessageAsync(new AssistantResponse("Unknown intent. Please clarify your request."), _userSettings.UserId);
                break;
        }
    }
}
#pragma warning restore