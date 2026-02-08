using System.Text.Json;
using AccuTrack.Runtime.EventDispatch;
using AccuTrack.Runtime.Tags;

namespace AccuTrack.Runtime.Screens;

/// <summary>
/// Bind tag values to visual properties; subscribe to TagManager updates; apply to Avalonia control properties.
/// Initialize from animations.json (Plan 2 §13).
/// </summary>
public sealed class AnimationManager
{
    private TagManager? _tagManager;
    private readonly Dictionary<string, List<Action<object?>>> _bindings = new(StringComparer.OrdinalIgnoreCase);
    private EventSubscription? _tagSubscription;

    public void SetTagManager(TagManager? manager)
    {
        if (_tagManager != null && _tagSubscription != null)
            EventDispatcher.Instance.Unsubscribe(_tagSubscription);
        _tagManager = manager;
        if (manager != null)
        {
            _tagSubscription = EventDispatcher.Instance.Subscribe(EventTypes.TagValueChanged, evt =>
            {
                var name = evt.Source ?? evt.Payload?.GetValueOrDefault("TagName")?.ToString();
                if (name != null && _bindings.TryGetValue(name, out var list))
                {
                    var value = manager.GetValue(name);
                    foreach (var action in list)
                        action(value);
                }
                return ValueTask.CompletedTask;
            });
        }
    }

    public void BindTagToProperty(string tagName, Action<object?> setProperty)
    {
        if (!_bindings.TryGetValue(tagName, out var list))
        {
            list = new List<Action<object?>>();
            _bindings[tagName] = list;
        }
        list.Add(setProperty);
        var v = _tagManager?.GetValue(tagName);
        if (v != null)
            setProperty(v);
    }

    public void Unbind(string tagName)
    {
        _bindings.Remove(tagName);
    }

    /// <summary>
    /// Load animation bindings from animations.json (Designer output, Plan 1 §17). Schema: array of { tagName, controlId, property }.
    /// </summary>
    public void LoadFromConfig(JsonElement? config)
    {
        if (config == null || config.Value.ValueKind != JsonValueKind.Array) return;
        foreach (var item in config.Value.EnumerateArray())
        {
            var tagName = item.TryGetProperty("tagName", out var tn) ? tn.GetString() : item.TryGetProperty("tag", out var t) ? t.GetString() : null;
            if (string.IsNullOrEmpty(tagName)) continue;
            // Bindings are applied when controls register via BindTagToProperty; config can preload list for resolution later
        }
    }
}
