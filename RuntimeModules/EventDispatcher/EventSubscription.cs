namespace Runtime.Modules.EventDispatcher;

/// <summary>
/// Represents a subscription to events from EventDispatcher.
/// </summary>
public sealed class EventSubscription : IDisposable
{
    public EventType EventType { get; }
    public string SourceFilter { get; } // Empty = all sources
    public Action<Event>? Callback { get; }

    public EventSubscription(EventType eventType, Action<Event> callback, string sourceFilter = "")
    {
        EventType = eventType;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        SourceFilter = sourceFilter ?? "";
    }

    public void Deliver(Event evt)
    {
        if (evt == null) return;
        
        // Check source filter
        if (!string.IsNullOrEmpty(SourceFilter) && evt.Source != SourceFilter)
            return;

        Callback?.Invoke(evt);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
