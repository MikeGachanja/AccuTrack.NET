using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTriangle : UserControl
{
    public RuntimeTriangle()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        // Designer uses fillColor; fallback to color
        var fillColor = GetProperty(d, "fillColor", "");
        if (string.IsNullOrEmpty(fillColor)) fillColor = GetProperty(d, "color", "#808080");
        var fill = ParseBrush(fillColor);
        var w = d.Width > 0 ? d.Width : 50;
        var h = d.Height > 0 ? d.Height : 50;
        var pts = new List<Point> { new(w * 0.5, 0), new(w, h), new(0, h) };
        TheCanvas.Children.Clear();
        TheCanvas.Children.Add(new Polygon
        {
            Points = pts,
            Fill = fill
        });
        IsVisible = d.Visible;
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static IBrush ParseBrush(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Brushes.Gray;
        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (hex.Length >= 7)
        {
            var r = Convert.ToInt32(hex.Substring(1, 2), 16);
            var g = Convert.ToInt32(hex.Substring(3, 2), 16);
            var b = Convert.ToInt32(hex.Substring(5, 2), 16);
            return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
        }
        return Brushes.Gray;
    }
}
