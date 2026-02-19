using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeText : UserControl
{
    public RuntimeText()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        TheText.Text = GetProperty(d, "text", d.Name);

        // Text color: use textColor first, then foreColor (supports "Black" and hex)
        var textColor = GetProperty(d, "textColor", "");
        if (string.IsNullOrEmpty(textColor)) textColor = GetProperty(d, "foreColor", "#333333");
        TheText.Foreground = ColorParser.ParseBrush(textColor);

        var backColor = GetProperty(d, "backColor", "");
        if (!string.IsNullOrEmpty(backColor))
            TheText.Background = ColorParser.ParseBrush(backColor);
        else
            TheText.Background = Brushes.Transparent;
        
        // Apply font properties if available
        var fontName = GetProperty(d, "font", "");
        var fontSize = GetPropertyDouble(d, "fontSize", 0);
        var fontStyleStr = GetProperty(d, "fontStyle", "");
        
        if (!string.IsNullOrEmpty(fontName))
        {
            TheText.FontFamily = new FontFamily(fontName);
        }
        if (fontSize > 0)
        {
            TheText.FontSize = fontSize;
        }
        if (!string.IsNullOrEmpty(fontStyleStr))
        {
            var fontWeight = FontWeight.Normal;
            var fontStyle = FontStyle.Normal;
            
            if (fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase))
                fontWeight = FontWeight.Bold;
            if (fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase))
                fontStyle = FontStyle.Italic;
                
            TheText.FontWeight = fontWeight;
            TheText.FontStyle = fontStyle;
        }
        
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    public void SetValue(object? value)
    {
        TheText.Text = value?.ToString() ?? "";
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
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
