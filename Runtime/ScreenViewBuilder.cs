using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Runtime.Modules.Screens;
using Runtime.Modules.TagsEngine;
using Runtime.Views.Controls;

namespace Runtime;

/// <summary>Builds an Avalonia view from a screen descriptor (parsed JSON).</summary>
public static class ScreenViewBuilder
{
    public static Control? Build(ScreenRenderer.ScreenDescriptor? screen)
        => Build(screen, null, null, null, null, null);

    /// <summary>Builds view and wires tag bindings and button events when services are provided.</summary>
    /// <param name="resolveImagePath">Optional resolver for image names (e.g. ScreenManager.ResolveImagePath).</param>
    /// <param name="animationManager">Optional; when set, subscribes each control with an Id to animation state (visibility, opacity, color).</param>
    public static Control? Build(ScreenRenderer.ScreenDescriptor? screen, Runtime.Modules.TagsEngine.TagManager? tagManager, EventManager? eventManager, TagIOHandler? tagIOHandler = null, Func<string, string?>? resolveImagePath = null, AnimationManager? animationManager = null)
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
            var control = CreateControl(comp, tagManager, eventManager, tagIOHandler, resolveImagePath, subs);
            if (control == null) continue;
            Canvas.SetLeft(control, comp.X);
            Canvas.SetTop(control, comp.Y);
            control.Width = Math.Max(1, comp.Width);
            control.Height = Math.Max(1, comp.Height);
            canvas.Children.Add(control);
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

    private static Control? CreateControl(ComponentDescriptor d, Runtime.Modules.TagsEngine.TagManager? tagManager, EventManager? eventManager, TagIOHandler? tagIOHandler, Func<string, string?>? resolveImagePath, List<IDisposable> subs)
    {
        if (!d.Visible) return null;
        var type = d.ComponentType ?? "";
        Control? c = type switch
        {
            "Button" => CreateButton(d, eventManager),
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
            "CircularGauge" => CreateGaugeView(d, tagManager, subs),
            "Tank" => CreateTank(d, tagManager, subs),
            "Motor" => CreateMotor(d, tagManager, subs),
            "Pump" => CreatePump(d, tagManager, subs),
            "Triangle" => CreateTriangle(d),
            "Spinner" => CreateSpinner(d),
            "ComboBox" => CreateComboBox(d, tagManager, tagIOHandler, subs),
            "Tab" => CreateTab(d),
            "Conveyor" => CreateConveyor(d, tagManager, subs),
            "SVGView" => CreateSVGView(d),
            "TableView" => CreateTableView(d),
            "Popup" => CreatePopup(d),
            _ => CreatePlaceholder(d)
        };
        return c;
    }

    private static RuntimeButton CreateButton(ComponentDescriptor d, EventManager? eventManager)
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
        return b;
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
            n.SetValue(tagManager.GetTagValue(d.TagName));
            var sub = tagManager.Subscribe(d.TagName, (_, value, _) =>
                Avalonia.Threading.Dispatcher.UIThread.Post(() => n.SetValue(value)));
            subs.Add(sub);
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

    private static RuntimeSVGView CreateSVGView(ComponentDescriptor d)
    {
        var s = new RuntimeSVGView();
        s.ApplyDescriptor(d);
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
        if (!string.IsNullOrEmpty(state.BackgroundColor))
        {
            var brush = ParseBrush(state.BackgroundColor);
            if (control is Avalonia.Controls.Border b)
                b.Background = brush;
        }
        if (!string.IsNullOrEmpty(state.ForegroundColor))
        {
            var brush = ParseBrush(state.ForegroundColor);
            if (control is Avalonia.Controls.TextBlock tbf)
                tbf.Foreground = brush;
        }
        if (state.IsFlashing.HasValue)
        {
            control.Classes.Set("flashing", state.IsFlashing.Value);
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
        if (string.IsNullOrEmpty(hex)) return Brushes.White;
        if (hex.StartsWith("#") && hex.Length >= 7)
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
        }
        return Brushes.White;
    }
}
