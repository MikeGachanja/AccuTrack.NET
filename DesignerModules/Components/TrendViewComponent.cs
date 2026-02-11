using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Trend view component for displaying historical data trends.
/// </summary>
public class TrendViewComponent : BaseComponent
{
    public override string ComponentType => "TrendView";

    public List<string> TagNames { get; set; } = new List<string>();
    public int TimeRangeMinutes { get; set; } = 60;
    public Color BackColor { get; set; } = Color.White;
    public Color GridColor { get; set; } = Color.LightGray;
    public List<Color> LineColors { get; set; } = new List<Color> { Color.Blue, Color.Red, Color.Green, Color.Orange };
    public bool ShowGrid { get; set; } = true;
    public bool ShowLegend { get; set; } = true;
    public string YAxisLabel { get; set; } = "Value";
    public double YAxisMin { get; set; } = 0.0;
    public double YAxisMax { get; set; } = 100.0;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Draw background
        var backBrush = new SolidBrush(BackColor);
        g.FillRectangle(backBrush, rect);
        
        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }
        else
        {
            var pen = new Pen(Color.Gray, 1);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }

        // Draw grid
        if (ShowGrid)
        {
            var gridPen = new Pen(GridColor, 1);
            int gridLines = 5;
            
            // Horizontal grid lines
            for (int i = 0; i <= gridLines; i++)
            {
                int y = rect.Y + (rect.Height * i / gridLines);
                g.DrawLine(gridPen, rect.X, y, rect.Right, y);
            }
            
            // Vertical grid lines
            for (int i = 0; i <= gridLines; i++)
            {
                int x = rect.X + (rect.Width * i / gridLines);
                g.DrawLine(gridPen, x, rect.Y, x, rect.Bottom);
            }
            
            gridPen.Dispose();
        }

        // Draw placeholder trend lines (would be replaced with actual data)
        if (TagNames.Count > 0)
        {
            for (int i = 0; i < TagNames.Count && i < LineColors.Count; i++)
            {
                var linePen = new Pen(LineColors[i], 2);
                // Draw a sample sine wave as placeholder
                var points = new List<Point>();
                int pointsCount = 50;
                for (int j = 0; j < pointsCount; j++)
                {
                    double x = (double)j / pointsCount;
                    double y = 0.5 + 0.3 * Math.Sin(x * Math.PI * 4); // Sample sine wave
                    points.Add(new Point(
                        rect.X + (int)(x * rect.Width),
                        rect.Y + (int)((1 - y) * rect.Height)
                    ));
                }
                if (points.Count > 1)
                {
                    g.DrawLines(linePen, points.ToArray());
                }
                linePen.Dispose();
            }
        }

        // Draw legend
        if (ShowLegend && TagNames.Count > 0)
        {
            var legendFont = new Font(SystemFonts.DefaultFont.FontFamily, 8);
            int legendY = rect.Y + 5;
            for (int i = 0; i < TagNames.Count && i < LineColors.Count; i++)
            {
                var colorBrush = new SolidBrush(LineColors[i]);
                var colorRect = new Rectangle(rect.X + 5, legendY, 15, 10);
                g.FillRectangle(colorBrush, colorRect);
                g.DrawRectangle(Pens.Black, colorRect);
                
                var textBrush = new SolidBrush(Color.Black);
                g.DrawString(TagNames[i], legendFont, textBrush, rect.X + 25, legendY - 2);
                
                legendY += 15;
                colorBrush.Dispose();
                textBrush.Dispose();
            }
            legendFont.Dispose();
        }

        // Draw axis labels
        var labelFont = new Font(SystemFonts.DefaultFont.FontFamily, 8);
        var labelBrush = new SolidBrush(Color.Black);
        g.DrawString(YAxisMin.ToString("F0"), labelFont, labelBrush, rect.X + 2, rect.Bottom - 15);
        g.DrawString(YAxisMax.ToString("F0"), labelFont, labelBrush, rect.X + 2, rect.Y + 2);
        g.DrawString(YAxisLabel, labelFont, labelBrush, rect.X + 2, rect.Y + rect.Height / 2);
        labelFont.Dispose();
        labelBrush.Dispose();

        backBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new TrendViewComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            TagNames = new List<string>(TagNames),
            TimeRangeMinutes = TimeRangeMinutes,
            BackColor = BackColor,
            GridColor = GridColor,
            LineColors = new List<Color>(LineColors),
            ShowGrid = ShowGrid,
            ShowLegend = ShowLegend,
            YAxisLabel = YAxisLabel,
            YAxisMin = YAxisMin,
            YAxisMax = YAxisMax
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        var tagNamesArray = new JArray();
        foreach (var tagName in TagNames)
        {
            tagNamesArray.Add(tagName);
        }
        json["tagNames"] = tagNamesArray;
        
        var lineColorsArray = new JArray();
        foreach (var color in LineColors)
        {
            lineColorsArray.Add(ColorTranslator.ToHtml(color));
        }
        json["lineColors"] = lineColorsArray;
        
        json["timeRangeMinutes"] = TimeRangeMinutes;
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["gridColor"] = ColorTranslator.ToHtml(GridColor);
        json["showGrid"] = ShowGrid;
        json["showLegend"] = ShowLegend;
        json["yAxisLabel"] = YAxisLabel;
        json["yAxisMin"] = YAxisMin;
        json["yAxisMax"] = YAxisMax;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        TagNames.Clear();
        var tagNamesArray = json["tagNames"] as JArray;
        if (tagNamesArray != null)
        {
            foreach (var item in tagNamesArray)
            {
                TagNames.Add(item?.ToString() ?? string.Empty);
            }
        }

        LineColors.Clear();
        var lineColorsArray = json["lineColors"] as JArray;
        if (lineColorsArray != null)
        {
            foreach (var item in lineColorsArray)
            {
                if (ColorTranslator.FromHtml(item?.ToString() ?? "#0000FF") is Color color)
                {
                    LineColors.Add(color);
                }
            }
        }
        if (LineColors.Count == 0)
        {
            LineColors.AddRange(new[] { Color.Blue, Color.Red, Color.Green, Color.Orange });
        }

        TimeRangeMinutes = json["timeRangeMinutes"]?.ToObject<int>() ?? 60;
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#FFFFFF") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["gridColor"]?.ToString() ?? "#D3D3D3") is Color gridColor)
            GridColor = gridColor;

        ShowGrid = json["showGrid"]?.ToObject<bool>() ?? true;
        ShowLegend = json["showLegend"]?.ToObject<bool>() ?? true;
        YAxisLabel = json["yAxisLabel"]?.ToString() ?? "Value";
        YAxisMin = json["yAxisMin"]?.ToObject<double>() ?? 0.0;
        YAxisMax = json["yAxisMax"]?.ToObject<double>() ?? 100.0;
    }
}
