using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
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
    private bool _deferredUpdateScheduled;

    public RuntimeGaugeView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        AttachedToVisualTree += OnAttachedToVisualTree;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Ensure gauge is drawn after layout has run and we have a real size
        Avalonia.Threading.Dispatcher.UIThread.Post(UpdateGauge, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Run UpdateGauge once after layout so we have valid Bounds
        Avalonia.Threading.Dispatcher.UIThread.Post(UpdateGauge, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateGauge();
    }

    /// <summary>Ensure we never return NaN or invalid size from measure (avoids InvalidOperationException in layout).</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        double w = availableSize.Width;
        double h = availableSize.Height;
        if (double.IsNaN(w) || w <= 0 || double.IsPositiveInfinity(w)) w = 100;
        if (double.IsNaN(h) || h <= 0 || double.IsPositiveInfinity(h)) h = 100;
        var validSize = new Size(w, h);
        try
        {
            var result = base.MeasureOverride(validSize);
            // Coerce result in case content returned invalid size
            var rw = result.Width;
            var rh = result.Height;
            if (double.IsNaN(rw) || rw < 0) rw = w;
            if (double.IsNaN(rh) || rh < 0) rh = h;
            return new Size(rw, rh);
        }
        catch (InvalidOperationException)
        {
            return validSize;
        }
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Get properties
        _value = GetPropertyDouble(d, "value", 50.0);
        _minimum = GetPropertyDouble(d, "minimum", 0.0);
        _maximum = GetPropertyDouble(d, "maximum", 100.0);
        _backColor = ColorParser.ParseColor(GetProperty(d, "backColor", "White"));
        _foreColor = ColorParser.ParseColor(GetProperty(d, "foreColor", "Green"));
        _needleColor = ColorParser.ParseColor(GetProperty(d, "needleColor", "Red"));
        _textColor = ColorParser.ParseColor(GetProperty(d, "textColor", "Black"));
        _showValue = GetPropertyBool(d, "showValue", true);
        _showMinMax = GetPropertyBool(d, "showMinMax", true);
        _unit = GetProperty(d, "unit", "");
        
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        UpdateGauge();
    }

    public void SetValue(object? value)
    {
        // Only update _value when we have a valid tag value; keep previous value when null (e.g. tag not connected yet)
        if (value is int i)
            _value = Math.Clamp(i, _minimum, _maximum);
        else if (value is long l)
            _value = Math.Clamp((double)l, _minimum, _maximum);
        else if (value is double d)
            _value = Math.Clamp(d, _minimum, _maximum);
        else if (value is float f)
            _value = Math.Clamp((double)f, _minimum, _maximum);
        else if (value is decimal dec)
            _value = Math.Clamp((double)dec, _minimum, _maximum);
        else if (value != null && double.TryParse(value.ToString(), out var parsed))
            _value = Math.Clamp(parsed, _minimum, _maximum);
        UpdateGauge();
    }

    private void UpdateGauge()
    {
        // Prefer Bounds; fall back to Width/Height (may be set before layout). Reject NaN/zero.
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || double.IsNaN(w)) w = Width;
        if (h <= 0 || double.IsNaN(h)) h = Height;
        if (w <= 0 || double.IsNaN(w) || h <= 0 || double.IsNaN(h))
        {
            w = Math.Max(50, w);
            h = Math.Max(50, h);
            if (w <= 0) w = 100;
            if (h <= 0) h = 100;
            if (!_deferredUpdateScheduled)
            {
                _deferredUpdateScheduled = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _deferredUpdateScheduled = false;
                    UpdateGauge();
                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }
        }
        var width = Math.Max(50, w);
        var height = Math.Max(50, h);
        
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
        
        // Gauge spans 270 degrees (from -135 to +135), GaugeControl1-style: fill path with RotateTransform
        const double startAngle = -135;
        double sweepAngle = normalizedValue * 270;
        double angle = startAngle + sweepAngle;
        double angleRad = angle * Math.PI / 180.0;
        
        // GaugeControl1-style: fixed wedge (2° slice) at origin, rotate then translate so rotation is around gauge center
        const double wedgeSpanDeg = 2.0;
        double r0 = wedgeSpanDeg * -0.5 * Math.PI / 180.0;
        double r1 = wedgeSpanDeg * 0.5 * Math.PI / 180.0;
        var centerPt = new Point(0, 0);
        var p0 = new Point(radius * Math.Cos(r0), radius * Math.Sin(r0));
        var p1 = new Point(radius * Math.Cos(r1), radius * Math.Sin(r1));
        var wedgeFigure = new PathFigure { StartPoint = centerPt, IsClosed = true };
        wedgeFigure.Segments.Add(new LineSegment { Point = p0 });
        wedgeFigure.Segments.Add(new ArcSegment
        {
            Point = p1,
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = false
        });
        var wedgeGeometry = new PathGeometry();
        wedgeGeometry.Figures.Add(wedgeFigure);
        GaugeArc.Data = wedgeGeometry;
        GaugeArc.Fill = new SolidColorBrush(_foreColor);
        GaugeArc.Stroke = null;
        GaugeArc.Width = width;
        GaugeArc.Height = height;
        // Rotate wedge then translate to center (like fillPathRT in GaugeControl1)
        GaugeArc.RenderTransform = new TransformGroup
        {
            Children =
            {
                new RotateTransform(startAngle + sweepAngle),
                new TranslateTransform(centerX, centerY)
            }
        };
        
        // Draw needle (GaugeControls-style: center to point on circle by angle)
        double needleLength = radius * 0.7;
        double needleEndX = centerX + needleLength * Math.Cos(angleRad);
        double needleEndY = centerY + needleLength * Math.Sin(angleRad);
        
        NeedleLine.StartPoint = new Point(centerX, centerY);
        NeedleLine.EndPoint = new Point(needleEndX, needleEndY);
        NeedleLine.Stroke = new SolidColorBrush(_needleColor);
        NeedleLine.StrokeThickness = 3;
        NeedleLine.Width = width;
        NeedleLine.Height = height;
        
        // Center dot (pivot point)
        Canvas.SetLeft(CenterDot, centerX - 5);
        Canvas.SetTop(CenterDot, centerY - 5);
        CenterDot.Fill = new SolidColorBrush(_needleColor);
        
        // Needle tip ellipse (GaugeControl2-style: circle at end of needle)
        const double needleTipRadius = 6;
        NeedleTip.Width = needleTipRadius * 2;
        NeedleTip.Height = needleTipRadius * 2;
        Canvas.SetLeft(NeedleTip, needleEndX - needleTipRadius);
        Canvas.SetTop(NeedleTip, needleEndY - needleTipRadius);
        NeedleTip.Fill = new SolidColorBrush(_needleColor);
        
        // Min/Max labels
        if (_showMinMax)
        {
            MinLabel.IsVisible = true;
            MaxLabel.IsVisible = true;
            MinLabel.Text = _minimum.ToString("F0");
            MinLabel.FontSize = 10;
            Canvas.SetLeft(MinLabel, centerX - radius + 5);
            Canvas.SetTop(MinLabel, centerY + radius - 20);
            MinLabel.Foreground = new SolidColorBrush(_textColor);

            MaxLabel.Text = _maximum.ToString("F0");
            MaxLabel.FontSize = 10;
            Canvas.SetLeft(MaxLabel, centerX + radius - 30);
            Canvas.SetTop(MaxLabel, centerY + radius - 20);
            MaxLabel.Foreground = new SolidColorBrush(_textColor);
        }
        else
        {
            MinLabel.IsVisible = false;
            MaxLabel.IsVisible = false;
        }

        // Value text (ensure on top so not hidden behind arc/needle)
        if (_showValue)
        {
            ValueText.IsVisible = true;
            string valueText = $"{_value:F1}";
            if (!string.IsNullOrEmpty(_unit))
                valueText += $" {_unit}";
            ValueText.Text = valueText;
            ValueText.FontSize = 14;
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

        GaugeCanvas.InvalidateVisual();
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
}
