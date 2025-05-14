#pragma warning disable
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;
using AccedeSimple.Domain;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Options;
using AccedeSimple.Service.Services;

namespace AccedeSimple.Service.ProcessSteps;

public class ReceiptProcessingStep : KernelProcessStep
{
    private IChatClient _chatClient;
    private StateStore _state;
    private readonly MessageService _messageService;
    private readonly UserSettings _userSettings;

    public ReceiptProcessingStep(
        IChatClient chatClient,
        StateStore state,
        MessageService messageService,
        IOptions<UserSettings> userSettings)
    {
        _chatClient = chatClient;
        _state = state;
        _messageService = messageService;
        _userSettings = userSettings.Value;
    }

    [KernelFunction("ProcessReceiptsAsync")]
    [Description("Process receipts for expense compliance and categorization.")]
    public async Task ProcessReceiptsAsync(
        UserMessage userInput,
        KernelProcessStepContext context)
    {

        var receiptResponse = await _chatClient.GetResponseAsync<List<ReceiptData>>(userInput.ToChatMessage());

        receiptResponse.TryGetResult(out var receipts);

        // Update state with processed receipts
        _state.Set("receipts", receipts);

        // await context.EmitEventAsync("ReceiptsProcessed");
        await _messageService.AddMessageAsync(new AssistantResponse("Receipts processed successfully."), _userSettings.UserId);
    }
}