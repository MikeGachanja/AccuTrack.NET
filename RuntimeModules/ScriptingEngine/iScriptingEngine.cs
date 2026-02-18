namespace Runtime.Modules.ScriptingEngine;

/// <summary>Scripting engine interface (Lua via MoonSharp). Used by Scheduler and EventManager RunScript actions.</summary>
public interface IScriptingEngine
{
    /// <summary>Initialize the script engine.</summary>
    bool Initialize();
    /// <summary>Load a script file for later execution.</summary>
    bool LoadScript(string filePath);
    /// <summary>Execute the last loaded script, optionally passing a single argument string (e.g. for EventManager).</summary>
    bool ExecuteScript(string? arguments = null);
    /// <summary>Execute inline script code.</summary>
    bool ExecuteString(string luaCode);
    /// <summary>Last error message from load/execute.</summary>
    string GetLastError();
    /// <summary>Raised when a script error occurs.</summary>
    event EventHandler<string>? ErrorOccurred;
    /// <summary>Raised when a script is loaded.</summary>
    event EventHandler<string>? ScriptLoaded;
    /// <summary>Raised when execution completes.</summary>
    event EventHandler? ScriptExecuted;
}
