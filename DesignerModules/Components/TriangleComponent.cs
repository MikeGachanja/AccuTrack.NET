using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Triangle shape component.
/// </summary>
public class TriangleComponent : BaseComponent
{
    public override string ComponentType => "Triangle";

    public Color FillColor { get; set; } = Color.LightBlue;
    public Color BorderColor { get; set; } = Color.Black;
    public int BorderWidth { get; set; } = 1;
    public bool Filled { get; set; } = true;
    public TriangleDirection Direction { get; set; } = TriangleDirection.Up;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Calculate triangle points based on direction
        Point[] points = Direction switch
        {
            TriangleDirection.Up => new[]
            {
                new Point(rect.X + rect.Width / 2, rect.Y),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.X, rect.Bottom)
            },
            TriangleDirection.Down => new[]
            {
                new Point(rect.X + rect.Width / 2, rect.Bottom),
                new Point(rect.Right, rect.Y),
                new Point(rect.X, rect.Y)
            },
            TriangleDirection.Left => new[]
            {
                new Point(rect.X, rect.Y + rect.Height / 2),
                new Point(rect.Right, rect.Y),
                new Point(rect.Right, rect.Bottom)
            },
            TriangleDirection.Right => new[]
            {
                new Point(rect.Right, rect.Y + rect.Height / 2),
                new Point(rect.X, rect.Y),
                new Point(rect.X, rect.Bottom)
            },
            _ => new[]
            {
                new Point(rect.X + rect.Width / 2, rect.Y),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.X, rect.Bottom)
            }
        };
        
        var pen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);
        
        if (Filled)
        {
            var brush = new SolidBrush(FillColor);
            g.FillPolygon(brush, points);
            brush.Dispose();
        }
        
        g.DrawPolygon(pen, points);
        pen.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new TriangleComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            FillColor = FillColor,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            Filled = Filled,
            Direction = Direction
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["fillColor"] = ColorTranslator.ToHtml(FillColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        json["filled"] = Filled;
        json["direction"] = Direction.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        if (ColorTranslator.FromHtml(json["fillColor"]?.ToString() ?? "#ADD8E6") is Color fillColor)
            FillColor = fillColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#000000") is Color borderColor)
            BorderColor = borderColor;
        
        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
        Filled = json["filled"]?.ToObject<bool>() ?? true;
        
        if (Enum.TryParse<TriangleDirection>(json["direction"]?.ToString() ?? "Up", out var direction))
        {
            Direction = direction;
        }
    }
}

public enum TriangleDirection
{
    Up,
    Down,
    Left,
    Right
}
