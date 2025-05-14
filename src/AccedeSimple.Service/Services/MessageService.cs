using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace AccedeSimple.Service.Services;

public class MessageService
{
    private readonly ChatStream _chatStream;
    private readonly ConcurrentDictionary<string, List<ChatItem>> _history;

    public MessageService(ChatStream chatStream, [FromKeyedServices("history")] ConcurrentDictionary<string, List<ChatItem>> history)
    {
        _chatStream = chatStream;
        _history = history;
    }

    public async Task AddMessageAsync(ChatItem message, string userId)
    {
        _chatStream.AddMessage(message);
        _history.AddOrUpdate(userId, new List<ChatItem> { message }, (key, oldValue) =>
        {
            oldValue.Add(message);
            return oldValue;
        });
    }
}