using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.Screens;

/// <summary>State applied to a control by AnimationManager (visibility, opacity, color, flashing, translation).</summary>
public sealed class AnimationState
{
    public bool? Visible { get; set; }
    public double? Opacity { get; set; }
    public string? BackgroundColor { get; set; }
    public string? ForegroundColor { get; set; }
    /// <summary>Flashing (stub: on/off). View layer can drive blink from this.</summary>
    public bool? IsFlashing { get; set; }
    /// <summary>Translation offset X (stub). View layer can apply render transform.</summary>
    public double? TranslationX { get; set; }
    /// <summary>Translation offset Y (stub). View layer can apply render transform.</summary>
    public double? TranslationY { get; set; }
}

/// <summary>Animation rule: which tag drives which component and how (Visibility, ColorChange, Flashing, Translation).</summary>
public sealed class AnimationRule
{
    public string ScreenId { get; set; } = "";
    public string ComponentId { get; set; } = "";
    public string TagName { get; set; } = "";
    public AnimationType Type { get; set; }
    /// <summary>Optional JSON-like config (e.g. color map, thresholds). Stub: unused.</summary>
    public IReadOnlyDictionary<string, object>? Config { get; set; }
}

public enum AnimationType
{
    Visibility,
    ColorChange,
    Flashing,
    Translation
}

/// <summary>Manages animations from animations.json (visibility, color, etc.). Supports subscription so ScreenViewBuilder can apply state to controls.</summary>
public sealed class AnimationManager
{
    private bool _initialized;
    private readonly object _lock = new();
    private readonly List<(string screenId, string componentId, Action<AnimationState> callback)> _subscriptions = new();
    private readonly List<AnimationRule> _rules = new();
    private readonly Dictionary<string, AnimationState> _lastStateByKey = new();

    public bool IsInitialized => _initialized;

    public bool Initialize() { _initialized = true; return true; }

