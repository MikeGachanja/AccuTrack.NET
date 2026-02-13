using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runtime.Modules.TagsEngine;

/// <summary>Manages all tags: load from JSON, register, update value, subscriptions.</summary>
public sealed class TagManager
{
    private readonly ConcurrentDictionary<string, RuntimeTag> _tagsByName = new();
    private readonly ConcurrentDictionary<string, RuntimeTag> _tagsByAddress = new();
    private readonly ConcurrentDictionary<string, List<TagSubscription>> _subscriptions = new();
    private readonly object _subLock = new();

    public bool LoadFromJsonFile(string filePath)
    {
        if (!File.Exists(filePath)) return false;
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            return LoadFromJson(doc.RootElement);
        }
        catch { return false; }
    }

    public bool LoadFromJson(JsonElement root)
    {
        try
        {
            if (root.TryGetProperty("tags", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var address = item.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "";
                    var description = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                    var dataType = item.TryGetProperty("dataType", out var dt) ? dt.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(name))
                        RegisterTag(name, address, description, dataType);
                }
            }
            return true;
        }
        catch { return false; }
    }

    public bool RegisterTag(string tagName, string address = "", string description = "", string dataType = "")
    {
        if (string.IsNullOrEmpty(tagName)) return false;
        var tag = new RuntimeTag(tagName) 
        { 
            Address = address ?? "", 
            Description = description ?? "",
            DataType = dataType ?? ""
        };
        _tagsByName[tagName] = tag;
        if (!string.IsNullOrEmpty(address))
            _tagsByAddress[address] = tag;
        return true;
    }

    public bool UnregisterTag(string tagName)
    {
        if (!_tagsByName.TryRemove(tagName, out var tag)) return false;
        if (!string.IsNullOrEmpty(tag.Address))
            _tagsByAddress.TryRemove(tag.Address, out _);
        lock (_subLock)
            _subscriptions.TryRemove(tagName, out _);
        return true;
    }

    public RuntimeTag? GetTag(string tagName) => _tagsByName.TryGetValue(tagName, out var t) ? t : null;
    public RuntimeTag? GetTagByAddress(string address) => _tagsByAddress.TryGetValue(address ?? "", out var t) ? t : null;
    public bool HasTag(string tagName) => _tagsByName.ContainsKey(tagName ?? "");
    public bool HasTagByAddress(string address) => _tagsByAddress.ContainsKey(address ?? "");
    public IReadOnlyList<string> GetTagNames() => _tagsByName.Keys.ToList();
    public int TagCount => _tagsByName.Count;

    public bool UpdateTagValue(string tagName, object? value, TagQuality quality = TagQuality.Good)
    {
        if (!_tagsByName.TryGetValue(tagName ?? "", out var tag)) return false;
        var prev = tag.GetValue();
        tag.SetValue(value, quality);
        if (tag.HasValueChanged(value))
            NotifySubscriptions(tagName!, value, quality);
        return true;
    }

    public bool UpdateTagValueByAddress(string address, object? value, TagQuality quality = TagQuality.Good)
    {
        if (!_tagsByAddress.TryGetValue(address ?? "", out var tag)) return false;
        tag.SetValue(value, quality);
        NotifySubscriptions(tag.Name, value, quality);
        return true;
    }

    public object? GetTagValue(string tagName) => GetTag(tagName)?.GetValue();

    public TagSubscription Subscribe(string tagName, Action<string, object?, TagQuality> callback)
    {
        var sub = new TagSubscription(tagName, callback);
        lock (_subLock)
        {
            if (!_subscriptions.ContainsKey(tagName))
                _subscriptions[tagName] = new List<TagSubscription>();
            _subscriptions[tagName].Add(sub);
        }
        return sub;
    }

    public void Unsubscribe(TagSubscription subscription)
    {
        if (subscription == null) return;
        lock (_subLock)
        {
            if (_subscriptions.TryGetValue(subscription.TagName, out var list))
            {
                list.Remove(subscription);
                subscription.Dispose();
            }
        }
    }

    public void Clear()
    {
        _tagsByName.Clear();
        _tagsByAddress.Clear();
        lock (_subLock)
        {
            foreach (var list in _subscriptions.Values)
                foreach (var s in list)
                    s.Dispose();
            _subscriptions.Clear();
        }
    }

    private void NotifySubscriptions(string tagName, object? value, TagQuality quality)
    {
        List<TagSubscription>? list;
        lock (_subLock)
            list = _subscriptions.TryGetValue(tagName, out var l) ? l.ToList() : null;
        if (list == null) return;
        foreach (var sub in list)
        {
            try { sub.Deliver(value, quality); }
            catch { }
        }
    }
}
