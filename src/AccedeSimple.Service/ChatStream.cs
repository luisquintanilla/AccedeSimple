using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Serialization;
using AccedeSimple.Domain;
using Microsoft.Extensions.AI;
using static AccedeSimple.Service.Endpoints;

namespace AccedeSimple.Service;

public class ChatStream
{
    private readonly Subject<ChatItem> _messageSubject = new();
    
    // private int _nextIndex = 0;

    public IObservable<ChatItem> Messages => _messageSubject.AsObservable();

    public void AddMessage(ChatItem item)
    {
        _messageSubject.OnNext(item);
    }
}

public abstract class ChatItem(string text)
{

    public string Text { get; init; } = text;

    public string Id { get; init; }

    public abstract ChatRole Role { get; }

    public abstract string Type { get; }

    [JsonIgnore]
    public abstract bool IsUserVisible { get; }

    public virtual ChatMessage? ToChatMessage() => new ChatMessage(Role, Text);

    [JsonIgnore]
    internal bool IsUserMessage => Role == ChatRole.User;
}

// User messages
public sealed class UserMessage(string text) : ChatItem(text)
{
    public override string Type => "user";
    public override ChatRole Role => ChatRole.User;
    public override bool IsUserVisible => true;

    public List<UriAttachment>? Attachments { get; init; }

    public override ChatMessage? ToChatMessage() => Attachments switch
    {
        { Count: > 0 } attachments => CreateChatMessageWithAttachments(attachments),
        _ => base.ToChatMessage(),
    };

    private ChatMessage CreateChatMessageWithAttachments(List<UriAttachment> attachments)
    {
        var content = new List<AIContent>(attachments.Count + 1) { new TextContent(Text) };
        foreach (var attachment in attachments)
        {
            if (attachment.Uri.StartsWith("data:"))
            {
                content.Add(new DataContent(attachment.Uri, attachment.ContentType));
            }
            else
            {
                content.Add(new UriContent(attachment.Uri, attachment.ContentType));
            }
        }

        return new ChatMessage(ChatRole.User, content);
    }
}


// Assistant messages
public class AssistantResponse(string text) : ChatItem(text)
{
    public override string Type => "assistant";

    public string? ResponseId { get; set; }
    public bool IsFinal { get; set; }
    public override ChatRole Role => ChatRole.Assistant;
    public override bool IsUserVisible => true;
}


// Candidate itinerary messages
internal sealed class CandidateItineraryChatItem : ChatItem
{
    [SetsRequiredMembers]
    public CandidateItineraryChatItem(string text, List<TripOption> options) : base(text)
    {
        Id = Guid.NewGuid().ToString();
        Options = options;
    }

    public List<TripOption> Options { get; }
    public override string Type => "candidate-itineraries";
    public override ChatRole Role => ChatRole.Assistant;
    public override bool IsUserVisible => true;
    public override ChatMessage? ToChatMessage()
    {
        var text =
            $"""
            Here are the trips matching your requirements:

            {string.Join("\n", Options.Select(option => JsonSerializer.Serialize(option, JsonSerializerOptions.Web)))}
            """;
        return new ChatMessage(ChatRole.User, text);
    }
}

// Trip request messages
public sealed class TripRequestUpdated(string text) : ChatItem(text)
{
    public override string Type => "trip-request-updated";
    public override ChatRole Role => ChatRole.Assistant;
    public override bool IsUserVisible => true;
    public override ChatMessage? ToChatMessage() => null;
}

// Itinerary selected messages
public class ItinerarySelectedChatItem(string text) : ChatItem(text)
{
    public required string MessageId { get; init; }
    
    public required string OptionId { get; init; }

    public override string Type => "itinerary-selected";
    public override ChatRole Role => ChatRole.User;
    public override bool IsUserVisible => false;
    public override ChatMessage? ToChatMessage() => 
        new ChatMessage(ChatRole.User, $"I've selected itinerary option {OptionId}.");
}

// Trip request result messages
public class TripRequestDecisionChatItem(TripRequestResult result) : ChatItem(GetTextForStatus(result.Status))
{
    public TripRequestResult Result { get; } = result;
    public override string Type => "trip-approval-result";
    public override ChatRole Role => ChatRole.Assistant;
    public override bool IsUserVisible => true;
    public override ChatMessage? ToChatMessage() => new ChatMessage(ChatRole.User, Text);
    private static string GetTextForStatus(TripRequestStatus status) => status switch
    {
        TripRequestStatus.Approved => "Trip request approved.",
        TripRequestStatus.Rejected => "Trip request rejected.",
        _ => throw new NotSupportedException($"Unsupported trip request status: {status}")
    };
}