namespace AccuTrack.Runtime.EventDispatch;

/// <summary>
/// Event with type, source, and payload for EventDispatcher.
/// </summary>
public class Event
{
    public string Type { get; }
    public string? Source { get; }
    public IReadOnlyDictionary<string, object?>? Payload { get; }

    public Event(string type, string? source = null, IReadOnlyDictionary<string, object?>? payload = null)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Source = source;
        Payload = payload;
    }
}
