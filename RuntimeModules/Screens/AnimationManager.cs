using System;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>Load animations for a screen (stub: optional animations.json; use AddImplicitVisibilityRules for tag-bound visibility).</summary>
    public bool LoadScreenAnimations(string screenJsonPath, string screenId) => true;

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
                    state.Visible = IsTrue(value);
                    break;
                case AnimationType.ColorChange:
                    state.BackgroundColor = ValueToStubColor(value);
                    break;
                case AnimationType.Flashing:
                    state.IsFlashing = IsTrue(value);
                    break;
                case AnimationType.Translation:
                    state.TranslationX = ValueToStubDouble(value, 0);
                    state.TranslationY = ValueToStubDouble(value, 0);
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

    private static string? ValueToStubColor(object? value)
    {
        if (value == null) return null;
        return IsTrue(value) ? "#00FF00" : "#808080";
    }

    private static double ValueToStubDouble(object? value, double defaultVal)
    {
        if (value == null) return defaultVal;
        if (value is int i) return i;
        if (value is double d) return d;
        if (value is float f) return f;
        return double.TryParse(value.ToString(), out var n) ? n : defaultVal;
    }

    /// <summary>Subscribe to animation state for a component. Callback is invoked on the thread that calls NotifyState or OnTagValueChanged; host should marshal to UI thread.</summary>
    public IDisposable Subscribe(string screenId, string componentId, Action<AnimationState> callback)
    {
        if (string.IsNullOrEmpty(screenId) || string.IsNullOrEmpty(componentId) || callback == null)
            return new SubscriptionToken(() => { });
        lock (_lock)
        {
            _subscriptions.Add((screenId, componentId, callback));
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
