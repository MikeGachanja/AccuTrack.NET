using System;
using System.Collections.Generic;
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
    public void SetPrintCallback(Action<string>? callback)
    {
        _printCallback = callback;
        // Re-register print to ensure the callback is used if script already exists
        if (_script != null)
        {
            RegisterPrint();
            System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] SetPrintCallback: Callback {(callback != null ? "set" : "cleared")}, re-registered print");
        }
    }

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
        else
        {
            // Ensure print is registered even if script already exists (callback might have been set after script creation)
            RegisterPrint();
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
        
        System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] RegisterPrint: Callback is {(_printCallback != null ? "set" : "null")}");
        
        // Set DebugPrint option to capture print() calls (MoonSharp's primary print mechanism)
        _script.Options.DebugPrint = s =>
        {
            string message = s ?? "";
            bool callbackIsSet = _printCallback != null;
            System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] DebugPrint: Received message='{message}', callback is {(callbackIsSet ? "SET" : "NULL")}");
            if (callbackIsSet)
            {
                try
                {
                    _printCallback.Invoke(message);
                    System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] DebugPrint: Successfully invoked callback");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] DebugPrint: ERROR invoking callback: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("[ScriptingEngine] DebugPrint: WARNING - Callback is null, message will NOT appear in console!");
            }
            System.Diagnostics.Trace.WriteLine("[Lua] " + message);
        };
        
        // Also register print as a global function to ensure all print calls are captured
        // This overrides MoonSharp's default print implementation
        _script.Globals["print"] = (Action<DynValue[]>)PrintImpl;
        
        System.Diagnostics.Trace.WriteLine("[ScriptingEngine] RegisterPrint: Print function registered");
    }
    
    private void PrintImpl(params DynValue[] args)
    {
        bool callbackIsSet = _printCallback != null;
        
        if (args == null || args.Length == 0)
        {
            var emptyMsg = "";
            System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] PrintImpl: Empty print call, callback is {(callbackIsSet ? "SET" : "NULL")}");
            if (callbackIsSet)
            {
                try
                {
                    _printCallback.Invoke(emptyMsg);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] PrintImpl: ERROR invoking callback: {ex.Message}");
                }
            }
            System.Diagnostics.Trace.WriteLine("[Lua] ");
            return;
        }
        
        // Format multiple arguments similar to Lua's print behavior
        var parts = new List<string>();
        foreach (var arg in args)
        {
            if (arg.IsNil())
                parts.Add("nil");
            else if (arg.Type == DataType.String)
                parts.Add(arg.String);
            else if (arg.Type == DataType.Number)
                parts.Add(arg.Number.ToString(System.Globalization.CultureInfo.InvariantCulture));
            else if (arg.Type == DataType.Boolean)
                parts.Add(arg.Boolean ? "true" : "false");
            else
                parts.Add(arg.ToString());
        }
        
        string message = string.Join("\t", parts);
        System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] PrintImpl: Message='{message}', callback is {(callbackIsSet ? "SET" : "NULL")}");
        if (callbackIsSet)
        {
            try
            {
                _printCallback.Invoke(message);
                System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] PrintImpl: Successfully invoked callback");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] PrintImpl: ERROR invoking callback: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Trace.WriteLine("[ScriptingEngine] PrintImpl: WARNING - Callback is null, message will NOT appear in console!");
        }
        System.Diagnostics.Trace.WriteLine("[Lua] " + message);
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
            
            // Ensure script instance exists and print is registered before loading code
            EnsureScript();
            System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] LoadScript: Loaded script from {filePath}, callback={(_printCallback != null ? "set" : "null")}");
            
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
            // Ensure print function is registered before execution
            RegisterPrint();
            bool callbackIsSet = _printCallback != null;
            System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] ExecuteScript: About to execute, print callback is {(callbackIsSet ? "SET" : "NULL")}");
            if (!callbackIsSet)
            {
                System.Diagnostics.Trace.WriteLine("[ScriptingEngine] ExecuteScript: WARNING - Print callback is null! Lua print() output will NOT appear in console.");
            }
            
            if (!string.IsNullOrEmpty(arguments))
                _script!.Globals["arg"] = arguments;

            if (!string.IsNullOrEmpty(_loadedCode))
            {
                System.Diagnostics.Trace.WriteLine($"[ScriptingEngine] ExecuteScript: Executing script code ({_loadedCode.Length} chars)");
                _script!.DoString(_loadedCode);
            }

            ScriptExecuted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (ScriptRuntimeException ex)
        {
            _lastError = ex.DecoratedMessage ?? ex.Message;
            string errorMsg = $"[Lua Error] {_lastError}";
            _printCallback?.Invoke(errorMsg);
            System.Diagnostics.Trace.WriteLine(errorMsg);
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (SyntaxErrorException ex)
        {
            _lastError = ex.DecoratedMessage ?? ex.Message;
            string errorMsg = $"[Lua Syntax Error] {_lastError}";
            _printCallback?.Invoke(errorMsg);
            System.Diagnostics.Trace.WriteLine(errorMsg);
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            string errorMsg = $"[Lua Exception] {_lastError}";
            _printCallback?.Invoke(errorMsg);
            System.Diagnostics.Trace.WriteLine(errorMsg);
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
            string errorMsg = $"[Lua Error] {_lastError}";
            _printCallback?.Invoke(errorMsg);
            System.Diagnostics.Trace.WriteLine(errorMsg);
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (SyntaxErrorException ex)
        {
            _lastError = ex.DecoratedMessage ?? ex.Message;
            string errorMsg = $"[Lua Syntax Error] {_lastError}";
            _printCallback?.Invoke(errorMsg);
            System.Diagnostics.Trace.WriteLine(errorMsg);
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            string errorMsg = $"[Lua Exception] {_lastError}";
            _printCallback?.Invoke(errorMsg);
            System.Diagnostics.Trace.WriteLine(errorMsg);
            ErrorOccurred?.Invoke(this, _lastError);
            return false;
        }
    }

    public string GetLastError() => _lastError;

    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<string>? ScriptLoaded;
    public event EventHandler? ScriptExecuted;
}
