using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTextLabel : UserControl
{
    public RuntimeTextLabel()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        TheText.Text = GetProperty(d, "text", d.Name);
        
        // Text color: use textColor from designer first, then foreColor (supports "Black" and hex)
        var textColor = GetProperty(d, "textColor", "");
        if (string.IsNullOrEmpty(textColor)) textColor = GetProperty(d, "foreColor", "#000000");
        TheText.Foreground = ColorParser.ParseBrush(textColor);
        
        var backColor = GetProperty(d, "backColor", "");
        if (!string.IsNullOrEmpty(backColor))
            TheText.Background = ColorParser.ParseBrush(backColor);
        
        // Apply font from designer; enforce minimum 12pt so text is always readable
        var fontName = GetProperty(d, "font", "Arial");
        var fontSize = GetPropertyDouble(d, "fontSize", 12);
        if (fontSize <= 0) fontSize = 12;
        TheText.FontSize = Math.Max(12, fontSize);
        var fontStyleStr = GetProperty(d, "fontStyle", "Regular");

        TheText.FontFamily = new FontFamily(fontName);

        var fontWeight = FontWeight.Normal;
        var fontStyle = FontStyle.Normal;
        if (fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase))
            fontWeight = FontWeight.Bold;
        if (fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase))
            fontStyle = FontStyle.Italic;
        TheText.FontWeight = fontWeight;
        TheText.FontStyle = fontStyle;
        
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }
    
    private static double GetPropertyDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
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
}
