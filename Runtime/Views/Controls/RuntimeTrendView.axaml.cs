using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTrendView : UserControl
{
    private HistorianQueryHelper? _historianQuery;
    private ComponentDescriptor? _descriptor;
    private readonly List<(string TagName, string Color, int LineWidth, string ChartType, List<(long Ts, double Value)> Points)> _chartSeries = new();
    private bool _drawingChart;

    public RuntimeTrendView()
    {
        InitializeComponent();
        if (ChartCanvas != null)
            ChartCanvas.SizeChanged += OnChartCanvasSizeChanged;
    }

    private void OnChartCanvasSizeChanged(object? sender, EventArgs e)
    {
        if (_drawingChart) return;
        DrawChart();
    }

    public string? TagName { get; set; }

    public void SetHistorianQuery(HistorianQueryHelper? helper)
    {
        _historianQuery = helper;
        LoadData();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        _descriptor = d;
        LabelText.Text = GetProperty(d, "label", "Trend");
        TagText.Text = string.IsNullOrEmpty(d.TagName) ? "" : "Tag: " + d.TagName;
        TagText.IsVisible = !string.IsNullOrEmpty(d.TagName);
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        LoadData();
    }

    private void LoadData()
    {
        if (ChartCanvas == null || _descriptor == null) return;
        _chartSeries.Clear();
        ChartCanvas.Children.Clear();
        if (LegendPanel != null) { LegendPanel.Children.Clear(); LegendPanel.IsVisible = false; }

        var dataSource = GetProperty(_descriptor, "dataSource", "Live");
        var timeRangeMin = (int)GetPropDouble(_descriptor, "timeRangeMinutes", 60);
        var seriesList = GetTrendSeries();
        if (seriesList.Count == 0)
        {
            ChartCanvas.Children.Add(new TextBlock
            {
                Text = "No trend series configured.",
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Avalonia.Thickness(8, 8, 0, 0)
            });
            return;
        }

        if (dataSource == "Historian" && _historianQuery != null)
        {
            var endMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var startMs = endMs - timeRangeMin * 60 * 1000L;
            foreach (var series in seriesList)
            {
                var tagName = series.TagName;
                if (string.IsNullOrEmpty(tagName)) continue;
                var data = _historianQuery.QueryTagValues(tagName, startMs, endMs, 500);
                var points = new List<(long Ts, double Value)>();
                foreach (var (ts, value, _) in data)
                {
                    var v = 0.0;
                    if (value is double d) v = d;
                    else if (value is int i) v = i;
                    else if (value != null && double.TryParse(value.ToString(), out var parsed)) v = parsed;
                    points.Add((ts, v));
                }
                _chartSeries.Add((tagName, series.Color, series.LineWidth, series.ChartType, points));
            }
            DrawChart();
            DrawLegend();
        }
        else
        {
            ChartCanvas.Children.Add(new TextBlock
            {
                Text = "Configure data source as Historian and add trend series (tags).",
                Foreground = new SolidColorBrush(Colors.Gray),
                Margin = new Avalonia.Thickness(8, 8, 0, 0)
            });
        }
    }

    private void DrawChart()
    {
        if (ChartCanvas == null || _descriptor == null || _chartSeries.Count == 0) return;
        var w = ChartCanvas.Bounds.Width;
        var h = ChartCanvas.Bounds.Height;
        if (w <= 0 || h <= 0) return;
        if (_drawingChart) return;
        _drawingChart = true;
        try
        {
        ChartCanvas.Children.Clear();
        var padding = 40;
        var chartW = Math.Max(1, w - padding * 2);
        var chartH = Math.Max(1, h - padding * 2);

        var yMin = GetPropDouble(_descriptor, "yAxisMin", 0);
        var yMax = GetPropDouble(_descriptor, "yAxisMax", 100);
        if (yMax <= yMin) yMax = yMin + 1;
        var showGrid = GetPropBool(_descriptor, "showGrid", true);

        if (showGrid)
        {
            for (int g = 0; g <= 5; g++)
            {
                var y = padding + chartH * g / 5;
                ChartCanvas.Children.Add(new Line
                {
                    StartPoint = new Point(padding, y),
                    EndPoint = new Point(padding + chartW, y),
                    Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    StrokeThickness = 1
                });
            }
            for (int g = 0; g <= 5; g++)
            {
                var x = padding + chartW * g / 5;
                ChartCanvas.Children.Add(new Line
                {
                    StartPoint = new Point(x, padding),
                    EndPoint = new Point(x, padding + chartH),
                    Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    StrokeThickness = 1
                });
            }
        }

        long tMin = long.MaxValue, tMax = long.MinValue;
        foreach (var (_, _, _, _, pts) in _chartSeries)
        {
            foreach (var (ts, _) in pts)
            {
                if (ts < tMin) tMin = ts;
                if (ts > tMax) tMax = ts;
            }
        }
        if (tMax <= tMin) tMax = tMin + 1;

        foreach (var (tagName, colorStr, lineWidth, _, points) in _chartSeries)
        {
            if (points.Count == 0) continue;
            var color = ParseColor(colorStr);
            var ptList = new List<Point>();
            foreach (var (ts, value) in points)
            {
                var x = padding + (ts - tMin) * chartW / (tMax - tMin);
                var yNorm = (value - yMin) / (yMax - yMin);
                yNorm = Math.Max(0, Math.Min(1, yNorm));
                var y = padding + chartH * (1 - yNorm);
                ptList.Add(new Point(x, y));
            }
            if (ptList.Count >= 2)
            {
                var pointsCollection = new List<Point>(ptList);
                ChartCanvas.Children.Add(new Polyline
                {
                    Points = pointsCollection,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = Math.Max(1, lineWidth)
                });
            }
        }

        var yAxisLabel = GetProperty(_descriptor, "yAxisLabel", "Value");
        var tbMax = new TextBlock { Text = yMax.ToString("F0"), FontSize = 9, Foreground = new SolidColorBrush(Colors.Gray) };
        Canvas.SetLeft(tbMax, 2);
        Canvas.SetTop(tbMax, padding - 8);
        ChartCanvas.Children.Add(tbMax);
        var tbMin = new TextBlock { Text = yMin.ToString("F0"), FontSize = 9, Foreground = new SolidColorBrush(Colors.Gray) };
        Canvas.SetLeft(tbMin, 2);
        Canvas.SetTop(tbMin, padding + chartH - 4);
        ChartCanvas.Children.Add(tbMin);
        var tbLabel = new TextBlock { Text = yAxisLabel, FontSize = 9, Foreground = new SolidColorBrush(Colors.Gray) };
        Canvas.SetLeft(tbLabel, 2);
        Canvas.SetTop(tbLabel, padding + chartH / 2 - 6);
        ChartCanvas.Children.Add(tbLabel);
        }
        finally { _drawingChart = false; }
    }

    private void DrawLegend()
    {
        if (LegendPanel == null || !GetPropBool(_descriptor!, "showLegend", true)) return;
        LegendPanel.Children.Clear();
        foreach (var (tagName, colorStr, _, _, _) in _chartSeries)
        {
            var color = ParseColor(colorStr);
            var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Avalonia.Thickness(0, 0, 16, 0) };
            panel.Children.Add(new Border
            {
                Width = 16,
                Height = 10,
                Background = new SolidColorBrush(color),
                CornerRadius = new Avalonia.CornerRadius(1),
                Margin = new Avalonia.Thickness(0, 0, 4, 0)
            });
            panel.Children.Add(new TextBlock { Text = tagName, FontSize = 10, Foreground = new SolidColorBrush(Colors.Black) });
            LegendPanel.Children.Add(panel);
        }
        LegendPanel.IsVisible = LegendPanel.Children.Count > 0;
    }

    private static Color ParseColor(string nameOrHex)
    {
        if (string.IsNullOrEmpty(nameOrHex)) return Colors.Blue;
        if (nameOrHex.StartsWith("#") && nameOrHex.Length >= 7)
        {
            try
            {
                var r = Convert.ToInt32(nameOrHex.Substring(1, 2), 16);
                var g = Convert.ToInt32(nameOrHex.Substring(3, 2), 16);
                var b = Convert.ToInt32(nameOrHex.Substring(5, 2), 16);
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            catch { return Colors.Blue; }
        }
        return nameOrHex.ToLowerInvariant() switch
        {
            "blue" => Colors.Blue,
            "red" => Colors.Red,
            "green" => Colors.Green,
            "orange" => Color.FromRgb(255, 165, 0),
            "gray" or "grey" => Colors.Gray,
            _ => Colors.Blue
        };
    }

    private List<(string TagName, string Color, int LineWidth, string ChartType)> GetTrendSeries()
    {
        var result = new List<(string, string, int, string)>();
        if (_descriptor == null || !_descriptor.Properties.TryGetValue("trendSeries", out var v) || v is not IList<object?> list) return result;
        foreach (var item in list)
        {
            if (item is not IReadOnlyDictionary<string, object?> dict) continue;
            var tagName = dict.TryGetValue("tagName", out var tn) ? tn?.ToString() ?? "" : "";
            var color = dict.TryGetValue("color", out var c) ? c?.ToString() ?? "Blue" : "Blue";
            var lw = 2;
            if (dict.TryGetValue("lineWidth", out var lwObj))
            {
                if (lwObj is int i) lw = i;
                else if (lwObj is double d) lw = (int)d;
                else int.TryParse(lwObj?.ToString(), out lw);
            }
            var chartType = dict.TryGetValue("chartType", out var ct) ? ct?.ToString() ?? "Line" : "Line";
            result.Add((tagName, color, lw, chartType));
        }
        return result;
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
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }

    private static bool GetPropBool(ComponentDescriptor d, string key, bool fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is bool b) return b;
        return bool.TryParse(v?.ToString(), out var parsed) && parsed;
    }
}
