using System;
using System.IO;
using MoonSharp.Interpreter;

namespace Runtime.Modules.ScriptingEngine;

/// <summary>Lua scripting engine using MoonSharp. Used by Scheduler and EventManager.</summary>
public sealed class ScriptingEngineModule : IScriptingEngine
{
    private Script? _script;
    private string _lastError = "";
    private string _loadedPath = "";
    private string _loadedCode = "";
    private Action<string>? _printCallback;
    private Func<string, object?>? _readTag;
    private Action<string, object?>? _writeTag;

    /// <summary>Optional: set a callback to capture print() output (e.g. for console/logs).</summary>
    public void SetPrintCallback(Action<string>? callback) => _printCallback = callback;

    /// <summary>Optional: provide tag read/write so Lua can use read_tag(name) and write_tag(name, value).</summary>
    public void SetTagAccess(Func<string, object?>? readTag, Action<string, object?>? writeTag)
    {
        _readTag = readTag;
        _writeTag = writeTag;
    }

    public bool Initialize()
    {
        try
        {
            _script = new Script();
            RegisterPrint();
            RegisterTagGlobals();
            return true;
        }
        catch (Exception ex)
        {
            _lastError = "Failed to initialize Lua: " + ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
    }

    private void EnsureScript()
    {
        if (_script == null)
        {
            _script = new Script();
            RegisterPrint();
            RegisterTagGlobals();
        }
    }

    private void RegisterTagGlobals()
    {
        if (_script == null) return;
        _script.Globals["read_tag"] = (Func<string, DynValue>)ReadTagImpl;
        _script.Globals["write_tag"] = (Action<string, DynValue>)WriteTagImpl;
    }

    private DynValue ReadTagImpl(string name)
    {
        var v = _readTag?.Invoke(name);
        if (v == null) return DynValue.Nil;
        return DynValue.FromObject(_script!, v);
    }

    private void WriteTagImpl(string name, DynValue value)
    {
        var o = DynValueToObject(value);
        _writeTag?.Invoke(name, o);
    }

    private static object? DynValueToObject(DynValue dv)
    {
        if (dv.IsNil()) return null;
        if (dv.Type == DataType.String) return dv.String;
        if (dv.Type == DataType.Number) return dv.Number;
        if (dv.Type == DataType.Boolean) return dv.Boolean;
        return dv.ToObject();
    }

    private void RegisterPrint()
    {
        if (_script == null) return;
        _script.Options.DebugPrint = s =>
        {
            _printCallback?.Invoke(s ?? "");
            System.Diagnostics.Trace.WriteLine("[Lua] " + (s ?? ""));
        };
    }

    public bool LoadScript(string filePath)
    {
        _lastError = "";
        if (string.IsNullOrEmpty(filePath))
        {
            _lastError = "File path is empty.";
            return false;
        }
        if (!File.Exists(filePath))
        {
            _lastError = "File not found: " + filePath;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        try
        {
            _loadedCode = File.ReadAllText(filePath);
            _loadedPath = filePath;
            ScriptLoaded?.Invoke(this, filePath);
            return true;
        }
        catch (Exception ex)
        {
            _lastError = "Load failed: " + ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
    }

    public bool ExecuteScript(string? arguments = null)
    {
        _lastError = "";
        EnsureScript();
        if (string.IsNullOrEmpty(_loadedPath) && string.IsNullOrEmpty(_loadedCode))
        {
            _lastError = "No script loaded.";
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        try
        {
            if (!string.IsNullOrEmpty(arguments))
                _script!.Globals["arg"] = arguments;

            if (!string.IsNullOrEmpty(_loadedCode))
                _script!.DoString(_loadedCode);

            ScriptExecuted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (ScriptRuntimeException ex)
        {
            _lastError = ex.DecoratedMessage ?? ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (SyntaxErrorException ex)
        {
            _lastError = ex.DecoratedMessage ?? ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
    }

    public bool ExecuteString(string luaCode)
    {
        _lastError = "";
        if (string.IsNullOrWhiteSpace(luaCode))
        {
            ScriptExecuted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        EnsureScript();
        try
        {
            _script!.DoString(luaCode);
            ScriptExecuted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (ScriptRuntimeException ex)
        {
            _lastError = ex.DecoratedMessage ?? ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (SyntaxErrorException ex)
        {
            _lastError = ex.DecoratedMessage ?? ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
    }

    public string GetLastError() => _lastError;

    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? ScriptLoaded;
    public event EventHandler? ScriptExecuted;
}
