using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
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
        TheButton.Content = GetProperty(d, "text", d.Name);
        
        // Colors
        var backColor = GetProperty(d, "backColor", "#F0F0F0");
        TheButton.Background = ParseBrush(backColor);
        
        var foreColor = GetProperty(d, "foreColor", "#000000");
        TheButton.Foreground = ParseBrush(foreColor);
        
        var borderColor = GetProperty(d, "borderColor", "#808080");
        TheButton.BorderBrush = ParseBrush(borderColor);
        
        // Border width
        var borderWidth = GetPropertyInt(d, "borderWidth", 1);
        TheButton.BorderThickness = new Avalonia.Thickness(borderWidth);
        
        // Font properties
        var fontName = GetProperty(d, "font", "Arial");
        var fontSize = GetPropertyDouble(d, "fontSize", 9.0);
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

    private static IBrush ParseBrush(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return new SolidColorBrush(Colors.Gray);
        
        // Handle hex colors with or without #
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
                return new SolidColorBrush(Colors.Gray);
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }
}
