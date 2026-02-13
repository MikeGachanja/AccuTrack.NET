using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
        
        // Apply colors if available
        var foreColor = GetProperty(d, "foreColor", "");
        if (!string.IsNullOrEmpty(foreColor))
        {
            TheText.Foreground = ParseBrush(foreColor);
        }
        
        var backColor = GetProperty(d, "backColor", "");
        if (!string.IsNullOrEmpty(backColor))
        {
            TheText.Background = ParseBrush(backColor);
        }
        
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
    
    private static IBrush ParseBrush(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return new SolidColorBrush(Colors.Black);
        
        if (!hex.StartsWith("#"))
            hex = "#" + hex;
            
        if (hex.Length >= 7)
        {
            try
            {
                var r = Convert.ToInt32(hex.Substring(1, 2), 16);
                var g = Convert.ToInt32(hex.Substring(3, 2), 16);
                var b = Convert.ToInt32(hex.Substring(5, 2), 16);
                return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
            catch
            {
                return new SolidColorBrush(Colors.Black);
            }
        }
        return new SolidColorBrush(Colors.Black);
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
