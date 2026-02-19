using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeIndicator : UserControl
{
    private static SolidColorBrush OffBrush = new(Color.FromRgb(80, 80, 80));
    private static SolidColorBrush OnBrush = new(Color.FromRgb(0, 200, 83));
    private static SolidColorBrush WarningBrush = new(Color.FromRgb(255, 255, 0));
    private static SolidColorBrush ErrorBrush = new(Color.FromRgb(255, 0, 0));

    public RuntimeIndicator()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Apply size from descriptor - use the smaller dimension for a circle, or use configured size
        // The Designer uses Math.Min(width - 10, height - 30) but we'll use the full configured size
        var size = Math.Min(Math.Max(1, d.Width), Math.Max(1, d.Height));
        TheEllipse.Width = size;
        TheEllipse.Height = size;
        
        // Apply colors from properties if available
        var onColorStr = GetProperty(d, "onColor", "");
        var offColorStr = GetProperty(d, "offColor", "");
        var warningColorStr = GetProperty(d, "warningColor", "");
        var errorColorStr = GetProperty(d, "errorColor", "");
        
        if (!string.IsNullOrEmpty(onColorStr))
            OnBrush = new SolidColorBrush(ParseColor(onColorStr));
        if (!string.IsNullOrEmpty(offColorStr))
            OffBrush = new SolidColorBrush(ParseColor(offColorStr));
        if (!string.IsNullOrEmpty(warningColorStr))
            WarningBrush = new SolidColorBrush(ParseColor(warningColorStr));
        if (!string.IsNullOrEmpty(errorColorStr))
            ErrorBrush = new SolidColorBrush(ParseColor(errorColorStr));
        
        // Apply shape if specified (Circle or Square)
        var shapeStr = GetProperty(d, "shape", "Circle");
        // Note: Currently only Circle is implemented in XAML, Square would need additional XAML changes
        
        var label = GetProperty(d, "label", "");
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);

        // Font from designer; minimum 12pt so label is readable
        ApplyFontToText(LabelText, d, "font", "fontSize", "fontStyle", 12);

        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        // Initialize with off state
        SetValue(0);
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

    public void SetValue(object? value)
    {
        // Support multiple states: Off (0/false), On (1/true), Warning (2), Error (3)
        var stateValue = 0;
        if (value is bool b)
            stateValue = b ? 1 : 0;
        else if (value is int i)
            stateValue = i;
        else if (value is 1 or "1" || (value?.ToString()?.Equals("1", StringComparison.Ordinal) == true))
            stateValue = 1;
        else if (value is 2 or "2" || (value?.ToString()?.Equals("2", StringComparison.Ordinal) == true))
            stateValue = 2;
        else if (value is 3 or "3" || (value?.ToString()?.Equals("3", StringComparison.Ordinal) == true))
            stateValue = 3;
        
        var brush = stateValue switch
        {
            1 => OnBrush,
            2 => WarningBrush,
            3 => ErrorBrush,
            _ => OffBrush
        };
        TheEllipse.Fill = brush;
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static void ApplyFontToText(Avalonia.Controls.TextBlock text, ComponentDescriptor d, string fontKey, string sizeKey, string styleKey, double defaultSize)
    {
        var fontName = GetProperty(d, fontKey, "Arial");
        text.FontFamily = new FontFamily(fontName);
        var size = GetPropDouble(d, sizeKey, defaultSize);
        text.FontSize = Math.Max(12, size > 0 ? size : defaultSize);
        var labelColor = GetProperty(d, "labelColor", "#333333");
        text.Foreground = new SolidColorBrush(ParseColor(labelColor));
        var styleStr = GetProperty(d, styleKey, "Regular");
        text.FontWeight = styleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        text.FontStyle = styleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
    }

    private static double GetPropDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }
}
