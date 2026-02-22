namespace Runtime.Modules.TagsEngine;

/// <summary>Subscription to tag value changes.</summary>
public sealed class TagSubscription : IDisposable
{
    public string TagName { get; }
    private Action<string, object?, TagQuality>? _callback;
    private readonly Action<TagSubscription>? _onDispose;
    private bool _disposed;

    public TagSubscription(string tagName, Action<string, object?, TagQuality> callback, Action<TagSubscription>? onDispose = null)
    {
        TagName = tagName ?? "";
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        _onDispose = onDispose;
    }

    public void Deliver(object? value, TagQuality quality)
    {
        _callback?.Invoke(TagName, value, quality);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _callback = null;
        _onDispose?.Invoke(this);
    }
}
