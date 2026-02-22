using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Runtime.Modules.Console;
using Runtime.Modules.Screens;
using Runtime.Modules.TagsEngine;
using Runtime.Views.Controls;

namespace Runtime;

/// <summary>Tracks translation animation state for smooth interpolation.</summary>
internal sealed class TranslationAnimationTracker
{
    public double StartX { get; set; } // Position when animation started (point A)
    public double StartY { get; set; }
    public double EndX { get; set; } // End position (point B) - for looping
    public double EndY { get; set; }
    public double CurrentX { get; set; } // Current position (updated each frame)
    public double CurrentY { get; set; }
    public double TargetX { get; set; }
    public double TargetY { get; set; }
    public DateTime StartTime { get; set; }
    public double Duration { get; set; } = 1.0; // seconds
    public bool IsAnimating { get; set; }
    public bool ShouldLoop { get; set; } // Whether to loop back to start when reaching end
}

/// <summary>Builds an Avalonia view from a screen descriptor (parsed JSON).</summary>
public static class ScreenViewBuilder
{
    // Track translation animations for smooth interpolation
    private static readonly Dictionary<Control, TranslationAnimationTracker> _translationTrackers = new();
    private static Avalonia.Threading.DispatcherTimer? _animationTimer;
    public static Control? Build(ScreenRenderer.ScreenDescriptor? screen)
        => Build(screen, null, null, null, null, null, null, null, null);

