using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Line shape component.
/// </summary>
public class LineComponent : BaseComponent
{
    public override string ComponentType => "Line";

    public Color LineColor { get; set; } = Color.Black;
    public int LineWidth { get; set; } = 1;
    public LineStyle Style { get; set; } = LineStyle.Solid;
    public Point StartPoint { get; set; } = Point.Empty;
    public Point EndPoint { get; set; } = Point.Empty;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Calculate line endpoints
        Point start = StartPoint == Point.Empty ? new Point(rect.X, rect.Y) : new Point(rect.X + StartPoint.X, rect.Y + StartPoint.Y);
        Point end = EndPoint == Point.Empty ? new Point(rect.Right, rect.Bottom) : new Point(rect.X + EndPoint.X, rect.Y + EndPoint.Y);
        
        var pen = new Pen(isSelected ? Color.Blue : LineColor, LineWidth);
        
        switch (Style)
        {
            case LineStyle.Dashed:
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                break;
            case LineStyle.Dotted:
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                break;
            default:
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                break;
        }
        
        g.DrawLine(pen, start, end);
        pen.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new LineComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            LineColor = LineColor,
            LineWidth = LineWidth,
            Style = Style,
            StartPoint = StartPoint,
            EndPoint = EndPoint
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["lineColor"] = ColorTranslator.ToHtml(LineColor);
        json["lineWidth"] = LineWidth;
        json["style"] = Style.ToString();
        json["startPoint"] = new JObject
        {
            ["x"] = StartPoint.X,
            ["y"] = StartPoint.Y
        };
        json["endPoint"] = new JObject
        {
            ["x"] = EndPoint.X,
            ["y"] = EndPoint.Y
        };
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        if (ColorTranslator.FromHtml(json["lineColor"]?.ToString() ?? "#000000") is Color lineColor)
            LineColor = lineColor;
        
        LineWidth = json["lineWidth"]?.ToObject<int>() ?? 1;
        
        if (Enum.TryParse<LineStyle>(json["style"]?.ToString() ?? "Solid", out var style))
        {
            Style = style;
        }
        
        var startPointObj = json["startPoint"] as JObject;
        if (startPointObj != null)
        {
            StartPoint = new Point(
                startPointObj["x"]?.ToObject<int>() ?? 0,
                startPointObj["y"]?.ToObject<int>() ?? 0
            );
        }
        
        var endPointObj = json["endPoint"] as JObject;
        if (endPointObj != null)
        {
            EndPoint = new Point(
                endPointObj["x"]?.ToObject<int>() ?? 0,
                endPointObj["y"]?.ToObject<int>() ?? 0
            );
        }
    }
}

public enum LineStyle
{
    Solid,
    Dashed,
    Dotted
}
