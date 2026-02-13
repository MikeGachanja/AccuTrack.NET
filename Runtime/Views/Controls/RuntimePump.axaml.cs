using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimePump : UserControl
{
    private static SolidColorBrush OffBrush = new(Color.FromRgb(80, 80, 80));
    private static SolidColorBrush OnBrush = new(Color.FromRgb(0, 150, 255));
    private static SolidColorBrush FaultBrush = new(Color.FromRgb(255, 0, 0));

    public RuntimePump()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Apply size from descriptor - use the smaller dimension for a circle
        var size = Math.Min(Math.Max(1, d.Width), Math.Max(1, d.Height));
        TheEllipse.Width = size;
        TheEllipse.Height = size;
        
        // Scale font size proportionally
        var fontSize = Math.Max(8, size / 2);
        PumpText.FontSize = fontSize;
        
        // Apply label
        LabelText.Text = GetProperty(d, "label", "Pump");
        
        // Apply colors from properties
        var onColorStr = GetProperty(d, "onColor", "");
        var offColorStr = GetProperty(d, "offColor", "");
        var faultColorStr = GetProperty(d, "faultColor", "");
        var borderColorStr = GetProperty(d, "borderColor", "");
        
        if (!string.IsNullOrEmpty(onColorStr))
            OnBrush = new SolidColorBrush(ParseColor(onColorStr));
        if (!string.IsNullOrEmpty(offColorStr))
            OffBrush = new SolidColorBrush(ParseColor(offColorStr));
        if (!string.IsNullOrEmpty(faultColorStr))
            FaultBrush = new SolidColorBrush(ParseColor(faultColorStr));
        if (!string.IsNullOrEmpty(borderColorStr))
            TheEllipse.Stroke = new SolidColorBrush(ParseColor(borderColorStr));
        
        // Apply border
        var borderWidth = GetPropertyInt(d, "borderWidth", 2);
        TheEllipse.StrokeThickness = borderWidth;
        
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        // Initialize with off state
        SetValue(0);
    }

    public void SetValue(object? value)
    {
        var on = value is bool b ? b : (value is 1 or "1" || (value?.ToString()?.Equals("1", StringComparison.Ordinal) == true));
        var faulted = value is 2 or "2" || (value?.ToString()?.Equals("2", StringComparison.Ordinal) == true);
        
        var fillBrush = faulted ? FaultBrush : (on ? OnBrush : OffBrush);
        TheEllipse.Fill = fillBrush;
    }
    
    private static int GetPropertyInt(ComponentDescriptor d, string key, int fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return (int)dbl;
        if (v is float f) return (int)f;
        return int.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }
    
    private static Color ParseColor(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Colors.Gray;
        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (hex.Length >= 7)
        {
            try
            {
                var r = Convert.ToInt32(hex.Substring(1, 2), 16);
                var g = Convert.ToInt32(hex.Substring(3, 2), 16);
                var b = Convert.ToInt32(hex.Substring(5, 2), 16);
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            catch
            {
                return Colors.Gray;
            }
        }
        return Colors.Gray;
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
