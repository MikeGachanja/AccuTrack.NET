namespace Runtime.Modules.TagsEngine;

/// <summary>Handles tag I/O with communication layer. Stub: writes go to TagManager, reads return from TagManager.</summary>
public sealed class TagIOHandler
{
    private readonly TagManager _tagManager;
    private volatile bool _running;
    private object? _communicationModule; // ICommunication when available

    public TagIOHandler(TagManager tagManager)
    {
        _tagManager = tagManager ?? throw new ArgumentNullException(nameof(tagManager));
    }

    public void SetCommunicationModule(object? commModule)
    {
        _communicationModule = commModule;
    }

    public void Start() => _running = true;
    public void Stop() => _running = false;
    public bool IsRunning => _running;

    public bool ReadTag(string tagName)
    {
        if (!_running) return false;
        // When Communication module is wired: read from device and call _tagManager.UpdateTagValue
        var tag = _tagManager.GetTag(tagName);
        if (tag != null)
            return true; // Stub: just indicate tag exists
        return false;
    }

    public bool ReadTagByAddress(string address)
    {
        if (!_running) return false;
        var tag = _tagManager.GetTagByAddress(address);
        return tag != null;
    }

    public bool WriteTag(string tagName, object? value)
    {
        if (!_running) return false;
        // When Communication module is wired: write to device first, then update tag
        return _tagManager.UpdateTagValue(tagName, value, TagQuality.Good);
    }

    public bool WriteTagByAddress(string address, object? value)
    {
        if (!_running) return false;
        return _tagManager.UpdateTagValueByAddress(address, value, TagQuality.Good);
    }

    public void ReadAllTags()
    {
        foreach (var name in _tagManager.GetTagNames())
            ReadTag(name);
    }
}
