using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AccuTrack.Runtime.EventDispatch;

/// <summary>
/// Singleton; thread-safe event queue and delivery; subscribe by type (and optional source filter).
/// </summary>
public sealed class EventDispatcher
{
    private static EventDispatcher? _instance;
    private static readonly object StaticLock = new();

    public static EventDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (StaticLock)
                    _instance ??= new EventDispatcher();
            }
            return _instance;
        }
    }

    private readonly Channel<Event> _queue = Channel.CreateUnbounded<Event>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<string, List<EventSubscription>> _subscriptions = new();
    private readonly object _subLock = new();
    private CancellationTokenSource? _cts;
    private Task? _dispatchTask;
    private bool _initialized;

    public static void SetInstance(EventDispatcher? instance)
    {
        lock (StaticLock)
            _instance = instance;
    }

    public void Initialize()
    {
        if (_initialized) return;
        lock (_subLock)
        {
            if (_initialized) return;
            _cts = new CancellationTokenSource();
            _dispatchTask = Task.Run(() => DispatchLoopAsync(_cts.Token));
            _initialized = true;
        }
    }

    public void Shutdown()
    {
        lock (_subLock)
        {
            if (!_initialized) return;
            _cts?.Cancel();
            _queue.Writer.Complete();
            _dispatchTask?.GetAwaiter().GetResult();
            _subscriptions.Clear();
            _initialized = false;
        }
    }

    public void Publish(Event evt)
    {
        if (!_queue.Writer.TryWrite(evt))
            _queue.Writer.Complete(); // writer completed elsewhere
    }

    public void PublishSync(Event evt)
    {
        DispatchToSubscribersAsync(evt).AsTask().GetAwaiter().GetResult();
    }

    public EventSubscription Subscribe(string eventType, Func<Event, ValueTask> callback, string? sourceFilter = null)
    {
        var sub = new EventSubscription(eventType, callback, sourceFilter);
        _subscriptions.AddOrUpdate(eventType, _ => new List<EventSubscription> { sub }, (_, list) =>
        {
            lock (list) list.Add(sub);
            return list;
        });
        return sub;
    }

    public void Unsubscribe(EventSubscription subscription)
    {
        foreach (var list in _subscriptions.Values)
        {
            lock (list)
                list.RemoveAll(s => ReferenceEquals(s, subscription));
        }
    }

    public int PendingEventCount => 0; // Channel doesn't expose count easily; could use a counter.
    public int SubscriptionCount => _subscriptions.Values.Sum(list => { lock (list) return list.Count; });

    private async Task DispatchLoopAsync(CancellationToken ct)
    {
        await foreach (var evt in _queue.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            await DispatchToSubscribersAsync(evt).ConfigureAwait(false);
    }

    private async ValueTask DispatchToSubscribersAsync(Event evt)
    {
        var key = evt.Type;
        if (!_subscriptions.TryGetValue(key, out var list))
            return;
        EventSubscription[] copy;
        lock (list)
            copy = list.Where(s => s.Matches(evt.Type, evt.Source)).ToArray();
        foreach (var sub in copy)
            await sub.InvokeAsync(evt).ConfigureAwait(false);
    }
}
