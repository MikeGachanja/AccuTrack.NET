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

/// <summary>Builds an Avalonia view from a screen descriptor (parsed JSON).</summary>
public static class ScreenViewBuilder
{
    public static Control? Build(ScreenRenderer.ScreenDescriptor? screen)
        => Build(screen, null, null, null, null, null, null, null);

    /// <summary>Builds view and wires tag bindings and button events when services are provided.</summary>
    /// <param name="resolveImagePath">Optional resolver for image names (e.g. ScreenManager.ResolveImagePath).</param>
    /// <param name="resolveSvgPath">Optional resolver for SVG paths (e.g. ScreenManager.ResolveSvgPath). If null, uses resolveImagePath.</param>
    /// <param name="animationManager">Optional; when set, subscribes each control with an Id to animation state (visibility, opacity, color).</param>
    /// <param name="console">Optional; when set, Console components on the screen will display log output from this service.</param>
    public static Control? Build(ScreenRenderer.ScreenDescriptor? screen, Runtime.Modules.TagsEngine.TagManager? tagManager, EventManager? eventManager, TagIOHandler? tagIOHandler = null, Func<string, string?>? resolveImagePath = null, Func<string, string?>? resolveSvgPath = null, AnimationManager? animationManager = null, IConsole? console = null)
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
            var control = CreateControl(comp, tagManager, eventManager, tagIOHandler, resolveImagePath, resolveSvgPath, subs, console);
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
            
            Canvas.SetLeft(control, comp.X);
            Canvas.SetTop(control, comp.Y);
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
        return scroll;
    }

    private static Control? CreateControl(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, EventManager? eventManager, TagIOHandler? tagIOHandler, Func<string, string?>? resolveImagePath, Func<string, string?>? resolveSvgPath, List<IDisposable> subs, IConsole? console = null)
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
            "TextInput" => CreateTextInput(d, tagManager, tagIOHandler, subs),
            "Rectangle" => CreateRectangle(d),
            "Line" => CreateLine(d),
            "ToggleSwitch" => CreateToggleSwitch(d, tagManager, tagIOHandler, eventManager, subs),
            "Numeric" => CreateNumeric(d, tagManager, subs),
            "ImageView" => CreateImageView(d, resolveImagePath),
            "Text" => CreateText(d, tagManager, subs),
            "RadioButton" => CreateRadioButton(d, tagManager, tagIOHandler, eventManager, subs),
            "DateTime" => CreateDateTime(d, tagManager, subs),
            "TrendView" => CreateTrendView(d),
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
            "SVGView" => CreateSVGView(d, resolveSvgPath ?? resolveImagePath), // Use resolveSvgPath if available, fallback to resolveImagePath
            "TableView" => CreateTableView(d),
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
            // Wire various trigger types
            b.Click += (_, _) => eventManager.FireTrigger(d.Id, "OnClick");
            // Note: Double-click and right-click would need to be added to RuntimeButton if not already present
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
            c.CheckedChanged += (_, _) => eventManager.FireTrigger(d.Id, "click");
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
            s.ValueChanged += (_, _) => eventManager.FireTrigger(d.Id, "click");
        return s;
    }

    private static RuntimeTextInput CreateTextInput(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, TagIOHandler? tagIOHandler, List<IDisposable> subs)
    {
        var t = new RuntimeTextInput();
        t.ApplyDescriptor(d);
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
            t.Toggled += (_, _) => eventManager.FireTrigger(d.Id, "click");
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
            r.CheckedChanged += (_, _) => eventManager.FireTrigger(d.Id, "click");
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

    private static RuntimeTrendView CreateTrendView(ComponentDescriptor d)
    {
        var t = new RuntimeTrendView();
        t.ApplyDescriptor(d);
        t.TagName = d.TagName;
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

    private static RuntimeTableView CreateTableView(ComponentDescriptor d)
    {
        var t = new RuntimeTableView();
        t.ApplyDescriptor(d);
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
                    // For other controls, try border background
                    var border = uc.FindControl<Avalonia.Controls.Border>("TheBorder");
                    if (border != null)
                        border.Background = brush;
                    else if (control is Avalonia.Controls.Border b)
                        b.Background = brush;
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
        
        if (state.TranslationX.HasValue || state.TranslationY.HasValue)
        {
            var tx = state.TranslationX ?? 0;
            var ty = state.TranslationY ?? 0;
            control.RenderTransform = new Avalonia.Media.TranslateTransform(tx, ty);
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
