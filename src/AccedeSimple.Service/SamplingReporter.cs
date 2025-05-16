#pragma warning disable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

namespace AccedeConcierge.Service;

public class SamplingReporter : DelegatingChatClient
{
    private readonly ReportingConfiguration _reportingConfiguration;
    private readonly ActivitySource? _activitySource;

    public SamplingReporter(IChatClient innerClient, string path) : base(innerClient)
    {
        _activitySource = InnerClient.GetService<ActivitySource>();

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        _reportingConfiguration = DiskBasedReportingConfiguration.Create(path,
            [new RelevanceTruthAndCompletenessEvaluator()],
            // Don't use 'this' as that could include the evaluation conversation as part of the evaluation
            new ChatConfiguration(InnerClient));
    }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var resp = await InnerClient.GetResponseAsync(messages, options, cancellationToken);

        await RunEvaluation(messages, resp, options);

        return resp;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var resp = InnerClient.GetStreamingResponseAsync(messages, options, cancellationToken);
        await foreach (var value in resp)
        {
            yield return value;
        }

        var chatResp = await resp.ToChatResponseAsync();

        await RunEvaluation(messages, chatResp, options);
    }

    protected override void Dispose(bool disposing)
    {

    }

    private async Task RunEvaluation(IEnumerable<ChatMessage> messages, ChatResponse response, ChatOptions? options)
    {
        if (string.IsNullOrEmpty(response.Text))
        {
            return;
        }

        var activity = _activitySource?.StartActivity("Evaluating");
        await using var scenarioRun = await _reportingConfiguration.CreateScenarioRunAsync(GetType().Name,
            iterationName: options?.ChatThreadId ?? Guid.NewGuid().ToString());
        var evalResult = await scenarioRun.EvaluateAsync(messages, response);
        activity?.Stop();
    }
}
#pragma warning restore