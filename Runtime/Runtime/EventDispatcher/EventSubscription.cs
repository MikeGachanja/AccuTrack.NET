namespace AccuTrack.Runtime.EventDispatch;

/// <summary>
/// Token for unsubscribing; holds callback and optional filter.
/// </summary>
public sealed class EventSubscription
{
    private readonly Func<Event, ValueTask> _callback;
    private readonly string _eventType;
    private readonly string? _sourceFilter;

    internal EventSubscription(string eventType, Func<Event, ValueTask> callback, string? sourceFilter)
    {
        _eventType = eventType;
        _callback = callback;
        _sourceFilter = sourceFilter;
    }

    internal bool Matches(string eventType, string? source)
    {
        if (string.Equals(_eventType, eventType, StringComparison.OrdinalIgnoreCase) == false)
            return false;
        if (_sourceFilter == null)
            return true;
        return string.Equals(_sourceFilter, source, StringComparison.OrdinalIgnoreCase);
    }

    internal ValueTask InvokeAsync(Event evt) => _callback(evt);
}
