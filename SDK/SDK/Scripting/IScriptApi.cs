namespace AccuTrack.SDK.Scripting;

/// <summary>
/// Contract for the script API that Designer and Runtime agree on: ReadTag(name), WriteTag(name, value),
/// Navigate(screen), Log(message). Runtime implements; scripts call these from Lua/other engine.
/// </summary>
public interface IScriptApi
{
    /// <summary>Read tag value by name.</summary>
    object? ReadTag(string name);

    /// <summary>Write tag by name.</summary>
    void WriteTag(string name, object? value);

    /// <summary>Navigate to screen by name.</summary>
    void Navigate(string screenName);

    /// <summary>Log message.</summary>
    void Log(string message);
}
