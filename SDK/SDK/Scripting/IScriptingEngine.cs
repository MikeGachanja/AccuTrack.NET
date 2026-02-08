namespace AccuTrack.SDK.Scripting;

/// <summary>
/// Optional interface for scripting: run script by name, evaluate expression, register API
/// (ReadTag, WriteTag, Log, Navigate). Runtime plugs Lua or another engine.
/// </summary>
public interface IScriptingEngine
{
    /// <summary>Run a script by name.</summary>
    void RunScript(string scriptName);

    /// <summary>Evaluate an expression and return result.</summary>
    object? Evaluate(string expression);

    /// <summary>Register the script API (ReadTag, WriteTag, Log, Navigate, etc.).</summary>
    void RegisterApi(IScriptApi api);
}
