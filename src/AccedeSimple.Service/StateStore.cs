using System.Collections.Concurrent;

public class StateStore
{
    private readonly ConcurrentDictionary<string, StateEntry> _store = new();

    public void Set(string key, object value, Dictionary<string, string>? metadata = null)
    {
        var entry = new StateEntry(key, value, metadata);
        _store[key] = entry;
    }

    public StateEntry? Get(string key)
    {
        _store.TryGetValue(key, out var entry);
        return entry;
    }

    public T? GetAs<T>(string key)
    {
        return _store.TryGetValue(key, out var entry) && entry.Value is T t
            ? t
            : default;
    }

    public bool Delete(string key) => _store.TryRemove(key, out _);

    public IEnumerable<StateEntry> All() => _store.Values;

    public List<StateEntry> Search(Func<StateEntry, bool> predicate)
    {
        return _store.Values.Where(predicate).ToList();
    }
}


public class StateEntry
{
    public string Key { get; }
    public object Value { get; set; }
    public Type ValueType { get; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Metadata { get; }

    public StateEntry(string key, object value, Dictionary<string, string>? metadata = null)
    {
        Key = key;
        Value = value;
        ValueType = value.GetType();
        Timestamp = DateTime.UtcNow;
        Metadata = metadata ?? new();
    }
}