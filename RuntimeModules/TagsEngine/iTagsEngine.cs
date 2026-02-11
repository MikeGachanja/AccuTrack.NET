namespace Runtime.Modules.TagsEngine;

/// <summary>Tags engine interface. Manages runtime tags, I/O, and subscriptions; used by screens and Communication.</summary>
public interface ITagsEngine
{
    /// <summary>Tag storage and subscriptions.</summary>
    TagManager TagManager { get; }
    /// <summary>Write path for tags (used by EventManager and runtime controls).</summary>
    TagIOHandler TagIOHandler { get; }
    /// <summary>Wire to Communication module for real I/O (Modbus, OPC UA).</summary>
    void SetCommunicationModule(object? commModule);
}
