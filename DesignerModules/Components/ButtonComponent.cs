using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Button component for screens.
/// </summary>
public class ButtonComponent : BaseComponent
{
    public override string ComponentType => "Button";

    public string Text { get; set; } = "Button";
    public Color BackColor { get; set; } = Color.FromArgb(240, 240, 240);
    public Color ForeColor { get; set; } = Color.Black;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;
    public Font Font { get; set; } = new Font("Arial", 9);
    public string Action { get; set; } = string.Empty; // Script or action to execute

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        var brush = new SolidBrush(BackColor);
        var pen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);

        // Draw background
        g.FillRectangle(brush, rect);

        // Draw border
        g.DrawRectangle(pen, rect);

        // Draw text
        var textBrush = new SolidBrush(ForeColor);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(Text, Font, textBrush, rect, stringFormat);

        brush.Dispose();
        pen.Dispose();
        textBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new ButtonComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            Text = Text,
            BackColor = BackColor,
            ForeColor = ForeColor,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style),
            Action = Action
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["text"] = Text;
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        json["action"] = Action;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Text = json["text"]?.ToString() ?? "Button";
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#F0F0F0") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#000000") is Color foreColor)
            ForeColor = foreColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;

        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
        Action = json["action"]?.ToString() ?? string.Empty;

        var fontName = json["font"]?.ToString() ?? "Arial";
        var fontSize = json["fontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }
    }
}
