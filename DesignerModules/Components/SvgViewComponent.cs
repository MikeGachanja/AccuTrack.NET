using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// SVG view component for displaying SVG files.
/// </summary>
public class SvgViewComponent : BaseComponent
{
    public override string ComponentType => "SVGView";

    public string SvgPath { get; set; } = string.Empty;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Draw background
        var backBrush = new SolidBrush(Color.White);
        g.FillRectangle(backBrush, rect);
        
        // Draw placeholder for SVG (in Designer, we show a placeholder)
        // Actual SVG rendering happens in Runtime
        var placeholderBrush = new SolidBrush(Color.LightBlue);
        g.FillRectangle(placeholderBrush, rect);
        
        var textBrush = new SolidBrush(Color.DarkBlue);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        string displayText = string.IsNullOrEmpty(SvgPath) ? "SVG" : Path.GetFileNameWithoutExtension(SvgPath);
        g.DrawString(displayText, SystemFonts.DefaultFont, textBrush, rect, stringFormat);
        
        placeholderBrush.Dispose();
        textBrush.Dispose();

        // Draw border
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);
        g.DrawRectangle(borderPen, rect);
        borderPen.Dispose();

        backBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new SvgViewComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            SvgPath = SvgPath,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["svgPath"] = SvgPath;
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        SvgPath = json["svgPath"]?.ToString() ?? string.Empty;
        
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;
        
        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
    }
}
