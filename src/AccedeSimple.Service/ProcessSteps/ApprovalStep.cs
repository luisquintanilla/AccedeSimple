#pragma warning disable
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using AccedeSimple.Domain;
using System.ComponentModel;
using AccedeSimple.Service.Services;
using Microsoft.Extensions.Options;

namespace AccedeSimple.Service.ProcessSteps;

public class ApprovalStep : KernelProcessStep
{
    private StateStore _state = new();
    private readonly UserSettings _userSettings;
    private readonly IList<TripRequest> _requests;
    private readonly MessageService _messageService;
    private readonly IChatClient _chatClient;

    public ApprovalStep(
        IChatClient _chatClient,
        MessageService messageService,
        IOptions<UserSettings> userSettings,
        StateStore state)
    {
        _chatClient = _chatClient;
        _messageService = messageService;
        _state = state;
        _userSettings = userSettings.Value;

    }

    [KernelFunction("ProcessApprovalAsync")]
    [Description("Process the trip request for approval.")]
    public async Task ProcessApprovalAsync(
        ItinerarySelectedChatItem userInput,
        KernelProcessStepContext context)
    {
        var input = userInput;

        var options = _state.Get("trip-options").Value as List<TripOption>;

        var selectedOption = options.Where(o => o.OptionId == input.OptionId);

        var generateTripRequestPrompt =
            $"""
            Create a formal trip request based on the selected travel option.
            Include all necessary details for approval:

            # Trip Option
            
            {JsonSerializer.Serialize(selectedOption)}
            """;

        var tripResponse = await _chatClient.GetResponseAsync<TripRequest>(generateTripRequestPrompt);

        tripResponse.TryGetResult(out var tripRequest);

        _state.Set("trip-requests", new List<TripRequest> { tripRequest });
    }
            

    [KernelFunction("HandleApprovalResponseAsync")]
    [Description("Handle the admin's approval or rejection of the trip request.")]
    public async Task HandleApprovalResponseAsync(
        TripRequestResult result,
        KernelProcessStepContext context)
    {
        // Safely remove the request from the list
        var requests = _state.Get("trip-requests").Value as List<TripRequest>;

        var request = requests?.FirstOrDefault(r => r.RequestId == result.RequestId);

        if (request != null)
        {
            requests.Remove(request);
            _state.Set("trip-requests", requests);
        }

        await _messageService.AddMessageAsync(new TripRequestDecisionChatItem(result), _userSettings.UserId);
    }
}
#pragma warning restore