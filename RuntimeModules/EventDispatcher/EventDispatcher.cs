using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;

namespace Runtime.Modules.EventDispatcher;

/// <summary>
/// Central event dispatcher for the SCADA system. Thread-safe event routing and delivery.
/// </summary>
public sealed class EventDispatcher : ModuleBase, IEventDispatcher
{
    private static EventDispatcher? _instance;
    private static readonly object _instanceLock = new();

    private readonly ConcurrentQueue<Event> _eventQueue = new();
    private readonly List<EventSubscription> _subscriptions = new();
    private readonly object _subscriptionLock = new();
    private readonly Thread _processingThread;
    private volatile bool _running;

    public static EventDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                    _instance ??= new EventDispatcher();
            }
            return _instance;
        }
    }

    public int PendingEventCount => _eventQueue.Count;

    public int SubscriptionCount
    {
        get
        {
            lock (_subscriptionLock)
                return _subscriptions.Count;
        }
    }

    private EventDispatcher()
    {
        _processingThread = new Thread(ProcessEvents)
        {
            IsBackground = true,
            Name = "EventDispatcher"
        };
    }

    public override string ModuleName => "EventDispatcher";
    public override string DisplayName => "Event Dispatcher";

    public override bool Initialize(JsonObject? config = null)
    {
        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        if (_running) return true;

        _running = true;
        _processingThread.Start();
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        _running = false;
        SetRunning(false);
    }

    public override void Shutdown()
    {
        Stop();
        // Wait for thread to finish processing
        if (_processingThread.IsAlive)
        {
            _processingThread.Join(1000);
        }
    }

    public void Publish(Event evt)
    {
        if (evt == null) return;
        _eventQueue.Enqueue(evt);
    }

    public void PublishSync(Event evt)
    {
        if (evt == null) return;
        DeliverEvent(evt);
    }

    public EventSubscription Subscribe(EventType eventType, Action<Event> callback, string sourceFilter = "")
    {
        var subscription = new EventSubscription(eventType, callback, sourceFilter);
        lock (_subscriptionLock)
        {
            _subscriptions.Add(subscription);
        }
        return subscription;
    }

    public void Unsubscribe(EventSubscription subscription)
    {
        if (subscription == null) return;
        lock (_subscriptionLock)
        {
            _subscriptions.Remove(subscription);
        }
        subscription.Dispose();
    }

    private void ProcessEvents()
    {
        while (_running || _eventQueue.Count > 0)
        {
            if (_eventQueue.TryDequeue(out var evt))
            {
                DeliverEvent(evt);
            }
            else
            {
                Thread.Sleep(10); // Small delay when queue is empty
            }
        }
    }

    private void DeliverEvent(Event evt)
    {
        List<EventSubscription> toNotify;
        lock (_subscriptionLock)
        {
            toNotify = _subscriptions
                .Where(s => s.EventType == evt.Type)
                .ToList();
        }

        foreach (var subscription in toNotify)
        {
            try
            {
                subscription.Deliver(evt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EventDispatcher] Error delivering event: {ex.Message}");
            }
        }
    }
}