    /// <summary>Builds view and wires tag bindings and button events when services are provided.</summary>
    /// <param name="resolveImagePath">Optional resolver for image names (e.g. ScreenManager.ResolveImagePath).</param>
    /// <param name="resolveSvgPath">Optional resolver for SVG paths (e.g. ScreenManager.ResolveSvgPath). If null, uses resolveImagePath.</param>
    /// <param name="animationManager">Optional; when set, subscribes each control with an Id to animation state (visibility, opacity, color).</param>
    /// <param name="console">Optional; when set, Console components on the screen will display log output from this service.</param>
    /// <param name="historianQuery">Optional; when set, Table and TrendView can display historian data.</param>
    public static Control? Build(ScreenRenderer.ScreenDescriptor? screen, Runtime.Modules.TagsEngine.TagManager? tagManager, EventManager? eventManager, TagIOHandler? tagIOHandler = null, Func<string, string?>? resolveImagePath = null, Func<string, string?>? resolveSvgPath = null, AnimationManager? animationManager = null, IConsole? console = null, Runtime.Modules.Screens.HistorianQueryHelper? historianQuery = null)
    {
        if (screen == null) return null;
        var subs = new List<IDisposable>();
        var screenId = screen.Id ?? screen.Name ?? "";
        // Force light background - override dark colors
        var bgColor = screen.BackgroundColor ?? "#FFFFFF";
        if (IsDarkColor(bgColor))
        {
            bgColor = "#FFFFFF"; // Force white for dark colors
        }
        
        var canvas = new Canvas
        {
            Width = screen.Width,
            Height = screen.Height,
            Background = ParseBrush(bgColor)
        };
        var sorted = new List<ComponentDescriptor>(screen.Components);
        sorted.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
        foreach (var comp in sorted)
        {
            var control = CreateControl(comp, tagManager, eventManager, tagIOHandler, resolveImagePath, resolveSvgPath, subs, console, historianQuery);
            if (control == null) continue;
            
            // Set size constraints BEFORE adding to canvas to ensure they're respected
            // Use explicit Width/Height, MinWidth/MinHeight, and MaxWidth/MaxHeight to ensure controls maintain their exact size
            // For Numeric components, ensure minimum size so they're always visible
            var minSize = comp.ComponentType == "Numeric" ? 50 : 1;
            var width = Math.Max(minSize, comp.Width);
            var height = comp.ComponentType == "Numeric" ? Math.Max(20, comp.Height) : Math.Max(1, comp.Height);
            control.Width = width;
            control.Height = height;
            control.MinWidth = width;
            control.MinHeight = height;
            control.MaxWidth = width;
            control.MaxHeight = height;
            
            // Check if component has a translation animation - if so, position at start point
            // Note: AnimationManager will handle the actual animation, but we position component at start point
            double componentX = comp.X;
            double componentY = comp.Y;
            if (animationManager != null && comp.Properties != null && comp.Properties.TryGetValue("animations", out var animsObj))
            {
                try
                {
                    // Properties stores animations as a string (JSON) or object
                    string? animsJsonStr = null;
                    if (animsObj is string str)
                        animsJsonStr = str;
                    else if (animsObj != null)
                        animsJsonStr = animsObj.ToString();
                    
                    if (!string.IsNullOrEmpty(animsJsonStr))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(animsJsonStr);
                        var root = doc.RootElement;
                        System.Text.Json.JsonElement? animsArray = null;
                        if (root.ValueKind == System.Text.Json.JsonValueKind.Object && root.TryGetProperty("animations", out var nestedAnims))
                            animsArray = nestedAnims;
                        else if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                            animsArray = root;
                        
                        if (animsArray.HasValue)
                        {
                            foreach (var anim in animsArray.Value.EnumerateArray())
                            {
                                if (anim.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    var animType = anim.TryGetProperty("type", out var t) ? t.GetString() : "";
                                    var enabled = anim.TryGetProperty("enabled", out var en) ? en.GetBoolean() : true;
                                    if (animType == "Translation" && enabled)
                                    {
                                        // Found translation animation - use start point as component position
                                        if (anim.TryGetProperty("startX", out var startX))
                                            componentX = startX.GetDouble();
                                        if (anim.TryGetProperty("startY", out var startY))
                                            componentY = startY.GetDouble();
                                        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Translation animation found for component {comp.Id}, positioning at ({componentX}, {componentY})");
                                        break; // Use first translation animation's start point
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Error parsing animations for component {comp.Id}: {ex.Message}");
                }
            }
            
            Canvas.SetLeft(control, componentX);
            Canvas.SetTop(control, componentY);
            canvas.Children.Add(control);
            
            // For SVG components, ensure minimum size and trigger a re-render after control is added and sized
            if (comp.ComponentType == "SVGView" && control is RuntimeSVGView svgView)
            {
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ===== Post-processing SVG component =====");
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Initial size: {width}x{height}");
                
                // Ensure SVG component has minimum size so it's visible
                if (width < 50) width = 50;
                if (height < 50) height = 50;
                control.Width = width;
                control.Height = height;
                control.MinWidth = width;
                control.MinHeight = height;
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Set control size to: {width}x{height}");
                
                // Delay SVG loading to ensure control has proper size - use multiple attempts if needed
                void TryLoadSvg(int attempt = 0)
                {
                    System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] TryLoadSvg attempt {attempt + 1}");
                    var currentPath = svgView.GetCurrentSvgPath();
                    System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Current SVG path: '{currentPath}'");
                    System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Control size: {svgView.Width}x{svgView.Height}");
                    
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        if (svgView.Width > 0 && svgView.Height > 0)
                        {
                            // Control has size - load SVG
                            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ✓ Control has size, re-loading SVG");
                            svgView.SetSvgPath(currentPath);
                        }
                        else if (attempt < 5)
                        {
                            // Control doesn't have size yet - try again after a short delay (max 5 attempts)
                            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Control size is 0, retrying in next frame (attempt {attempt + 1}/5)...");
                            Avalonia.Threading.Dispatcher.UIThread.Post(() => TryLoadSvg(attempt + 1), Avalonia.Threading.DispatcherPriority.Loaded);
                        }
                        else
                        {
                            // Load anyway with default size - SizeChanged will re-render when size is available
                            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Max attempts reached, loading SVG with default size");
                            svgView.SetSvgPath(currentPath);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ✗ No SVG path available to load");
                    }
                }
                Avalonia.Threading.Dispatcher.UIThread.Post(() => TryLoadSvg(), Avalonia.Threading.DispatcherPriority.Loaded);
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ===== Finished post-processing SVG component =====");
            }
            
            if (animationManager != null && !string.IsNullOrEmpty(screenId) && !string.IsNullOrEmpty(comp.Id))
            {
                var c = control;
                var sub = animationManager.Subscribe(screenId, comp.Id, state =>
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyAnimationState(c, state)));
                subs.Add(sub);
            }
        }
        var scroll = new ScrollViewer
        {
            Content = canvas,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        scroll.Tag = subs;
        
        // Start animation timer if not already running
        if (_animationTimer == null)
        {
            _animationTimer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _animationTimer.Tick += (s, e) => UpdateTranslationAnimations();
            _animationTimer.Start();
        }
        
        return scroll;
    }
    
    private static void UpdateTranslationAnimations()
    {
        var now = DateTime.Now;
        var controlsToRemove = new List<Control>();
        
        lock (_translationTrackers)
        {
            foreach (var kvp in _translationTrackers)
            {
                var control = kvp.Key;
                var tracker = kvp.Value;
                
                if (!tracker.IsAnimating)
                {
                    continue;
                }
                
                var elapsed = (now - tracker.StartTime).TotalSeconds;
                var progress = Math.Min(1.0, elapsed / tracker.Duration);
                
                // Linear interpolation from start position to target
                var currentX = tracker.StartX + (tracker.TargetX - tracker.StartX) * progress;
                var currentY = tracker.StartY + (tracker.TargetY - tracker.StartY) * progress;
                
                // Update control position
                control.RenderTransform = new Avalonia.Media.TranslateTransform(currentX, currentY);
                
                // Update tracker current position
                tracker.CurrentX = currentX;
                tracker.CurrentY = currentY;
                
                // Check if animation is complete
                if (progress >= 1.0)
                {
                    tracker.CurrentX = tracker.TargetX;
                    tracker.CurrentY = tracker.TargetY;
                    control.RenderTransform = new Avalonia.Media.TranslateTransform(tracker.TargetX, tracker.TargetY);
                    
                    // If looping and we've reached the end, reset to start and begin again
                    if (tracker.ShouldLoop && Math.Abs(tracker.CurrentX - tracker.EndX) < 0.01 && Math.Abs(tracker.CurrentY - tracker.EndY) < 0.01)
                    {
                        // Immediately reset to start position
                        tracker.CurrentX = tracker.StartX;
                        tracker.CurrentY = tracker.StartY;
                        tracker.TargetX = tracker.EndX;
                        tracker.TargetY = tracker.EndY;
                        tracker.StartTime = DateTime.Now; // Restart timer
                        tracker.IsAnimating = true; // Continue animating
                        control.RenderTransform = new Avalonia.Media.TranslateTransform(tracker.StartX, tracker.StartY);
                        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Translation loop: Reset to start ({tracker.StartX}, {tracker.StartY}), restarting animation");
                    }
                    else
                    {
                        // Animation complete, stop
                        tracker.IsAnimating = false;
                    }
                }
            }
        }
    }

    private static Control? CreateControl(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, EventManager? eventManager, TagIOHandler? tagIOHandler, Func<string, string?>? resolveImagePath, Func<string, string?>? resolveSvgPath, List<IDisposable> subs, IConsole? console = null, Runtime.Modules.Screens.HistorianQueryHelper? historianQuery = null)
    {
        if (!d.Visible) return null;
        var type = d.ComponentType ?? "";
        Control? c = type switch
        {
            "Button" => CreateButton(d, eventManager, console),
            "TextLabel" => CreateTextLabel(d, tagManager, subs),
            "GaugeView" => CreateGaugeView(d, tagManager, subs),
            "Checkbox" => CreateCheckbox(d, tagManager, tagIOHandler, eventManager, subs),
            "ProgressBar" => CreateProgressBar(d, tagManager, subs),
            "Indicator" => CreateIndicator(d, tagManager, subs),
            "Slider" => CreateSlider(d, tagManager, tagIOHandler, eventManager, subs),
            "TextInput" => CreateTextInput(d, tagManager, tagIOHandler, eventManager, subs),
            "Rectangle" => CreateRectangle(d),
            "Line" => CreateLine(d),
            "ToggleSwitch" => CreateToggleSwitch(d, tagManager, tagIOHandler, eventManager, subs),
            "Numeric" => CreateNumeric(d, tagManager, subs),
            "ImageView" => CreateImageView(d, resolveImagePath),
            "Text" => CreateText(d, tagManager, subs),
            "RadioButton" => CreateRadioButton(d, tagManager, tagIOHandler, eventManager, subs),
            "DateTime" => CreateDateTime(d, tagManager, subs),
            "TrendView" => CreateTrendView(d, historianQuery),
            "AlarmView" => CreateAlarmView(d),
            "Console" => CreateConsole(d, console),
            "CircularGauge" => CreateGaugeView(d, tagManager, subs),
            "Tank" => CreateTank(d, tagManager, subs),
            "Motor" => CreateMotor(d, tagManager, subs),
            "Pump" => CreatePump(d, tagManager, subs),
            "Triangle" => CreateTriangle(d),
            "Spinner" => CreateSpinner(d),
            "ComboBox" => CreateComboBox(d, tagManager, tagIOHandler, subs),
            "Tab" => CreateTab(d),
            "Conveyor" => CreateConveyor(d, tagManager, subs),
            "SVGView" => CreateSVGView(d, resolveSvgPath ?? resolveImagePath),
            "Table" => CreateTableView(d, historianQuery),
            "TableView" => CreateTableView(d, historianQuery),
            "Popup" => CreatePopup(d),
            _ => CreatePlaceholder(d)
        };
        return c;
    }

    private static RuntimeConsoleView CreateConsole(ComponentDescriptor d, IConsole? console)
    {
        var c = new RuntimeConsoleView();
        c.ApplyDescriptor(d);
        c.SetConsole(console);
        return c;
    }

    private static RuntimeButton CreateButton(ComponentDescriptor d, EventManager? eventManager, IConsole? console = null)
    {
        var b = new RuntimeButton();
        b.ApplyDescriptor(d);
        b.ComponentId = d.Id;
        b.TagName = d.TagName;
        if (eventManager != null && !string.IsNullOrEmpty(d.Id))
        {
            // Wire all trigger types
            b.Click += (_, _) => eventManager.FireTrigger(d.Id, "OnClick");
            b.DoubleClick += (_, _) => eventManager.FireTrigger(d.Id, "OnDoubleClick");
            b.RightClick += (_, _) => eventManager.FireTrigger(d.Id, "OnRightClick");
            b.MouseDown += (_, _) => eventManager.FireTrigger(d.Id, "OnPress");
            b.MouseUp += (_, _) => eventManager.FireTrigger(d.Id, "OnRelease");
            b.MouseEnter += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseEnter");
            b.MouseLeave += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseLeave");
        }
        // Wire ClearLogs action to console when on Logs screen
        var action = GetProperty(d, "action", "");
        if (string.Equals(action, "ClearLogs", StringComparison.OrdinalIgnoreCase) && console != null)
        {
            b.Click += (_, _) => console.ClearLogs();
        }
        return b;
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static RuntimeTextLabel CreateTextLabel(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var t = new RuntimeTextLabel();
        t.ApplyDescriptor(d);
        t.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            t.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => t.SetValue(value)));
            subs.Add(sub);
        }
        return t;
    }

    private static RuntimeGaugeView CreateGaugeView(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var g = new RuntimeGaugeView();
        g.ApplyDescriptor(d);
        g.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            g.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => g.SetValue(value)));
            subs.Add(sub);
        }
        return g;
    }

    private static RuntimeCheckbox CreateCheckbox(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, TagIOHandler? tagIOHandler, EventManager? eventManager, List<IDisposable> subs)
    {
        var c = new RuntimeCheckbox();
        c.ApplyDescriptor(d);
        c.ComponentId = d.Id;
        c.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            c.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => c.SetValue(value)));
            subs.Add(sub);
        }
        if (tagIOHandler != null && !string.IsNullOrEmpty(d.TagName))
            c.CheckedChanged += (_, _) => tagIOHandler.WriteTag(d.TagName, c.IsChecked == true ? 1 : 0);
        if (eventManager != null && !string.IsNullOrEmpty(d.Id))
        {
            c.CheckedChanged += (_, _) => eventManager.FireTrigger(d.Id, "OnClick");
            c.CheckedChanged += (_, _) => eventManager.FireTrigger(d.Id, "OnStateChange");
            c.MouseDown += (_, _) => eventManager.FireTrigger(d.Id, "OnPress");
            c.MouseUp += (_, _) => eventManager.FireTrigger(d.Id, "OnRelease");
            c.MouseEnter += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseEnter");
            c.MouseLeave += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseLeave");
            c.DoubleClick += (_, _) => eventManager.FireTrigger(d.Id, "OnDoubleClick");
            c.RightClick += (_, _) => eventManager.FireTrigger(d.Id, "OnRightClick");
        }
        return c;
    }

    private static RuntimeProgressBar CreateProgressBar(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var p = new RuntimeProgressBar();
        p.ApplyDescriptor(d);
        p.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            p.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => p.SetValue(value)));
            subs.Add(sub);
        }
        return p;
    }

    private static RuntimeIndicator CreateIndicator(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var i = new RuntimeIndicator();
        i.ApplyDescriptor(d);
        i.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            i.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => i.SetValue(value)));
            subs.Add(sub);
        }
        return i;
    }

    private static RuntimeSlider CreateSlider(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, TagIOHandler? tagIOHandler, EventManager? eventManager, List<IDisposable> subs)
    {
        var s = new RuntimeSlider();
        s.ApplyDescriptor(d);
        s.ComponentId = d.Id;
        s.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            s.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => s.SetValue(value)));
            subs.Add(sub);
        }
        if (tagIOHandler != null && !string.IsNullOrEmpty(d.TagName))
            s.ValueChanged += (_, _) => tagIOHandler.WriteTag(d.TagName, s.Value);
        if (eventManager != null && !string.IsNullOrEmpty(d.Id))
        {
            s.ValueChanged += (_, _) => eventManager.FireTrigger(d.Id, "OnValueChange");
            s.MouseDown += (_, _) => eventManager.FireTrigger(d.Id, "OnPress");
            s.MouseUp += (_, _) => eventManager.FireTrigger(d.Id, "OnRelease");
            s.MouseEnter += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseEnter");
            s.MouseLeave += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseLeave");
            s.DoubleClick += (_, _) => eventManager.FireTrigger(d.Id, "OnDoubleClick");
            s.RightClick += (_, _) => eventManager.FireTrigger(d.Id, "OnRightClick");
        }
        return s;
    }

    private static RuntimeTextInput CreateTextInput(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, TagIOHandler? tagIOHandler, EventManager? eventManager, List<IDisposable> subs)
    {
        var t = new RuntimeTextInput();
        t.ApplyDescriptor(d);
        t.ComponentId = d.Id;
        t.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            t.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => t.SetValue(value)));
            subs.Add(sub);
        }
        if (tagIOHandler != null && !string.IsNullOrEmpty(d.TagName))
            t.TextCommitted += (_, _) => tagIOHandler.WriteTag(d.TagName, t.Text);
        if (eventManager != null && !string.IsNullOrEmpty(d.Id))
        {
            t.TextCommitted += (_, _) => eventManager.FireTrigger(d.Id, "OnValueChange");
            t.MouseDown += (_, _) => eventManager.FireTrigger(d.Id, "OnPress");
            t.MouseUp += (_, _) => eventManager.FireTrigger(d.Id, "OnRelease");
            t.MouseEnter += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseEnter");
            t.MouseLeave += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseLeave");
            t.KeyPress += (_, _) => eventManager.FireTrigger(d.Id, "OnKeyPress");
            t.FocusIn += (_, _) => eventManager.FireTrigger(d.Id, "OnFocusIn");
            t.FocusOut += (_, _) => eventManager.FireTrigger(d.Id, "OnFocusOut");
        }
        return t;
    }

    private static RuntimeRectangle CreateRectangle(ComponentDescriptor d)
    {
        var r = new RuntimeRectangle();
        r.ApplyDescriptor(d);
        return r;
    }

    private static RuntimeLine CreateLine(ComponentDescriptor d)
    {
        var l = new RuntimeLine();
        l.ApplyDescriptor(d);
        return l;
    }

    private static RuntimeToggleSwitch CreateToggleSwitch(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, TagIOHandler? tagIOHandler, EventManager? eventManager, List<IDisposable> subs)
    {
        var t = new RuntimeToggleSwitch();
        t.ApplyDescriptor(d);
        t.ComponentId = d.Id;
        t.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            t.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => t.SetValue(value)));
            subs.Add(sub);
        }
        if (tagIOHandler != null && !string.IsNullOrEmpty(d.TagName))
            t.Toggled += (_, _) => tagIOHandler.WriteTag(d.TagName, t.IsChecked == true ? 1 : 0);
        if (eventManager != null && !string.IsNullOrEmpty(d.Id))
        {
            t.Toggled += (_, _) => eventManager.FireTrigger(d.Id, "OnClick");
            t.Toggled += (_, _) => eventManager.FireTrigger(d.Id, "OnStateChange");
            t.MouseDown += (_, _) => eventManager.FireTrigger(d.Id, "OnPress");
            t.MouseUp += (_, _) => eventManager.FireTrigger(d.Id, "OnRelease");
            t.MouseEnter += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseEnter");
            t.MouseLeave += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseLeave");
            t.DoubleClick += (_, _) => eventManager.FireTrigger(d.Id, "OnDoubleClick");
            t.RightClick += (_, _) => eventManager.FireTrigger(d.Id, "OnRightClick");
        }
        return t;
    }

    private static RuntimeNumeric CreateNumeric(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var n = new RuntimeNumeric();
        n.ApplyDescriptor(d);
        n.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            var tagValue = tagManager.GetTagValue(d.TagName);
            n.SetValue(tagValue ?? 0); // Ensure we always set a value, even if tag is null
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => n.SetValue(value ?? 0)));
            subs.Add(sub);
        }
        else
        {
            // No tag assigned - show default value of 0 so component is visible
            n.SetValue(0);
        }
        return n;
    }

    private static RuntimeImageView CreateImageView(ComponentDescriptor d, Func<string, string?>? resolveImagePath)
    {
        var img = new RuntimeImageView();
        img.ApplyDescriptor(d);
        var source = GetPropString(d, "source", GetPropString(d, "imageName", d.Name));
        if (resolveImagePath != null && !string.IsNullOrEmpty(source))
        {
            var path = resolveImagePath(source);
            img.SetSourcePath(path);
        }
        return img;
    }

    private static string GetPropString(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static RuntimeText CreateText(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var t = new RuntimeText();
        t.ApplyDescriptor(d);
        t.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            t.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => t.SetValue(value)));
            subs.Add(sub);
        }
        return t;
    }

    private static RuntimeRadioButton CreateRadioButton(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, TagIOHandler? tagIOHandler, EventManager? eventManager, List<IDisposable> subs)
    {
        var r = new RuntimeRadioButton();
        r.ApplyDescriptor(d);
        r.ComponentId = d.Id;
        r.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            r.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => r.SetValue(value)));
            subs.Add(sub);
        }
        if (tagIOHandler != null && !string.IsNullOrEmpty(d.TagName))
            r.CheckedChanged += (_, _) => tagIOHandler.WriteTag(d.TagName, r.IsChecked == true ? 1 : 0);
        if (eventManager != null && !string.IsNullOrEmpty(d.Id))
        {
            r.CheckedChanged += (_, _) => eventManager.FireTrigger(d.Id, "OnClick");
            r.CheckedChanged += (_, _) => eventManager.FireTrigger(d.Id, "OnStateChange");
            r.MouseDown += (_, _) => eventManager.FireTrigger(d.Id, "OnPress");
            r.MouseUp += (_, _) => eventManager.FireTrigger(d.Id, "OnRelease");
            r.MouseEnter += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseEnter");
            r.MouseLeave += (_, _) => eventManager.FireTrigger(d.Id, "OnMouseLeave");
            r.DoubleClick += (_, _) => eventManager.FireTrigger(d.Id, "OnDoubleClick");
            r.RightClick += (_, _) => eventManager.FireTrigger(d.Id, "OnRightClick");
        }
        return r;
    }

    private static RuntimeDateTime CreateDateTime(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var dt = new RuntimeDateTime();
        dt.ApplyDescriptor(d);
        dt.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            dt.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => dt.SetValue(value)));
            subs.Add(sub);
        }
        else
            dt.SetValue(null);
        return dt;
    }

    private static RuntimeTrendView CreateTrendView(ComponentDescriptor d, Runtime.Modules.Screens.HistorianQueryHelper? historianQuery)
    {
        var t = new RuntimeTrendView();
        t.ApplyDescriptor(d);
        t.TagName = d.TagName;
        if (historianQuery != null)
            t.SetHistorianQuery(historianQuery);
        return t;
    }

    private static RuntimeAlarmView CreateAlarmView(ComponentDescriptor d)
    {
        var a = new RuntimeAlarmView();
        a.ApplyDescriptor(d);
        return a;
    }

    private static RuntimeTank CreateTank(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var t = new RuntimeTank();
        t.ApplyDescriptor(d);
        t.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            t.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => t.SetValue(value)));
            subs.Add(sub);
        }
        return t;
    }

    private static RuntimeMotor CreateMotor(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var m = new RuntimeMotor();
        m.ApplyDescriptor(d);
        m.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            m.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => m.SetValue(value)));
            subs.Add(sub);
        }
        return m;
    }

    private static RuntimePump CreatePump(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var p = new RuntimePump();
        p.ApplyDescriptor(d);
        p.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            p.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => p.SetValue(value)));
            subs.Add(sub);
        }
        return p;
    }

    private static RuntimeTriangle CreateTriangle(ComponentDescriptor d)
    {
        var t = new RuntimeTriangle();
        t.ApplyDescriptor(d);
        return t;
    }

    private static RuntimeSpinner CreateSpinner(ComponentDescriptor d)
    {
        var s = new RuntimeSpinner();
        s.ApplyDescriptor(d);
        return s;
    }

    private static RuntimeComboBox CreateComboBox(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, TagIOHandler? tagIOHandler, List<IDisposable> subs)
    {
        var c = new RuntimeComboBox();
        c.ApplyDescriptor(d);
        c.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            c.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => c.SetValue(value)));
            subs.Add(sub);
        }
        if (tagIOHandler != null && !string.IsNullOrEmpty(d.TagName))
            c.SelectionChanged += (_, _) => tagIOHandler.WriteTag(d.TagName, c.SelectedItem ?? "");
        return c;
    }

    private static RuntimeTab CreateTab(ComponentDescriptor d)
    {
        var t = new RuntimeTab();
        t.ApplyDescriptor(d);
        return t;
    }

    private static RuntimeConveyor CreateConveyor(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, List<IDisposable> subs)
    {
        var c = new RuntimeConveyor();
        c.ApplyDescriptor(d);
        c.TagName = d.TagName;
        if (tagManager != null && !string.IsNullOrEmpty(d.TagName))
        {
            c.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => c.SetValue(value)));
            subs.Add(sub);
        }
        return c;
    }

    private static RuntimeSVGView CreateSVGView(ComponentDescriptor d, Func<string, string?>? resolveSvgPath)
    {
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ===== Creating SVG View Component =====");
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Component ID: {d.Id}, Type: {d.ComponentType}, Visible: {d.Visible}");
        
        var s = new RuntimeSVGView();
        s.ApplyDescriptor(d);
        
        // Get SVG path from properties - runtime simplifies: just use filename and look in data/svg
        var svgPath = GetPropString(d, "svgPath", "");
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] SVG path from descriptor: '{svgPath}'");
        
        if (string.IsNullOrEmpty(svgPath))
        {
            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] WARNING: SVG path is empty or null - component will show placeholder");
            return s;
        }
        
        // Extract just the filename (handle both full paths and just filenames)
        var fileName = Path.GetFileName(svgPath);
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Extracted filename: '{fileName}'");
        
        // Runtime always looks in data/svg folder relative to executable
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
        var dataSvgDir = Path.Combine(exeDir, "data", "svg");
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Executable directory: '{exeDir}'");
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Data/SVG directory: '{dataSvgDir}'");
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Data/SVG directory exists: {Directory.Exists(dataSvgDir)}");
        
        // First try direct path in data/svg
        var dataSvgPath = Path.Combine(dataSvgDir, fileName);
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Checking direct path: '{dataSvgPath}'");
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Direct path exists: {File.Exists(dataSvgPath)}");
        
        if (File.Exists(dataSvgPath))
        {
            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ✓ Found SVG at direct path: '{dataSvgPath}'");
            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Calling SetSvgPath with: '{dataSvgPath}'");
            s.SetSvgPath(dataSvgPath);
        }
        else
        {
            // Search recursively in data/svg subfolders (e.g., Containers/)
            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Direct path not found, searching subfolders...");
            string? foundPath = null;
            try
            {
                if (Directory.Exists(dataSvgDir))
                {
                    System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Searching recursively in: '{dataSvgDir}'");
                    var allSvgFiles = Directory.GetFiles(dataSvgDir, fileName, SearchOption.AllDirectories);
                    System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Found {allSvgFiles.Length} file(s) matching '{fileName}'");
                    
                    foreach (var file in allSvgFiles)
                    {
                        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder]   - Found: '{file}'");
                    }
                    
                    if (allSvgFiles.Length > 0)
                    {
                        foundPath = allSvgFiles[0];
                        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ✓ Using first match: '{foundPath}'");
                    }
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ERROR: Data/SVG directory does not exist: '{dataSvgDir}'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ERROR: Exception while searching for SVG '{fileName}': {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Exception type: {ex.GetType().Name}");
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] StackTrace: {ex.StackTrace}");
            }
            
            if (foundPath != null)
            {
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Calling SetSvgPath with found path: '{foundPath}'");
                s.SetSvgPath(foundPath);
            }
            else
            {
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ✗ SVG file not found anywhere: '{fileName}'");
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder]   Checked direct path: '{dataSvgPath}'");
                System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder]   Searched recursively in: '{dataSvgDir}'");
            }
        }
        
        System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] ===== Finished Creating SVG View Component =====");
        return s;
    }

    private static RuntimeTableView CreateTableView(ComponentDescriptor d, Runtime.Modules.Screens.HistorianQueryHelper? historianQuery)
    {
        var t = new RuntimeTableView();
        t.ApplyDescriptor(d);
        if (historianQuery != null)
            t.SetHistorianQuery(historianQuery);
        return t;
    }

    private static RuntimePopup CreatePopup(ComponentDescriptor d)
    {
        var p = new RuntimePopup();
        p.ApplyDescriptor(d);
        return p;
    }

    private static Control CreatePlaceholder(ComponentDescriptor d)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = $"{d.ComponentType}: {d.Name}",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
    }

    private static void ApplyAnimationState(Control control, AnimationState state)
    {
        if (state.Visible.HasValue)
            control.IsVisible = state.Visible.Value;
        if (state.Opacity.HasValue)
            control.Opacity = Math.Clamp(state.Opacity.Value, 0, 1);
        
        // Apply background color to appropriate element based on control type
        if (!string.IsNullOrEmpty(state.BackgroundColor))
        {
            var brush = ParseBrush(state.BackgroundColor);
            
            // Try to find common named elements in UserControls
            if (control is UserControl uc)
            {
                // For Indicator, Motor, Pump - apply to ellipse fill
                var ellipse = uc.FindControl<Avalonia.Controls.Shapes.Ellipse>("TheEllipse");
                if (ellipse != null)
                {
                    ellipse.Fill = brush;
                }
                else
                {
                    // For buttons, apply to TheButton
                    var button = uc.FindControl<Avalonia.Controls.Button>("TheButton");
                    if (button != null)
                    {
                        button.Background = brush;
                    }
                    else
                    {
                        // For other controls, try border background
                        var border = uc.FindControl<Avalonia.Controls.Border>("TheBorder");
                        if (border != null)
                            border.Background = brush;
                        else if (control is Avalonia.Controls.Border b)
                            b.Background = brush;
                    }
                }
            }
            else if (control is Avalonia.Controls.Border b)
            {
                b.Background = brush;
            }
        }
        
        if (!string.IsNullOrEmpty(state.ForegroundColor))
        {
            var brush = ParseBrush(state.ForegroundColor);
            if (control is Avalonia.Controls.TextBlock tbf)
                tbf.Foreground = brush;
            else if (control is UserControl uc)
            {
                // Try to find TextBlock in UserControl
                var textBlock = uc.FindControl<Avalonia.Controls.TextBlock>("TheText") 
                    ?? uc.FindControl<Avalonia.Controls.TextBlock>("ButtonText")
                    ?? uc.FindControl<Avalonia.Controls.TextBlock>("LabelText");
                if (textBlock != null)
                    textBlock.Foreground = brush;
            }
        }
        
        if (state.IsFlashing.HasValue)
        {
            control.Classes.Set("flashing", state.IsFlashing.Value);
            // For flashing, toggle opacity if needed
            if (state.IsFlashing.Value && state.Opacity == null)
            {
                // Start flashing animation - could use a timer here for actual flashing
                // For now, just set the class
            }
        }
        
        // Apply translation transform with smooth animation
        if (state.TranslationX.HasValue || state.TranslationY.HasValue)
        {
            var targetX = state.TranslationX ?? 0;
            var targetY = state.TranslationY ?? 0;
            
            lock (_translationTrackers)
            {
                if (!_translationTrackers.TryGetValue(control, out var tracker))
                {
                    // Initialize tracker - get current position from transform if it exists
                    double currentX = 0, currentY = 0;
                    if (control.RenderTransform is Avalonia.Media.TranslateTransform currentTransform)
                    {
                        currentX = currentTransform.X;
                        currentY = currentTransform.Y;
                    }
                    
                    tracker = new TranslationAnimationTracker
                    {
                        CurrentX = currentX,
                        CurrentY = currentY
                    };
                    _translationTrackers[control] = tracker;
                }
                
                // Get duration and positions from state
                var duration = state.TranslationDuration ?? 1.0;
                var startX = state.TranslationStartX ?? 0.0;
                var startY = state.TranslationStartY ?? 0.0;
                var endX = state.TranslationEndX ?? targetX;
                var endY = state.TranslationEndY ?? targetY;
                
                // Store start and end positions for looping
                tracker.StartX = startX;
                tracker.StartY = startY;
                tracker.EndX = endX;
                tracker.EndY = endY;
                
                // Determine if we should loop (when tag value indicates animation should be active)
                // If target is at end position, we should loop; if at start, don't loop
                bool shouldLoop = Math.Abs(targetX - endX) < 0.01 && Math.Abs(targetY - endY) < 0.01;
                tracker.ShouldLoop = shouldLoop;
                
                // If target is same as current, skip animation (unless we need to start looping)
                if (Math.Abs(tracker.CurrentX - targetX) < 0.01 && Math.Abs(tracker.CurrentY - targetY) < 0.01 && !shouldLoop)
                {
                    tracker.IsAnimating = false;
                    tracker.TargetX = targetX;
                    tracker.TargetY = targetY;
                    control.RenderTransform = new Avalonia.Media.TranslateTransform(targetX, targetY);
                }
                else
                {
                    // Store start position (current position when animation begins)
                    // If starting a new loop, use the actual start position
                    if (shouldLoop && Math.Abs(tracker.CurrentX - endX) < 0.01 && Math.Abs(tracker.CurrentY - endY) < 0.01)
                    {
                        // We're at end, reset to start for new loop
                        tracker.CurrentX = startX;
                        tracker.CurrentY = startY;
                        tracker.StartX = startX;
                        tracker.StartY = startY;
                    }
                    else
                    {
                        tracker.StartX = tracker.CurrentX;
                        tracker.StartY = tracker.CurrentY;
                    }
                    
                    // Update target and start animation from current position
                    tracker.TargetX = targetX;
                    tracker.TargetY = targetY;
                    tracker.Duration = duration;
                    tracker.StartTime = DateTime.Now;
                    tracker.IsAnimating = true;
                }
            }
            
            System.Diagnostics.Trace.WriteLine($"[ScreenViewBuilder] Set translation target: X={targetX}, Y={targetY} to control");
        }
        else
        {
            // Clear translation if not set (reset to no transform)
            lock (_translationTrackers)
            {
                if (_translationTrackers.TryGetValue(control, out var tracker))
                {
                    tracker.IsAnimating = false;
                    tracker.TargetX = 0;
                    tracker.TargetY = 0;
                }
            }
            control.RenderTransform = null;
        }
    }

    private static bool IsDarkColor(string hex)
    {
        if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length < 7)
            return false;
        try
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            // Calculate luminance - if less than 128, consider it dark
            var luminance = (0.299 * r + 0.587 * g + 0.114 * b);
            return luminance < 128;
        }
        catch
        {
            return false;
        }
    }

    private static IBrush ParseBrush(string hex)
    {
        // Use ColorParser which handles both hex and named colors
        return ColorParser.ParseBrush(hex);
    }
}
