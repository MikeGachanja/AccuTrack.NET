using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Console component for displaying log output on screens (e.g. Logs screen).
/// </summary>
public class ConsoleComponent : BaseComponent
{
    public override string ComponentType => "Console";

    public Color BackColor { get; set; } = Color.FromArgb(30, 30, 30);
    public Color ForeColor { get; set; } = Color.FromArgb(220, 220, 220);
    public Color BorderColor { get; set; } = Color.Gray;
    public int MaxLines { get; set; } = 500;
    public Font Font { get; set; } = new Font("Consolas", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;

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
            var pen = new Pen(BorderColor, 1);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }

        var textBrush = new SolidBrush(ForeColor);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near
        };
        g.DrawString("Console (log output)", Font, textBrush, new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 12), stringFormat);

        textBrush.Dispose();
        backBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new ConsoleComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            BackColor = BackColor,
            ForeColor = ForeColor,
            BorderColor = BorderColor,
            MaxLines = MaxLines,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["maxLines"] = MaxLines;
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);

        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#1E1E1E") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#DCDCDC") is Color foreColor)
            ForeColor = foreColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;

        MaxLines = json["maxLines"]?.ToObject<int>() ?? 500;

        var fontName = json["font"]?.ToString() ?? "Consolas";
        var fontSize = json["fontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }
    }
}
