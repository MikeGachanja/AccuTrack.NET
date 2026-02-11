using System;
using System.Collections.Generic;
using System.Linq;

namespace Designer.Modules.EventsBus;

/// <summary>
/// Central event bus for publish/subscribe communication between modules.
/// </summary>
public class EventsBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();
    private readonly object _lock = new object();

    /// <summary>
    /// Subscribes to an event type.
    /// </summary>
    public void Subscribe<T>(Action<T> handler) where T : class
    {
        lock (_lock)
        {
            if (!_subscribers.ContainsKey(typeof(T)))
            {
                _subscribers[typeof(T)] = new List<Delegate>();
            }
            _subscribers[typeof(T)].Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribes from an event type.
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        lock (_lock)
        {
            if (_subscribers.ContainsKey(typeof(T)))
            {
                _subscribers[typeof(T)].Remove(handler);
            }
        }
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    public void Publish<T>(T eventData) where T : class
    {
        List<Delegate> handlers;
        lock (_lock)
        {
            if (!_subscribers.ContainsKey(typeof(T)))
                return;

            handlers = new List<Delegate>(_subscribers[typeof(T)]);
        }

        foreach (var handler in handlers)
        {
            try
            {
                if (handler is Action<T> typedHandler)
                {
                    typedHandler(eventData);
                }
            }
            catch
            {
                // Silently handle exceptions from event handlers
            }
        }
    }

    /// <summary>
    /// Clears all subscribers.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }
}

/// <summary>
/// Common event types for the event bus.
/// </summary>
public class ProjectChangedEvent
{
    public string ProjectName { get; set; } = string.Empty;
}

public class TagTableChangedEvent
{
    public string ScadaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}

public class ScreenChangedEvent
{
    public string ScadaName { get; set; } = string.Empty;
    public string ScreenName { get; set; } = string.Empty;
}
