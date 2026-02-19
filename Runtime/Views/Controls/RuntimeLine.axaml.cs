using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeLine : UserControl
{
    public RuntimeLine()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        // Designer uses lineColor and lineWidth
        var lineColor = GetProperty(d, "lineColor", "");
        if (string.IsNullOrEmpty(lineColor)) lineColor = GetProperty(d, "color", "#808080");
        TheLine.Background = ParseBrush(lineColor);
        var thickness = Math.Max(1, GetPropDouble(d, "lineWidth", GetPropDouble(d, "thickness", 1.0)));
        var vertical = GetProperty(d, "orientation", "horizontal").Equals("vertical", StringComparison.OrdinalIgnoreCase);
        if (vertical)
        {
            TheLine.Width = thickness;
            TheLine.Height = d.Height > 0 ? d.Height : 50;
        }
        else
        {
            TheLine.Width = d.Width > 0 ? d.Width : 100;
            TheLine.Height = thickness;
        }
        IsVisible = d.Visible;
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static double GetPropDouble(ComponentDescriptor d, string key, double fallback)
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
        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (hex.Length >= 7)
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
        }
        return new SolidColorBrush(Colors.Gray);
    }
}
