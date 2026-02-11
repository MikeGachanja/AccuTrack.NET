using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Rectangle shape component.
/// </summary>
public class RectangleComponent : BaseComponent
{
    public override string ComponentType => "Rectangle";

    public Color FillColor { get; set; } = Color.LightBlue;
    public Color BorderColor { get; set; } = Color.Black;
    public int BorderWidth { get; set; } = 1;
    public bool Filled { get; set; } = true;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        var pen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);
        
        if (Filled)
        {
            var brush = new SolidBrush(FillColor);
            g.FillRectangle(brush, rect);
            brush.Dispose();
        }
        
        g.DrawRectangle(pen, rect);
        pen.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new RectangleComponent
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
            Filled = Filled
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["fillColor"] = ColorTranslator.ToHtml(FillColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        json["filled"] = Filled;
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
    }
}
