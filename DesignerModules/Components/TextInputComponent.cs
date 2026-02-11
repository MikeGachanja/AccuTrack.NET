using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Text input component for user input on screens.
/// </summary>
public class TextInputComponent : BaseComponent
{
    public override string ComponentType => "TextInput";

    public string PlaceholderText { get; set; } = "Enter text...";
    public Color BackColor { get; set; } = Color.White;
    public Color ForeColor { get; set; } = Color.Black;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;
    public Font Font { get; set; } = new Font("Arial", 9);
    public bool IsPassword { get; set; } = false;
    public int MaxLength { get; set; } = 0; // 0 = unlimited

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        var brush = new SolidBrush(BackColor);
        var pen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);

        // Draw background
        g.FillRectangle(brush, rect);

        // Draw border
        g.DrawRectangle(pen, rect);

        // Draw placeholder or text
        var textBrush = new SolidBrush(ForeColor);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        var textRect = new Rectangle(rect.X + 5, rect.Y, rect.Width - 10, rect.Height);
        g.DrawString(PlaceholderText, Font, textBrush, textRect, stringFormat);

        brush.Dispose();
        pen.Dispose();
        textBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new TextInputComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            PlaceholderText = PlaceholderText,
            BackColor = BackColor,
            ForeColor = ForeColor,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style),
            IsPassword = IsPassword,
            MaxLength = MaxLength
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["placeholderText"] = PlaceholderText;
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        json["isPassword"] = IsPassword;
        json["maxLength"] = MaxLength;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        PlaceholderText = json["placeholderText"]?.ToString() ?? "Enter text...";
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#FFFFFF") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#000000") is Color foreColor)
            ForeColor = foreColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;

        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
        IsPassword = json["isPassword"]?.ToObject<bool>() ?? false;
        MaxLength = json["maxLength"]?.ToObject<int>() ?? 0;

        var fontName = json["font"]?.ToString() ?? "Arial";
        var fontSize = json["fontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }
    }
}
