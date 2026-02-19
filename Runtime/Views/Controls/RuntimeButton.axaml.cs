using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeButton : UserControl
{
    public RuntimeButton()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Text content
        var text = GetProperty(d, "text", d.Name);
        ButtonText.Text = text;
        
        // Colors (designer may use hex or named colors e.g. "Black")
        var backColor = GetProperty(d, "backColor", "#F0F0F0");
        TheButton.Background = ColorParser.ParseBrush(backColor);

        // Text color: use textColor from designer first, then foreColor (so text is independent of background)
        var textColor = GetProperty(d, "textColor", "");
        if (string.IsNullOrEmpty(textColor)) textColor = GetProperty(d, "foreColor", "#000000");
        var textBrush = ColorParser.ParseBrush(textColor);
        TheButton.Foreground = textBrush;
        ButtonText.Foreground = textBrush;

        var borderColor = GetProperty(d, "borderColor", "#808080");
        TheButton.BorderBrush = ColorParser.ParseBrush(borderColor);
        
        // Border width
        var borderWidth = GetPropertyInt(d, "borderWidth", 1);
        TheButton.BorderThickness = new Avalonia.Thickness(borderWidth);
        
        // Font properties: enforce minimum 12pt so button text is always readable
        var fontName = GetProperty(d, "font", "Arial");
        var fontSize = Math.Max(12, GetPropertyDouble(d, "fontSize", 12.0));
        var fontStyleStr = GetProperty(d, "fontStyle", "Regular");
        
        var fontWeight = FontWeight.Normal;
        var fontStyle = FontStyle.Normal;
        
        if (fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase))
            fontWeight = FontWeight.Bold;
        if (fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase))
            fontStyle = FontStyle.Italic;
        
        TheButton.FontFamily = new FontFamily(fontName);
        TheButton.FontSize = fontSize;
        TheButton.FontWeight = fontWeight;
        TheButton.FontStyle = fontStyle;
        
        // Also apply to TextBlock
        ButtonText.FontFamily = new FontFamily(fontName);
        ButtonText.FontSize = fontSize;
        ButtonText.FontWeight = fontWeight;
        ButtonText.FontStyle = fontStyle;
        
        // Enabled and Visible
        TheButton.IsEnabled = d.Enabled;
        TheButton.IsVisible = d.Visible;
    }

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => TheButton.Click += value;
        remove => TheButton.Click -= value;
    }

    public string? ComponentId { get; set; }
    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static int GetPropertyInt(ComponentDescriptor d, string key, int fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return (int)dbl;
        if (v is float f) return (int)f;
        return int.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }

    private static double GetPropertyDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }

}
