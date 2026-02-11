namespace Runtime.Modules.EventDispatcher;

/// <summary>
/// Event dispatcher interface for inter-module communication.
/// </summary>
public interface IEventDispatcher
{
    /// <summary>Publish an event asynchronously.</summary>
    void Publish(Event evt);

    /// <summary>Publish an event synchronously (immediate delivery).</summary>
    void PublishSync(Event evt);

    /// <summary>Subscribe to events of a specific type with a callback.</summary>
    EventSubscription Subscribe(EventType eventType, Action<Event> callback, string sourceFilter = "");

    /// <summary>Unsubscribe from events.</summary>
    void Unsubscribe(EventSubscription subscription);

    /// <summary>Get number of pending events in queue.</summary>
    int PendingEventCount { get; }

    /// <summary>Get number of active subscriptions.</summary>
    int SubscriptionCount { get; }
}
