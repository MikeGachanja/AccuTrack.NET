using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Text label component for displaying text on screens.
/// </summary>
public class TextLabelComponent : BaseComponent
{
    public override string ComponentType => "TextLabel";

    public string Text { get; set; } = "Label";
    public Color ForeColor { get; set; } = Color.Black;
    /// <summary>Color of the label text (independent of background).</summary>
    public Color TextColor { get; set; } = Color.Black;
    public Font Font { get; set; } = new Font("Arial", 9);
    public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;
    public bool AutoSize { get; set; } = false;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        var brush = new SolidBrush(TextColor);
        var stringFormat = new StringFormat();

        // Set alignment
        switch (TextAlign)
        {
            case ContentAlignment.TopLeft:
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Near;
                break;
            case ContentAlignment.TopCenter:
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Near;
                break;
            case ContentAlignment.TopRight:
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Near;
                break;
            case ContentAlignment.MiddleLeft:
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Center;
                break;
            case ContentAlignment.MiddleCenter:
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                break;
            case ContentAlignment.MiddleRight:
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Center;
                break;
            case ContentAlignment.BottomLeft:
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Far;
                break;
            case ContentAlignment.BottomCenter:
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Far;
                break;
            case ContentAlignment.BottomRight:
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Far;
                break;
        }

        g.DrawString(Text, Font, brush, rect, stringFormat);

        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }

        brush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new TextLabelComponent
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
            ForeColor = ForeColor,
            TextColor = TextColor,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style),
            TextAlign = TextAlign,
            AutoSize = AutoSize
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["text"] = Text;
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["textColor"] = ColorTranslator.ToHtml(TextColor);
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        json["textAlign"] = TextAlign.ToString();
        json["autoSize"] = AutoSize;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Text = json["text"]?.ToString() ?? "Label";
        
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#000000") is Color foreColor)
            ForeColor = foreColor;
        if (json["textColor"] != null && ColorTranslator.FromHtml(json["textColor"]?.ToString() ?? "") is Color textColor)
            TextColor = textColor;
        else
            TextColor = ForeColor;

        var fontName = json["font"]?.ToString() ?? "Arial";
        var fontSize = json["fontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }

        if (Enum.TryParse<ContentAlignment>(json["textAlign"]?.ToString() ?? "MiddleLeft", out var align))
        {
            TextAlign = align;
        }

        AutoSize = json["autoSize"]?.ToObject<bool>() ?? false;
    }
}
