using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeGaugeView : UserControl
{
    private double _value = 50.0;
    private double _minimum = 0.0;
    private double _maximum = 100.0;
    private Color _backColor = Colors.White;
    private Color _foreColor = Colors.Green;
    private Color _needleColor = Colors.Red;
    private Color _textColor = Colors.Black;
    private bool _showValue = true;
    private bool _showMinMax = true;
    private string _unit = string.Empty;
    private double _lastWidth = 0;
    private double _lastHeight = 0;

    public RuntimeGaugeView()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateGauge();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Get properties
        _value = GetPropertyDouble(d, "value", 50.0);
        _minimum = GetPropertyDouble(d, "minimum", 0.0);
        _maximum = GetPropertyDouble(d, "maximum", 100.0);
        _backColor = ParseColor(GetProperty(d, "backColor", "#FFFFFF"));
        _foreColor = ParseColor(GetProperty(d, "foreColor", "#008000"));
        _needleColor = ParseColor(GetProperty(d, "needleColor", "#FF0000"));
        _textColor = ParseColor(GetProperty(d, "textColor", "#000000"));
        _showValue = GetPropertyBool(d, "showValue", true);
        _showMinMax = GetPropertyBool(d, "showMinMax", true);
        _unit = GetProperty(d, "unit", "");
        
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        UpdateGauge();
    }

    public void SetValue(object? value)
    {
        _value = 0;
        if (value is int i) _value = Math.Clamp(i, _minimum, _maximum);
        else if (value is double d) _value = Math.Clamp(d, _minimum, _maximum);
        else if (value != null && double.TryParse(value.ToString(), out var parsed))
            _value = Math.Clamp(parsed, _minimum, _maximum);
        UpdateGauge();
    }

    private void UpdateGauge()
    {
        var width = Bounds.Width > 0 ? Bounds.Width : 100;
        var height = Bounds.Height > 0 ? Bounds.Height : 100;
        
        if (width <= 0 || height <= 0) return;
        
        // Only update if size actually changed to avoid infinite loops
        if (Math.Abs(width - _lastWidth) < 0.1 && Math.Abs(height - _lastHeight) < 0.1)
            return;
        
        _lastWidth = width;
        _lastHeight = height;
        
        var size = Math.Min(width, height);
        var centerX = width / 2;
        var centerY = height / 2;
        var radius = size / 2 - 10;
        
        // Update background circle
        GaugeBackground.Width = size;
        GaugeBackground.Height = size;
        Canvas.SetLeft(GaugeBackground, centerX - size / 2);
        Canvas.SetTop(GaugeBackground, centerY - size / 2);
        GaugeBackground.Fill = new SolidColorBrush(_backColor);
        GaugeBackground.Stroke = new SolidColorBrush(Colors.Gray);
        
        // Calculate normalized value
        double range = _maximum - _minimum;
        double normalizedValue = range > 0 ? (_value - _minimum) / range : 0;
        normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
        
        // Gauge spans 270 degrees (from -135 to +135 degrees)
        double startAngle = -135;
        double sweepAngle = normalizedValue * 270;
        double angle = startAngle + sweepAngle;
        double angleRad = angle * Math.PI / 180.0;
        
        // Draw gauge arc using Path
        var arcRect = new Rect(centerX - radius, centerY - radius, radius * 2, radius * 2);
        var arcPath = new PathGeometry();
        var arcFigure = new PathFigure
        {
            StartPoint = new Point(
                centerX + radius * Math.Cos(startAngle * Math.PI / 180.0),
                centerY + radius * Math.Sin(startAngle * Math.PI / 180.0)
            )
        };
        
        var arcSegment = new ArcSegment
        {
            Point = new Point(
                centerX + radius * Math.Cos(angle * Math.PI / 180.0),
                centerY + radius * Math.Sin(angle * Math.PI / 180.0)
            ),
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = sweepAngle > 180
        };
        
        arcFigure.Segments.Add(arcSegment);
        arcPath.Figures.Add(arcFigure);
        
        GaugeArc.Data = arcPath;
        GaugeArc.Stroke = new SolidColorBrush(_foreColor);
        GaugeArc.StrokeThickness = 8;
        
        // Draw needle
        int needleLength = (int)(radius * 0.7);
        int needleEndX = (int)(centerX + needleLength * Math.Cos(angleRad));
        int needleEndY = (int)(centerY + needleLength * Math.Sin(angleRad));
        
        NeedleLine.StartPoint = new Point(centerX, centerY);
        NeedleLine.EndPoint = new Point(needleEndX, needleEndY);
        NeedleLine.Stroke = new SolidColorBrush(_needleColor);
        NeedleLine.StrokeThickness = 3;
        
        // Center dot
        Canvas.SetLeft(CenterDot, centerX - 5);
        Canvas.SetTop(CenterDot, centerY - 5);
        CenterDot.Fill = new SolidColorBrush(_needleColor);
        
        // Min/Max labels
        if (_showMinMax)
        {
            MinLabel.Text = _minimum.ToString("F0");
            Canvas.SetLeft(MinLabel, centerX - radius + 5);
            Canvas.SetTop(MinLabel, centerY + radius - 20);
            MinLabel.Foreground = new SolidColorBrush(_textColor);
            
            MaxLabel.Text = _maximum.ToString("F0");
            Canvas.SetLeft(MaxLabel, centerX + radius - 30);
            Canvas.SetTop(MaxLabel, centerY + radius - 20);
            MaxLabel.Foreground = new SolidColorBrush(_textColor);
        }
        else
        {
            MinLabel.IsVisible = false;
            MaxLabel.IsVisible = false;
        }
        
        // Value text
        if (_showValue)
        {
            string valueText = $"{_value:F1}";
            if (!string.IsNullOrEmpty(_unit))
                valueText += $" {_unit}";
            ValueText.Text = valueText;
            Canvas.SetLeft(ValueText, centerX - 30);
            Canvas.SetTop(ValueText, centerY + radius / 2 + 10);
            ValueText.Foreground = new SolidColorBrush(_textColor);
            ValueText.Width = 60;
            ValueText.TextAlignment = Avalonia.Media.TextAlignment.Center;
        }
        else
        {
            ValueText.IsVisible = false;
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
