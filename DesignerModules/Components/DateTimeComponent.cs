using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Date/time display component.
/// </summary>
public class DateTimeComponent : BaseComponent
{
    public override string ComponentType => "DateTime";

    public DateTimeFormat Format { get; set; } = DateTimeFormat.DateTime;
    public string CustomFormat { get; set; } = string.Empty;
    public Color BackColor { get; set; } = Color.White;
    public Color ForeColor { get; set; } = Color.Black;
    /// <summary>Color of the date/time text (independent of background).</summary>
    public Color TextColor { get; set; } = Color.Black;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;
    public Font Font { get; set; } = new Font("Arial", 12);
    public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
    public bool ShowLabel { get; set; } = false;
    public string Label { get; set; } = "Date/Time";

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Draw background
        var backBrush = new SolidBrush(BackColor);
        g.FillRectangle(backBrush, rect);

        // Draw border
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);
        g.DrawRectangle(borderPen, rect);

        // Get current date/time
        DateTime now = DateTime.Now;
        string dateTimeText = Format switch
        {
            DateTimeFormat.Date => now.ToString("yyyy-MM-dd"),
            DateTimeFormat.Time => now.ToString("HH:mm:ss"),
            DateTimeFormat.DateTime => now.ToString("yyyy-MM-dd HH:mm:ss"),
            DateTimeFormat.Custom => string.IsNullOrEmpty(CustomFormat) ? now.ToString() : now.ToString(CustomFormat),
            _ => now.ToString()
        };

        // Draw label if enabled
        Rectangle textRect = rect;
        if (ShowLabel && !string.IsNullOrEmpty(Label))
        {
            var labelFont = new Font(Font.FontFamily, Font.Size * 0.7f, Font.Style);
            var labelBrush = new SolidBrush(TextColor);
            var labelRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 3);
            var labelFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(Label, labelFont, labelBrush, labelRect, labelFormat);
            textRect = new Rectangle(rect.X, rect.Y + rect.Height / 3, rect.Width, rect.Height * 2 / 3);
            labelFont.Dispose();
            labelBrush.Dispose();
        }

        // Draw date/time text (use TextColor)
        var textBrush = new SolidBrush(TextColor);
        var stringFormat = new StringFormat();

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

        g.DrawString(dateTimeText, Font, textBrush, textRect, stringFormat);

        backBrush.Dispose();
        borderPen.Dispose();
        textBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new DateTimeComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            Format = Format,
            CustomFormat = CustomFormat,
            BackColor = BackColor,
            ForeColor = ForeColor,
            TextColor = TextColor,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style),
            TextAlign = TextAlign,
            ShowLabel = ShowLabel,
            Label = Label
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["format"] = Format.ToString();
        json["customFormat"] = CustomFormat;
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["textColor"] = ColorTranslator.ToHtml(TextColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        json["textAlign"] = TextAlign.ToString();
        json["showLabel"] = ShowLabel;
        json["label"] = Label;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        if (Enum.TryParse<DateTimeFormat>(json["format"]?.ToString() ?? "DateTime", out var format))
        {
            Format = format;
        }
        
        CustomFormat = json["customFormat"]?.ToString() ?? string.Empty;
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#FFFFFF") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#000000") is Color foreColor)
            ForeColor = foreColor;
        if (json["textColor"] != null && ColorTranslator.FromHtml(json["textColor"]?.ToString() ?? "") is Color textColor)
            TextColor = textColor;
        else
            TextColor = ForeColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;

        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;

        var fontName = json["font"]?.ToString() ?? "Arial";
        var fontSize = json["fontSize"]?.ToObject<float>() ?? 12f;
        var fontStyleStr = json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }

        if (Enum.TryParse<ContentAlignment>(json["textAlign"]?.ToString() ?? "MiddleCenter", out var align))
        {
            TextAlign = align;
        }

        ShowLabel = json["showLabel"]?.ToObject<bool>() ?? false;
        Label = json["label"]?.ToString() ?? "Date/Time";
    }
}

public enum DateTimeFormat
{
    Date,
    Time,
    DateTime,
    Custom
}
