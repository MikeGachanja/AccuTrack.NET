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
    /// <summary>Translation animation duration in seconds (for smooth interpolation).</summary>
    public double? TranslationDuration { get; set; }
    /// <summary>Translation start position X (for looping animations).</summary>
    public double? TranslationStartX { get; set; }
    /// <summary>Translation start position Y (for looping animations).</summary>
    public double? TranslationStartY { get; set; }
    /// <summary>Translation end position X (for looping animations).</summary>
    public double? TranslationEndX { get; set; }
    /// <summary>Translation end position Y (for looping animations).</summary>
    public double? TranslationEndY { get; set; }
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
    /// <summary>Component base position (X, Y) - used for translation animations to calculate offset.</summary>
    public double ComponentX { get; set; } = 0.0;
    public double ComponentY { get; set; } = 0.0;
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
                                    
                                    // Get basic properties first (needed for logging even if disabled)
                                    var typeStr = anim.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                                    var tagName = anim.TryGetProperty("tagName", out var tag) ? tag.GetString() ?? "" : "";
                                    var enabled = anim.TryGetProperty("enabled", out var en) ? en.GetBoolean() : true;
                                    
                                    if (!enabled)
                                    {
                                        string animName = anim.TryGetProperty("name", out var n) ? (n.GetString() ?? "unnamed") : "unnamed";
                                        System.Diagnostics.Trace.WriteLine($"[AnimationManager] Skipping disabled animation '{animName}' for tag '{tagName}'");
                                        continue;
                                    }
                                    
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
                                    if (anim.TryGetProperty("duration", out var duration))
                                        config["duration"] = duration.GetDouble();
                                    if (anim.TryGetProperty("speed", out var speed))
                                        config["speed"] = speed.GetDouble(); // Legacy: kept for backward compatibility
                                    if (anim.TryGetProperty("startX", out var startX))
                                        config["startX"] = startX.GetDouble();
                                    if (anim.TryGetProperty("startY", out var startY))
                                        config["startY"] = startY.GetDouble();
                                    if (anim.TryGetProperty("endX", out var endX))
                                        config["endX"] = endX.GetDouble();
                                    if (anim.TryGetProperty("endY", out var endY))
                                        config["endY"] = endY.GetDouble();
                                    
                                    // Get component position for translation animations
                                    double compX = comp.TryGetProperty("x", out var xProp) ? xProp.GetDouble() : 0.0;
                                    double compY = comp.TryGetProperty("y", out var yProp) ? yProp.GetDouble() : 0.0;
                                    
                                    // Try to get location from location object
                                    if (comp.TryGetProperty("location", out var loc))
                                    {
                                        if (loc.TryGetProperty("x", out var locX))
                                            compX = locX.GetDouble();
                                        if (loc.TryGetProperty("y", out var locY))
                                            compY = locY.GetDouble();
                                    }
                                    
                                    // For translation animations, use startX/startY as the component's base position
                                    // This ensures the component is positioned at StartX/StartY and translation is relative to that
                                    if (animType == AnimationType.Translation && config != null)
                                    {
                                        if (config.TryGetValue("startX", out var sxVal) && sxVal != null)
                                        {
                                            if (double.TryParse(sxVal.ToString(), out var startXVal))
                                                compX = startXVal;
                                        }
                                        if (config.TryGetValue("startY", out var syVal) && syVal != null)
                                        {
                                            if (double.TryParse(syVal.ToString(), out var startYVal))
                                                compY = startYVal;
                                        }
                                    }
                                    
                                    // Load color map for ColorChange animations
                                    if (animType == AnimationType.ColorChange && anim.TryGetProperty("colorMap", out var colorMapProp) && colorMapProp.ValueKind == JsonValueKind.Object)
                                    {
                                        var colorMap = new Dictionary<string, string>();
                                        foreach (var prop in colorMapProp.EnumerateObject())
                                        {
                                            string colorValue = prop.Value.GetString() ?? "#FF0000";
                                            // Normalize named colors to hex (e.g., "Red" -> "#FF0000")
                                            colorValue = NormalizeColorName(colorValue);
                                            colorMap[prop.Name] = colorValue;
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
                                        Config = config,
                                        ComponentX = compX,
                                        ComponentY = compY
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
                    // Get start and end points
                    var startX = rule.Config != null && rule.Config.TryGetValue("startX", out var sx) ? Convert.ToDouble(sx) : rule.ComponentX;
                    var startY = rule.Config != null && rule.Config.TryGetValue("startY", out var sy) ? Convert.ToDouble(sy) : rule.ComponentY;
                    var endX = rule.Config != null && rule.Config.TryGetValue("endX", out var ex) ? Convert.ToDouble(ex) : rule.ComponentX;
                    var endY = rule.Config != null && rule.Config.TryGetValue("endY", out var ey) ? Convert.ToDouble(ey) : rule.ComponentY;
                    
                    System.Diagnostics.Trace.WriteLine($"[AnimationManager] Translation: Component {rule.ComponentId}, Tag '{rule.TagName}' = {value}");
                    System.Diagnostics.Trace.WriteLine($"[AnimationManager] Translation: Component pos=({rule.ComponentX}, {rule.ComponentY}), Start=({startX}, {startY}), End=({endX}, {endY})");
                    
                    // Get tag value and normalize to 0-1 range for interpolation
                    // If tag is boolean: true = 1.0, false = 0.0
                    // If tag is numeric: normalize to 0-1 (clamp to 0-1 range)
                    double interpolationFactor = 0.0;
                    if (value is bool boolVal)
                    {
                        interpolationFactor = boolVal ? 1.0 : 0.0;
                    }
                    else
                    {
                        var tagValue = ValueToStubDouble(value, 0);
                        // Normalize: assume tag value of 0 = start point, 1 = end point
                        // For other ranges, user can scale their tag values accordingly
                        interpolationFactor = Math.Max(0.0, Math.Min(1.0, tagValue));
                    }
                    
                    // Get duration from config (total animation time in seconds) - default to 1.0 if not specified
                    // Backward compatibility: if duration not found, try to calculate from speed
                    double duration = 1.0;
                    if (rule.Config != null && rule.Config.TryGetValue("duration", out var dur))
                    {
                        duration = Convert.ToDouble(dur);
                    }
                    else if (rule.Config != null && rule.Config.TryGetValue("speed", out var sp))
                    {
                        // Legacy: calculate duration from speed and distance
                        double distance = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                        double speed = Convert.ToDouble(sp);
                        if (distance > 0 && speed > 0)
                        {
                            duration = distance / speed;
                        }
                    }
                    
                    System.Diagnostics.Trace.WriteLine($"[AnimationManager] Translation: Interpolation factor = {interpolationFactor}, Duration = {duration} seconds");
                    
                    // Calculate translation offset from component's base position (which should be at StartX/StartY)
                    // When interpolationFactor = 0: component should be at StartX/StartY (offset = 0)
                    // When interpolationFactor = 1: component should be at EndX/EndY (offset = End - Start)
                    // TranslationX/Y is the offset from component's base position (which is StartX/StartY)
                    // Note: Duration is stored for future smooth animation implementation (currently instant)
                    state.TranslationX = (endX - startX) * interpolationFactor;
                    state.TranslationY = (endY - startY) * interpolationFactor;
                    
                    // Store duration and positions in state for smooth animation and looping
                    state.TranslationDuration = duration;
                    state.TranslationStartX = startX;
                    state.TranslationStartY = startY;
                    state.TranslationEndX = endX;
                    state.TranslationEndY = endY;
                    
                    System.Diagnostics.Trace.WriteLine($"[AnimationManager] Translation: Calculated offset = ({state.TranslationX}, {state.TranslationY}), Duration = {duration} seconds");
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
    
    /// <summary>Normalizes color names to hex codes (e.g., "Red" -> "#FF0000").</summary>
    private static string NormalizeColorName(string color)
    {
        if (string.IsNullOrWhiteSpace(color)) return "#FF0000";
        var trimmed = color.Trim();
        
        // If it's already hex, return as-is
        if (trimmed.StartsWith("#")) return trimmed;
        
        // Convert named colors to hex
        return trimmed.ToLowerInvariant() switch
        {
            "black" => "#000000",
            "white" => "#FFFFFF",
            "gray" or "grey" => "#808080",
            "red" => "#FF0000",
            "green" => "#00FF00",
            "blue" => "#0000FF",
            "yellow" => "#FFFF00",
            "cyan" => "#00FFFF",
            "magenta" => "#FF00FF",
            "orange" => "#FFA500",
            "purple" => "#800080",
            "brown" => "#A52A2A",
            "darkgray" or "darkgrey" => "#A9A9A9",
            "lightgray" or "lightgrey" => "#D3D3D3",
            "transparent" => "#00000000",
            _ => trimmed // Return as-is if not recognized (might be hex without #)
        };
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
