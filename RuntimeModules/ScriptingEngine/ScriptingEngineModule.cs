namespace Runtime.Modules.ScriptingEngine;

/// <summary>Scripting engine stub. TODO: Integrate Lua (e.g. NLua) for script execution.</summary>
public sealed class ScriptingEngineModule : IScriptingEngine
{
    private string _lastError = "";
    private string _loadedPath = "";

    public bool Initialize() => true;

    public bool LoadScript(string filePath)
    {
        _lastError = "";
        if (!File.Exists(filePath))
        {
            _lastError = "File not found: " + filePath;
            return false;
        }
        _loadedPath = filePath;
        ScriptLoaded?.Invoke(this, filePath);
        return true;
    }

    public bool ExecuteScript()
    {
        _lastError = "";
        if (string.IsNullOrEmpty(_loadedPath))
        {
            _lastError = "No script loaded.";
            return false;
        }
        // Stub: no Lua execution yet
        ScriptExecuted?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool ExecuteString(string luaCode)
    {
        _lastError = "";
        // Stub: no Lua execution yet
        ScriptExecuted?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public string GetLastError() => _lastError;

    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? ScriptLoaded;
    public event EventHandler? ScriptExecuted;
}
