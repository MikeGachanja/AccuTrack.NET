using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Runtime.Modules.Screens;

/// <summary>Loads events from events.json and executes actions (NavigateScreen, WriteTag, etc.).</summary>
public sealed class EventManager
{
    private readonly Dictionary<string, ScadaEvent> _events = new();
    private string _eventsJsonPath = "";
    private string _projectDataPath = "";
    private bool _initialized;
    private ScreenManager? _screenManager;
    private object? _tagIOHandler; // ITagIOHandler or TagIOHandler from TagsEngine
    private object? _tagManager;
    private object? _scriptManager;

    public bool IsInitialized => _initialized;

    public void SetScreenManager(ScreenManager? screenManager) => _screenManager = screenManager;
    public void SetTagIOHandler(object? handler) => _tagIOHandler = handler;
    public void SetTagManager(object? tagManager) => _tagManager = tagManager;
    public void SetScriptManager(object? scriptManager) => _scriptManager = scriptManager;

    public bool Initialize(string eventsJsonPath, string projectDataPath)
    {
        _eventsJsonPath = eventsJsonPath ?? "";
        _projectDataPath = projectDataPath ?? "";
        if (!LoadEventsFromJson(_eventsJsonPath))
            return false;
        _initialized = true;
        return true;
    }

    public bool HandleEvent(string eventId, int componentId, string triggerType)
        => HandleEvent(eventId, componentId.ToString(), triggerType);

    public bool HandleEvent(string eventId, string componentId, string triggerType)
    {
        if (!_initialized || !_events.TryGetValue(eventId, out var evt))
            return false;
        if (string.IsNullOrEmpty(componentId) || evt.Trigger.ComponentId != componentId)
            return false;
        var normalized = NormalizeTriggerType(triggerType);
        if (NormalizeTriggerType(evt.Trigger.Type) != normalized)
            return false;
        foreach (var action in evt.Actions)
        {
            if (!ExecuteAction(action))
                return false;
        }
        return true;
    }

    /// <summary>Fires all events whose trigger matches the given component and trigger type (e.g. "click").</summary>
    public void FireTrigger(string componentId, string triggerType)
    {
        if (!_initialized || string.IsNullOrEmpty(componentId)) return;
        var normalized = NormalizeTriggerType(triggerType);
        foreach (var kv in _events)
        {
            if (kv.Value.Trigger.ComponentId != componentId) continue;
            if (NormalizeTriggerType(kv.Value.Trigger.Type) != normalized) continue;
            foreach (var action in kv.Value.Actions)
                ExecuteAction(action);
        }
    }

    public void ReloadEvents() => LoadEventsFromJson(_eventsJsonPath);

    private bool LoadEventsFromJson(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("events", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return false;
            _events.Clear();
            foreach (var item in arr.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var i) ? i.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(id)) continue;
                var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var desc = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                var triggerObj = item.TryGetProperty("trigger", out var t) ? t : default;
                var componentId = triggerObj.TryGetProperty("componentId", out var c) ? c.GetString() ?? "" : "";
                var type = triggerObj.TryGetProperty("type", out var ty) ? ty.GetString() ?? "" : "";
                var trigger = new EventTrigger { ComponentId = componentId, Type = type };
                var actions = new List<EventAction>();
                if (item.TryGetProperty("actions", out var actionsArr) && actionsArr.ValueKind == JsonValueKind.Array)
                    foreach (var a in actionsArr.EnumerateArray())
                        actions.Add(new EventAction
                        {
                            Type = a.TryGetProperty("type", out var at) ? at.GetString() ?? "" : "",
                            ScreenId = a.TryGetProperty("screenId", out var si) ? si.GetString() ?? "" : "",
                            Tag = a.TryGetProperty("tag", out var tg) ? tg.GetString() ?? "" : "",
                            Value = a.TryGetProperty("value", out var v) ? v.GetString() ?? "" : "",
                            Script = a.TryGetProperty("script", out var sc) ? sc.GetString() ?? "" : "",
                            Message = a.TryGetProperty("message", out var m) ? m.GetString() ?? "" : ""
                        });
                _events[id] = new ScadaEvent { Id = id, Name = name, Description = desc, Trigger = trigger, Actions = actions };
            }
            return true;
        }
        catch { return false; }
    }

    private static string NormalizeTriggerType(string? s)
    {
        if (s == null) return "";
        return s.Replace(" ", "", StringComparison.Ordinal);
    }

    private bool ExecuteAction(EventAction action)
    {
        switch (action.Type)
        {
            case "NavigateScreen":
                return NavigateToScreen(action.ScreenId);
            case "WriteTag":
                return WriteTag(action.Tag, action.Value);
            case "SetBit":
                return WriteTag(action.Tag, "1");
            case "ResetBit":
                return WriteTag(action.Tag, "0");
            case "ToggleBit":
                return ToggleBit(action.Tag);
            case "RunScript":
                return RunScript(action.Script);
            case "ShowMessage":
                return true; // Stub
            default:
                return false;
        }
    }

    private bool NavigateToScreen(string screenId)
    {
        if (_screenManager == null || string.IsNullOrEmpty(screenId)) return false;
        var path = string.IsNullOrEmpty(_projectDataPath) ? "" : Path.Combine(_projectDataPath, "screens", screenId + ".json");
        if (!File.Exists(path))
            path = Path.Combine(_projectDataPath, "screens", screenId);
        _screenManager.LoadScreen(path);
        return true;
    }

    private bool WriteTag(string tagName, string value)
    {
        if (_tagIOHandler == null) return false;
        // TagIOHandler is from TagsEngine - we need to call WriteTag(tagName, value)
        var method = _tagIOHandler.GetType().GetMethod("WriteTag", new[] { typeof(string), typeof(object) });
        if (method == null) return false;
        try
        {
            var result = method.Invoke(_tagIOHandler, new object[] { tagName, value });
            return result is true;
        }
        catch { return false; }
    }

    private bool ToggleBit(string tagName)
    {
        if (_tagManager == null) return false;
        var getMethod = _tagManager.GetType().GetMethod("GetTagValue", new[] { typeof(string) });
        if (getMethod == null) return false;
        try
        {
            var current = getMethod.Invoke(_tagManager, new object[] { tagName });
            var next = (current is 0 or null or false) ? 1 : 0;
            return WriteTag(tagName, next.ToString());
        }
        catch { return false; }
    }

    private bool RunScript(string scriptPath)
    {
        if (_scriptManager == null) return false;
        var load = _scriptManager.GetType().GetMethod("LoadScript", new[] { typeof(string) });
        var exec = _scriptManager.GetType().GetMethod("ExecuteScript");
        if (load == null || exec == null) return false;
        try
        {
            load.Invoke(_scriptManager, new object[] { scriptPath ?? "" });
            return exec.Invoke(_scriptManager, null) is true;
        }
        catch { return false; }
    }

    private sealed class ScadaEvent
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public EventTrigger Trigger { get; set; } = new();
        public List<EventAction> Actions { get; set; } = new();
    }

    private sealed class EventTrigger
    {
        public string ComponentId { get; set; } = "";
        public string Type { get; set; } = "";
    }

    private sealed class EventAction
    {
        public string Type { get; set; } = "";
        public string ScreenId { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Value { get; set; } = "";
        public string Script { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
