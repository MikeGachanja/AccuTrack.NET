using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTableView : UserControl
{
    private HistorianQueryHelper? _historianQuery;
    private ComponentDescriptor? _descriptor;

    public RuntimeTableView()
    {
        InitializeComponent();
    }

    public void SetHistorianQuery(HistorianQueryHelper? helper)
    {
        _historianQuery = helper;
        LoadData();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        _descriptor = d;
        TitleText.Text = GetProperty(d, "title", "Table");
        var fontName = GetProperty(d, "headerFont", "Arial");
        var fontSize = GetPropDouble(d, "headerFontSize", 12);
        if (fontSize <= 0) fontSize = 12;
        var fontStyleStr = GetProperty(d, "headerFontStyle", "Bold");
        TitleText.FontFamily = new FontFamily(fontName);
        TitleText.FontSize = fontSize;
        TitleText.FontWeight = fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.SemiBold;
        TitleText.FontStyle = fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        LoadData();
    }

    private void LoadData()
    {
        if (DataPanel == null || _descriptor == null) return;
        DataPanel.Children.Clear();

        var dataSource = GetProperty(_descriptor, "dataSource", "TagData");
        var showHeader = GetPropertyBool(_descriptor, "showHeader", true);

        if (dataSource == "Historian" && _historianQuery != null)
        {
            var tagName = GetProperty(_descriptor, "historianTagName", "");
            var timeRangeMin = (int)GetPropDouble(_descriptor, "historianTimeRangeMinutes", 60);
            if (string.IsNullOrEmpty(tagName))
            {
                AddTextRow(DataPanel, "(No historian tag configured)", 1);
                return;
            }
            var endMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var startMs = endMs - timeRangeMin * 60 * 1000L;
            var data = _historianQuery.QueryTagValues(tagName, startMs, endMs, 500);
            var headers = GetColumnHeaders();
            if (headers.Count == 0) headers = new List<string> { "Timestamp", "Value", "Quality" };
            var colWidths = GetColumnWidths(headers.Count);
            if (showHeader)
                AddHeaderRow(DataPanel, headers, colWidths);
            foreach (var (ts, value, quality) in data)
            {
                var timeStr = DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                AddDataRow(DataPanel, new[] { timeStr, value?.ToString() ?? "", quality.ToString() }, headers.Count, colWidths);
            }
            if (data.Count == 0)
                AddTextRow(DataPanel, "No historian data for tag: " + tagName, 1);
        }
        else
        {
            var headers = GetColumnHeaders();
            var rows = GetStaticRows();
            if (headers.Count > 0)
            {
                var colWidths = GetColumnWidths(headers.Count);
                if (showHeader)
                    AddHeaderRow(DataPanel, headers, colWidths);
                foreach (var row in rows)
                    AddDataRow(DataPanel, row, headers.Count, colWidths);
            }
            if (headers.Count == 0 && rows.Count == 0)
                AddTextRow(DataPanel, "No data", 1);
        }
    }

    private List<string> GetColumnHeaders()
    {
        if (_descriptor == null || !_descriptor.Properties.TryGetValue("columnHeaders", out var v) || v is not IList<object?> list) return new List<string>();
        return list.Select(x => x?.ToString() ?? "").ToList();
    }

    private List<string[]> GetStaticRows()
    {
        if (_descriptor == null || !_descriptor.Properties.TryGetValue("rows", out var v) || v is not IList<object?> list) return new List<string[]>();
        var rows = new List<string[]>();
        foreach (var item in list)
        {
            if (item is IList<object?> row)
                rows.Add(row.Select(c => c?.ToString() ?? "").ToArray());
        }
        return rows;
    }

    private static int[] GetColumnWidths(int colCount)
    {
        var widths = new[] { 180, 100, 80 };
        var result = new int[colCount];
        for (int i = 0; i < colCount; i++)
            result[i] = i < widths.Length ? widths[i] : 80;
        return result;
    }

    private static void AddHeaderRow(StackPanel panel, IList<string> headers, int[] colWidths)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        for (int i = 0; i < headers.Count; i++)
        {
            var w = i < colWidths.Length ? colWidths[i] : 80;
            row.Children.Add(new Border
            {
                Width = w,
                MinWidth = w,
                MaxWidth = w,
                Background = new SolidColorBrush(Color.FromRgb(211, 211, 211)),
                Padding = new Thickness(6, 4),
                Margin = new Thickness(0, 0, 1, 1),
                Child = new TextBlock { Text = headers[i], FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Colors.Black), TextWrapping = TextWrapping.NoWrap }
            });
        }
        panel.Children.Add(row);
    }

    private static void AddDataRow(StackPanel panel, string[] cells, int colCount, int[] colWidths)
    {
        var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        for (int i = 0; i < colCount; i++)
        {
            var cell = i < cells.Length ? cells[i] : "";
            var w = i < colWidths.Length ? colWidths[i] : 80;
            row.Children.Add(new Border
            {
                Width = w,
                MinWidth = w,
                MaxWidth = w,
                Background = new SolidColorBrush(Colors.White),
                Padding = new Thickness(6, 4),
                Margin = new Thickness(0, 0, 1, 1),
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Child = new TextBlock { Text = cell, Foreground = new SolidColorBrush(Colors.Black), TextWrapping = TextWrapping.NoWrap }
            });
        }
        panel.Children.Add(row);
    }

    private static void AddTextRow(StackPanel panel, string text, int span)
    {
        panel.Children.Add(new TextBlock { Text = text, Foreground = new SolidColorBrush(Colors.Gray), Margin = new Thickness(0, 4, 0, 0) });
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

    private static bool GetPropertyBool(ComponentDescriptor d, string key, bool fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is bool b) return b;
        return bool.TryParse(v?.ToString(), out var parsed) && parsed;
    }
}
