namespace Runtime.Modules.TagsEngine;

/// <summary>Subscription to tag value changes.</summary>
public sealed class TagSubscription : IDisposable
{
    public string TagName { get; }
    public Action<string, object?, TagQuality>? Callback { get; }

    public TagSubscription(string tagName, Action<string, object?, TagQuality> callback)
    {
        TagName = tagName ?? "";
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public void Deliver(object? value, TagQuality quality)
    {
        Callback?.Invoke(TagName, value, quality);
    }

    public void Dispose() { }
}
