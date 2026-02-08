using System.Text.Json;
using AccuTrack.Runtime.Tags;

namespace AccuTrack.Runtime.Screens;

/// <summary>
/// Register events from screen model (OnClick → script, Navigate, WriteTag); run action via Script or TagIOHandler.
/// Initialize from events.json (Plan 2 §13).
/// </summary>
public sealed class EventManager
{
    private TagIOHandler? _tagIOHandler;
    private TagManager? _tagManager;
    private Func<string, object?[]?, Task>? _runScript;

    public void SetTagIOHandler(TagIOHandler? handler) => _tagIOHandler = handler;
    public void SetTagManager(TagManager? manager) => _tagManager = manager;
    public void SetScriptRunner(Func<string, object?[]?, Task>? runScript) => _runScript = runScript;

    /// <summary>
    /// Load event bindings from events.json (Designer output, Plan 1 §17). Schema: array of { controlId, action, parameters }.
    /// </summary>
    public void LoadFromConfig(JsonElement? config)
    {
        if (config == null || config.Value.ValueKind != JsonValueKind.Array) return;
        // Stub: register event bindings when schema is defined; for now just accept config presence
        foreach (var _ in config.Value.EnumerateArray()) { }
    }

    public void OnClick(string? action, IReadOnlyDictionary<string, string>? parameters)
    {
        if (string.IsNullOrEmpty(action)) return;
        if (string.Equals(action, "WriteTag", StringComparison.OrdinalIgnoreCase) && parameters != null)
        {
            if (parameters.TryGetValue("tag", out var tag) && parameters.TryGetValue("value", out var value) && _tagIOHandler != null)
                _tagIOHandler.Write(tag, value);
            return;
        }
        if (string.Equals(action, "Navigate", StringComparison.OrdinalIgnoreCase) && parameters != null)
        {
            if (parameters.TryGetValue("screen", out var screen))
                NavigateRequested?.Invoke(this, screen);
            return;
        }
        if (string.Equals(action, "RunScript", StringComparison.OrdinalIgnoreCase) && parameters != null)
        {
            if (parameters.TryGetValue("script", out var script) && _runScript != null)
                _ = _runScript(script, null);
            return;
        }
    }

    public event EventHandler<string>? NavigateRequested;
}
