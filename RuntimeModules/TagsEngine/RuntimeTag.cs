using System.Collections.Concurrent;

namespace Runtime.Modules.TagsEngine;

/// <summary>Quality of a tag value.</summary>
public enum TagQuality
{
    Good = 0,
    Bad = 1,
    Uncertain = 2,
    NotConnected = 3
}

/// <summary>Runtime representation of a tag: value, quality, timestamp.</summary>
public sealed class RuntimeTag
{
    private readonly ConcurrentDictionary<object, object> _lock = new();
    private object? _value;
    private TagQuality _quality;
    private DateTime _timestamp;
    private object? _previousValue;

    public string Name { get; }
    public string Address { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsEnabled { get; set; } = true;

    public RuntimeTag(string name)
    {
        Name = name ?? "";
    }

    public object? Value
    {
        get => _value;
        set => SetValue(value, TagQuality.Good);
    }

    public TagQuality Quality => _quality;
    public DateTime Timestamp => _timestamp;

    public void SetValue(object? value, TagQuality quality = TagQuality.Good)
    {
        lock (_lock)
        {
            _previousValue = _value;
            _value = value;
            _quality = quality;
            _timestamp = DateTime.Now;
        }
    }

    public void SetQuality(TagQuality quality)
    {
        lock (_lock)
        {
            _quality = quality;
        }
    }

    public object? GetValue()
    {
        lock (_lock) return _value;
    }

    public TagQuality GetQuality()
    {
        lock (_lock) return _quality;
    }

    public DateTime GetTimestamp()
    {
        lock (_lock) return _timestamp;
    }

    public bool HasValueChanged(object? newValue)
    {
        if (newValue == null && _value == null) return false;
        if (newValue == null || _value == null) return true;
        return !Equals(newValue, _value);
    }

    public static string QualityToString(TagQuality quality) => quality.ToString();
    public static bool TryParseQuality(string? s, out TagQuality quality) => Enum.TryParse(s, true, out quality);
}
