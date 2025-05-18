#pragma warning disable
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using AccedeSimple.Domain;
using System.ComponentModel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using Microsoft.AspNetCore.Mvc;
using Azure;
using AccedeSimple.Service.Services;
using Microsoft.Extensions.Options;

namespace AccedeSimple.Service.ProcessSteps;

public class TravelPlanningStep : KernelProcessStep
{
    private StateStore _state = new();
    
    private readonly IChatClient _chatClient;
    private readonly ILogger<TravelPlanningStep> _logger;
    private readonly IMcpClient _mcpClient;

    private readonly MessageService _messageService;
    
    private readonly UserSettings _userSettings;


    public TravelPlanningStep(
        ILogger<TravelPlanningStep> logger,
        IChatClient chatClient,
        IMcpClient mcpClient,
        MessageService messageService,
        StateStore stateStore,
        IOptions<UserSettings> userSettings)
    {
        _chatClient = chatClient;
        _logger = logger;
        _mcpClient = mcpClient;
        _messageService = messageService;
        _userSettings = userSettings.Value;
        _state = stateStore;
    }


    [KernelFunction("PlanTripAsync")]
    [Description("Generate trip options based on user preferences and parameters.")]
    public async Task<List<TripOption>> PlanTripAsync(
        ChatItem userInput,
        KernelProcessStepContext context)
    {

        // Generate new trip parameters
        var tripParameterPrompt = 
            $"""
            You are a travel assistant. Your task is to generate trip parameters based on the user input.

            The user has provided the following information:

            {userInput.Text}

            Today's date is: {DateTime.Now.ToString()}

            Generate trip parameters
            """;


        var res = await _chatClient.GetResponseAsync<TripParameters>(tripParameterPrompt);

        res.TryGetResult(out var tripParameters);


        List<ChatMessage> messages = [
            new ChatMessage(ChatRole.User,
                $"""
                You are a travel planning assistant. Generate trip options based on the provided parameters. 

                {JsonSerializer.Serialize(tripParameters)}

                Consider factors like cost, convenience, and preferences. Each option should include: 
                - Flight details (departure/arrival times, airline, price)
                - Hotel options (location, check-in/out dates, price)
                - Car rental options if requested

                Ensure that there is a variety of options to choose from, including different airlines, hotels, and car rental companies.
                
                Generate at least 3 different trip options with a detailed breakdown of each option.

                Ensure that dates are formatted correctly.
                """)                   
        ];

        var tools = await _mcpClient.ListToolsAsync();

        var response = await _chatClient.GetResponseAsync<List<TripOption>>(
            messages,
            new ChatOptions { 
                Temperature = 0.7f,
                Tools = [.. tools ]
        });

        response.TryGetResult(out var result);

        // Update state with trip options
        _state.Set("trip-options", result);

        // Write the result to the chat stream
        // _chatStream.AddMessage();
        await _messageService.AddMessageAsync(
            new CandidateItineraryChatItem("Here are trips matching your requirements.", result),
            _userSettings.UserId);

        var options = result ?? [];

        return options;
    }

    [KernelFunction("CreateTripRequestAsync")]
    [Description("Create a trip request based on the selected travel option.")]
    public async Task<TripRequest> CreateTripRequestAsync(
        ChatItem userInput,
        KernelProcessStepContext context)
    {        

        var options = _state.Get("trip-options").Value as List<TripOption>;
        var tripRequestPrompt = 
            $"""
            You are a travel assistant. Create a formal trip request based on the selected travel option.
            Include all necessary details for approval.

            # Trip Options
            {JsonSerializer.Serialize(options)}

            # Selected Travel Option
            {userInput.Text}
            """;

        var tripRequestResponse = await _chatClient.GetResponseAsync<TripRequest>(tripRequestPrompt);
        
        tripRequestResponse.TryGetResult(out var tripRequest);

        _state.Set("trip-requests", new List<TripRequest> { tripRequest });

        // Write the result to the chat stream
        // _chatStream.AddMessage(new AssistantResponse("Admin Approval Needed"));
        await _messageService.AddMessageAsync(
            new AssistantResponse("Trip request created. Awaiting admin approval."),
            _userSettings.UserId);

        return tripRequest;
    }
}