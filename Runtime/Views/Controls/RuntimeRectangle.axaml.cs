using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeRectangle : UserControl
{
    public RuntimeRectangle()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        // Designer uses fillColor; fallback to color / backgroundColor
        var fillColor = GetProperty(d, "fillColor", "");
        if (string.IsNullOrEmpty(fillColor)) fillColor = GetProperty(d, "color", "");
        if (string.IsNullOrEmpty(fillColor)) fillColor = GetProperty(d, "backgroundColor", "#e0e0e0");
        TheBorder.Background = ParseBrush(fillColor);
        var borderColor = GetProperty(d, "borderColor", "");
        if (!string.IsNullOrEmpty(borderColor))
        {
            TheBorder.BorderBrush = ParseBrush(borderColor);
            var borderWidth = GetPropertyInt(d, "borderWidth", 1);
            TheBorder.BorderThickness = new Avalonia.Thickness(borderWidth);
        }
        IsVisible = d.Visible;
    }
    
    private static int GetPropertyInt(ComponentDescriptor d, string key, int fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return (int)dbl;
        if (v is float f) return (int)f;
        return int.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static IBrush ParseBrush(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return new SolidColorBrush(Colors.Gray);
        hex = hex.Trim();
        if (hex.Equals("null", StringComparison.OrdinalIgnoreCase)) return new SolidColorBrush(Colors.Gray);
        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (hex.Length >= 7)
        {
            var hexPart = hex.Length >= 7 ? hex.Substring(1, 6) : "";
            if (hexPart.Length == 6 && IsHexDigits(hexPart))
            {
                var r = Convert.ToInt32(hexPart.Substring(0, 2), 16);
                var g = Convert.ToInt32(hexPart.Substring(2, 2), 16);
                var b = Convert.ToInt32(hexPart.Substring(4, 2), 16);
                return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    private static bool IsHexDigits(ReadOnlySpan<char> s)
    {
        foreach (var c in s)
        {
            if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                continue;
            return false;
        }
        return true;
    }
}