    /// <summary>Trigger initial animation states for all loaded rules using current tag values from TagManager.</summary>
    public void TriggerInitialStates(TagManager? tagManager)
    {
        if (tagManager == null) return;
        
        List<AnimationRule> rulesCopy;
        lock (_lock)
        {
            rulesCopy = _rules.ToList();
        }
        
        // Group by tag name and trigger evaluation
        var tagsToEvaluate = rulesCopy.Select(r => r.TagName).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var tagName in tagsToEvaluate)
        {
            var tag = tagManager.GetTag(tagName);
            if (tag != null)
            {
                var currentValue = tag.GetValue();
                OnTagValueChanged(tagName, currentValue);
            }
        }
    }

    /// <summary>Load animations for a screen from screen JSON component properties.</summary>
    public bool LoadScreenAnimations(string screenJsonPath, string screenId)
    {
        if (string.IsNullOrEmpty(screenJsonPath) || !File.Exists(screenJsonPath) || string.IsNullOrEmpty(screenId))
            return false;
        
        try
        {
            var json = File.ReadAllText(screenJsonPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            if (!root.TryGetProperty("components", out var compArr) || compArr.ValueKind != JsonValueKind.Array)
                return false;
            
            lock (_lock)
            {
                // Remove existing rules for this screen
                _rules.RemoveAll(r => r.ScreenId == screenId);
                
                foreach (var comp in compArr.EnumerateArray())
                {
                    var componentId = comp.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(componentId)) continue;
                    
                    // Check for animations in properties
                    if (comp.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
                    {
                        if (props.TryGetProperty("animations", out var animsProp))
                        {
                            // Handle nested animations object: { "animations": { "animations": [...] } }
                            JsonElement? animsArray = null;
                            if (animsProp.ValueKind == JsonValueKind.Object && animsProp.TryGetProperty("animations", out var nestedAnims))
                                animsArray = nestedAnims;
                            else if (animsProp.ValueKind == JsonValueKind.Array)
                                animsArray = animsProp;
                            
                            if (animsArray.HasValue && animsArray.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var anim in animsArray.Value.EnumerateArray())
                                {
                                    if (anim.ValueKind != JsonValueKind.Object) continue;
                                    
                                    var enabled = anim.TryGetProperty("enabled", out var en) ? en.GetBoolean() : true;
                                    if (!enabled) continue;
                                    
                                    var typeStr = anim.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                                    var tagName = anim.TryGetProperty("tagName", out var tag) ? tag.GetString() ?? "" : "";
                                    
                                    if (string.IsNullOrEmpty(typeStr) || string.IsNullOrEmpty(tagName)) continue;
                                    
                                    if (!Enum.TryParse<AnimationType>(typeStr, out var animType)) continue;
                                    
                                    // Extract config
                                    var config = new Dictionary<string, object>();
                                    if (anim.TryGetProperty("color", out var color))
                                        config["color"] = color.GetString() ?? "#00FF00";
                                    if (anim.TryGetProperty("bitValue", out var bitVal))
                                        config["bitValue"] = bitVal.GetBoolean();
                                    if (anim.TryGetProperty("frequency", out var freq))
                                        config["frequency"] = freq.GetDouble();
                                    if (anim.TryGetProperty("speed", out var speed))
                                        config["speed"] = speed.GetDouble();
                                    
                                    // Load color map for ColorChange animations
                                    if (animType == AnimationType.ColorChange && anim.TryGetProperty("colorMap", out var colorMapProp) && colorMapProp.ValueKind == JsonValueKind.Object)
                                    {
                                        var colorMap = new Dictionary<string, string>();
                                        foreach (var prop in colorMapProp.EnumerateObject())
                                        {
                                            colorMap[prop.Name] = prop.Value.GetString() ?? "#FF0000";
                                        }
                                        if (colorMap.Count > 0)
                                        {
                                            config["colorMap"] = colorMap;
                                            System.Diagnostics.Trace.WriteLine($"[AnimationManager] Loaded colorMap for {tagName}: {colorMap.Count} entries");
                                        }
                                    }
                                    
                                    _rules.Add(new AnimationRule
                                    {
                                        ScreenId = screenId,
                                        ComponentId = componentId,
                                        TagName = tagName,
                                        Type = animType,
                                        Config = config
                                    });
                                }
                            }
                        }
                    }
                }
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Add one Visibility rule per component that has a tag (tag value != 0 -> visible).</summary>
    public void AddImplicitVisibilityRules(string screenId, IEnumerable<(string componentId, string? tagName)> components)
    {
        if (string.IsNullOrEmpty(screenId)) return;
        lock (_lock)
        {
            foreach (var (componentId, tagName) in components)
            {
                if (string.IsNullOrEmpty(componentId) || string.IsNullOrEmpty(tagName)) continue;
                _rules.Add(new AnimationRule
                {
                    ScreenId = screenId,
                    ComponentId = componentId,
                    TagName = tagName,
                    Type = AnimationType.Visibility
                });
            }
        }
    }

    public void UnloadScreenAnimations(string screenId)
    {
        lock (_lock)
        {
            _subscriptions.RemoveAll(s => s.screenId == screenId);
            _rules.RemoveAll(r => r.ScreenId == screenId);
            foreach (var key in _lastStateByKey.Keys.Where(k => k.StartsWith(screenId + "\0", StringComparison.Ordinal)).ToList())
                _lastStateByKey.Remove(key);
        }
    }

    /// <summary>Return last animation state for a component (exposed to view layer).</summary>
    public IReadOnlyDictionary<string, object>? GetAnimationState(string screenId, int componentId, string animationName) => null;

    /// <summary>Get last computed AnimationState for (screenId, componentId). Exposed to view layer for controls.</summary>
    public AnimationState? GetLastState(string screenId, string componentId)
    {
        lock (_lock)
        {
            return _lastStateByKey.TryGetValue(StateKey(screenId, componentId), out var s) ? s : null;
        }
    }

    /// <summary>Called when a tag value changes; evaluates rules (Visibility, ColorChange, Flashing, Translation stubs) and notifies subscribed controls.</summary>
    public void OnTagValueChanged(string tagName, object? value)
    {
        List<AnimationRule> rulesForTag;
        lock (_lock)
        {
            rulesForTag = _rules.Where(r => string.Equals(r.TagName, tagName, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (rulesForTag.Count == 0) return;

        var stateByComponent = new Dictionary<string, AnimationState>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rulesForTag)
        {
            var key = StateKey(rule.ScreenId, rule.ComponentId);
            if (!stateByComponent.TryGetValue(key, out var state))
                stateByComponent[key] = state = new AnimationState();

            switch (rule.Type)
            {
                case AnimationType.Visibility:
                    var bitValue = rule.Config != null && rule.Config.TryGetValue("bitValue", out var bv) && bv is bool b ? b : true;
                    state.Visible = bitValue ? IsTrue(value) : !IsTrue(value);
                    break;
                case AnimationType.ColorChange:
                    // Check if colorMap exists (new table-based approach)
                    Dictionary<string, string>? colorMap = null;
                    if (rule.Config != null && rule.Config.TryGetValue("colorMap", out var cm))
                    {
                        // Handle both Dictionary<string, string> and object that can be cast
                        if (cm is Dictionary<string, string> dict)
                        {
                            colorMap = dict;
                            System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: Found colorMap (direct), {dict.Count} entries");
                        }
                        else if (cm is System.Collections.IDictionary idict)
                        {
                            // Convert IDictionary to Dictionary<string, string>
                            colorMap = new Dictionary<string, string>();
                            foreach (System.Collections.DictionaryEntry entry in idict)
                            {
                                string mapKey = entry.Key?.ToString() ?? "";
                                string mapVal = entry.Value?.ToString() ?? "#FF0000";
                                if (!string.IsNullOrEmpty(mapKey))
                                    colorMap[mapKey] = mapVal;
                            }
                            System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: Converted IDictionary to colorMap, {colorMap.Count} entries");
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: colorMap found but wrong type: {cm?.GetType().Name ?? "null"}");
                        }
                    }
                    
                    if (colorMap != null && colorMap.Count > 0)
                    {
                        // Convert value to string for lookup (boolean true->"1", false->"0")
                        string valueKey = ConvertValueToKey(value);
                        System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: Tag '{rule.TagName}' value '{value}' -> key '{valueKey}'");
                        
                        // Try exact match first
                        if (colorMap.TryGetValue(valueKey, out var mappedColor))
                        {
                            state.BackgroundColor = mappedColor;
                            System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: Exact match found: '{valueKey}' -> '{mappedColor}'");
                        }
                        else
                        {
                            // Try range matching for numeric values (e.g., "< 0", "0-50", "> 100")
                            var numericValue = ValueToStubDouble(value, 0);
                            string? matchedKey = null;
                            foreach (var rangeKey in colorMap.Keys)
                            {
                                if (MatchesRange(rangeKey, numericValue))
                                {
                                    matchedKey = rangeKey;
                                    break;
                                }
                            }
                            if (matchedKey != null && colorMap.TryGetValue(matchedKey, out var rangeColor))
                            {
                                state.BackgroundColor = rangeColor;
                                System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: Range match found: '{matchedKey}' -> '{rangeColor}'");
                            }
                            else if (colorMap.TryGetValue("default", out var defaultColor))
                            {
                                state.BackgroundColor = defaultColor;
                                System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: Using default color: '{defaultColor}'");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: No match found for key '{valueKey}', available keys: {string.Join(", ", colorMap.Keys)}");
                            }
                        }
                    }
                    else
                    {
                        // Legacy single color approach
                        var color = rule.Config != null && rule.Config.TryGetValue("color", out var c) ? c.ToString() : "#00FF00";
                        var colorBitValue = rule.Config != null && rule.Config.TryGetValue("bitValue", out var cbv) && cbv is bool cb ? cb : true;
                        state.BackgroundColor = (colorBitValue ? IsTrue(value) : !IsTrue(value)) ? color : null;
                        System.Diagnostics.Trace.WriteLine($"[AnimationManager] ColorChange: Using legacy approach, color: '{state.BackgroundColor}'");
                    }
                    break;
                case AnimationType.Flashing:
                    var flashColor = rule.Config != null && rule.Config.TryGetValue("color", out var fc) ? fc.ToString() : "#FF0000";
                    var flashFrequency = rule.Config != null && rule.Config.TryGetValue("frequency", out var ff) ? Convert.ToDouble(ff) : 1.0;
                    if (IsTrue(value))
                    {
                        state.IsFlashing = true;
                        state.BackgroundColor = flashColor;
                        // Store frequency for flashing animation (could be used by view layer for timing)
                        if (state.Opacity == null) state.Opacity = flashFrequency;
                    }
                    else
                    {
                        state.IsFlashing = false;
                    }
                    break;
                case AnimationType.Translation:
                    var speed = rule.Config != null && rule.Config.TryGetValue("speed", out var sp) ? Convert.ToDouble(sp) : 1.0;
                    var translationValue = ValueToStubDouble(value, 0);
                    state.TranslationX = translationValue * speed;
                    state.TranslationY = translationValue * speed;
                    break;
            }
        }

        foreach (var (key, state) in stateByComponent)
        {
            var (screenId, componentId) = ParseStateKey(key);
            lock (_lock)
            {
                _lastStateByKey[key] = state;
            }
            NotifyState(screenId, componentId, state);
        }
    }

    private static string StateKey(string screenId, string componentId) => screenId + "\0" + componentId;
    private static (string screenId, string componentId) ParseStateKey(string key)
    {
        var i = key.IndexOf('\0');
        return i >= 0 ? (key.Substring(0, i), key.Substring(i + 1)) : (key, "");
    }

    private static bool IsTrue(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is double d) return d != 0;
        if (value is float f) return f != 0;
        return value.ToString()?.Trim() is string s && s.Length > 0 && !s.Equals("0", StringComparison.Ordinal);
    }


    private static double ValueToStubDouble(object? value, double defaultVal)
    {
        if (value == null) return defaultVal;
        if (value is int i) return i;
        if (value is double d) return d;
        if (value is float f) return f;
        return double.TryParse(value.ToString(), out var n) ? n : defaultVal;
    }
    
    /// <summary>Converts a tag value to a string key for color map lookup. Boolean true->"1", false->"0".</summary>
    private static string ConvertValueToKey(object? value)
    {
        if (value == null) return "0";
        if (value is bool b) return b ? "1" : "0";
        if (value is int i) return i.ToString();
        if (value is double d) return d.ToString();
        if (value is float f) return f.ToString();
        var str = value.ToString()?.Trim() ?? "";
        // Handle boolean strings
        if (str.Equals("true", StringComparison.OrdinalIgnoreCase)) return "1";
        if (str.Equals("false", StringComparison.OrdinalIgnoreCase)) return "0";
        return str;
    }
    
    /// <summary>Checks if a numeric value matches a range pattern (e.g., "< 0", "0-50", "> 100").</summary>
    private static bool MatchesRange(string rangeKey, double value)
    {
        if (string.IsNullOrEmpty(rangeKey)) return false;
        
        // Handle "< value" pattern
        if (rangeKey.StartsWith("<", StringComparison.Ordinal))
        {
            var numStr = rangeKey.Substring(1).Trim();
            if (double.TryParse(numStr, out var threshold))
                return value < threshold;
        }
        // Handle "> value" pattern
        else if (rangeKey.StartsWith(">", StringComparison.Ordinal))
        {
            var numStr = rangeKey.Substring(1).Trim();
            if (double.TryParse(numStr, out var threshold))
                return value > threshold;
        }
        // Handle "min-max" pattern
        else if (rangeKey.Contains("-"))
        {
            var parts = rangeKey.Split('-');
            if (parts.Length == 2)
            {
                if (double.TryParse(parts[0].Trim(), out var min) && double.TryParse(parts[1].Trim(), out var max))
                    return value >= min && value <= max;
            }
        }
        // Exact match
        else if (double.TryParse(rangeKey, out var exact))
        {
            return Math.Abs(value - exact) < 0.0001; // Small epsilon for floating point comparison
        }
        
        return false;
    }

    /// <summary>Subscribe to animation state for a component. Callback is invoked on the thread that calls NotifyState or OnTagValueChanged; host should marshal to UI thread.</summary>
    public IDisposable Subscribe(string screenId, string componentId, Action<AnimationState> callback)
    {
        if (string.IsNullOrEmpty(screenId) || string.IsNullOrEmpty(componentId) || callback == null)
            return new SubscriptionToken(() => { });
        lock (_lock)
        {
            _subscriptions.Add((screenId, componentId, callback));
            
            // Send initial state if available
            var key = StateKey(screenId, componentId);
            if (_lastStateByKey.TryGetValue(key, out var lastState))
            {
                try { callback(lastState); }
                catch { }
            }
        }
        return new SubscriptionToken(() =>
        {
            lock (_lock)
            {
                _subscriptions.RemoveAll(s => s.screenId == screenId && s.componentId == componentId && s.callback == callback);
            }
        });
    }

    /// <summary>Notify one component's animation state (e.g. after evaluating rules). Stub integration point.</summary>
    public void NotifyState(string screenId, string componentId, AnimationState state)
    {
        List<Action<AnimationState>>? copy;
        lock (_lock)
        {
            copy = _subscriptions
                .Where(s => s.screenId == screenId && s.componentId == componentId)
                .Select(s => s.callback)
                .ToList();
        }
        foreach (var cb in copy)
        {
            try { cb(state); }
            catch { }
        }
    }

    private sealed class SubscriptionToken : IDisposable
    {
        private Action? _onDispose;
        public SubscriptionToken(Action onDispose) { _onDispose = onDispose; }
        public void Dispose() { _onDispose?.Invoke(); _onDispose = null; }
    }
}
