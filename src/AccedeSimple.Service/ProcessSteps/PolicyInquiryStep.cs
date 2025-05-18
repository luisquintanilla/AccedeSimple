#pragma warning disable
using System.ComponentModel;
using AccedeSimple.Service.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;

namespace AccedeSimple.Service.ProcessSteps;

public class PolicyInquiryStep : KernelProcessStep
{
    private readonly MessageService _messageService;
    private readonly IChatClient _chatClient;

    private readonly SearchService _searchService;
    private readonly UserSettings _userSettings;

    public PolicyInquiryStep(
        MessageService messageService,
        IChatClient chatClient,
        SearchService searchService,
        IOptions<UserSettings> userSettings)
    {
        _messageService = messageService;
        _chatClient = chatClient;
        _searchService = searchService;
        _userSettings = userSettings.Value;
    }

    [KernelFunction("ProcessPolicyInquiryAsync")]
    [Description("Process the policy inquiry.")]
    public async Task ProcessPolicyInquiryAsync(
        UserMessage userInput,
        KernelProcessStepContext context)
    {
        var input = userInput;

        // Search for relevant documents
        var results = new List<Document>();
        await foreach (var result in _searchService.SearchAsync(input.Text))
        {
            results.Add(result);
        }

        var policyInquiryPrompt =
            $"""
            Process the policy inquiry.
            Provide a summary of the policy based on the following information:
            
            # Policy Inquiry
            {input.Text}

            # Search Results
            {string.Join(Environment.NewLine, results.Select(r => $"* {r.Embedding}"))}
            """;

        var policyResponse = await _chatClient.GetResponseAsync(policyInquiryPrompt);

        // Add the response to the message service
        await _messageService.AddMessageAsync(new AssistantResponse(policyResponse.Text), _userSettings.UserId);
    }
}
#pragma warning restore