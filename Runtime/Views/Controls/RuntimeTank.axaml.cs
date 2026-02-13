using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTank : UserControl
{
    private double _percent = 50.0;
    private double _minimum = 0.0;
    private double _maximum = 100.0;
    private Color _tankColor = Color.FromRgb(224, 224, 224); // LightGray
    private Color _fillColor = Color.FromRgb(0, 170, 119); // Dark blue-green
    private Color _borderColor = Colors.Black;
    private bool _showLevel = true;
    private string _label = "Tank";
    private double _lastHeight = 0;

    public RuntimeTank()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateTank();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Get properties
        _minimum = GetPropertyDouble(d, "minimum", 0.0);
        _maximum = GetPropertyDouble(d, "maximum", 100.0);
        _tankColor = ParseColor(GetProperty(d, "tankColor", "#E0E0E0"));
        _fillColor = ParseColor(GetProperty(d, "fillColor", "#00AA77"));
        _borderColor = ParseColor(GetProperty(d, "borderColor", "#000000"));
        _showLevel = GetPropertyBool(d, "showLevel", true);
        _label = GetProperty(d, "label", "Tank");
        
        LabelText.Text = _label;
        LabelText.IsVisible = !string.IsNullOrEmpty(_label);
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        UpdateTank();
    }

    public void SetValue(object? value)
    {
        _percent = 0;
        if (value is int i) _percent = Math.Clamp(i, _minimum, _maximum);
        else if (value is double d) _percent = Math.Clamp(d, _minimum, _maximum);
        else if (value != null && double.TryParse(value.ToString(), out var parsed))
            _percent = Math.Clamp(parsed, _minimum, _maximum);
        UpdateTank();
    }

    private void UpdateTank()
    {
        var width = Bounds.Width > 0 ? Bounds.Width : 50;
        var height = Bounds.Height > 0 ? Bounds.Height : 100;
        
        if (width <= 0 || height <= 0) return;
        
        // Only update if height actually changed to avoid infinite loops
        if (Math.Abs(height - _lastHeight) < 0.1)
            return;
        
        _lastHeight = height;
        
        // Update tank border
        TankBorder.Background = new SolidColorBrush(_tankColor);
        TankBorder.BorderBrush = new SolidColorBrush(_borderColor);
        TankBorder.BorderThickness = new Avalonia.Thickness(2);
        TankBorder.CornerRadius = new Avalonia.CornerRadius(2);
        
        // Calculate fill level
        double range = _maximum - _minimum;
        double normalizedLevel = range > 0 ? (_percent - _minimum) / range : 0;
        normalizedLevel = Math.Max(0, Math.Min(1, normalizedLevel));
        
        // Update fill
        var fillHeight = height * normalizedLevel;
        FillBorder.Height = fillHeight;
        FillBorder.Background = new SolidColorBrush(_fillColor);
        FillBorder.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
        FillBorder.CornerRadius = new Avalonia.CornerRadius(0);
        
        // Update level text if shown
        if (_showLevel)
        {
            // Level text would be displayed in the tank area
            // For now, we'll show it in the label area or overlay
        }
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

    private static bool GetPropertyBool(ComponentDescriptor d, string key, bool fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is bool b) return b;
        return bool.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
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
}
