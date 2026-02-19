using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Numeric viewer component for displaying numeric values from tags.
/// </summary>
public class NumericComponent : BaseComponent
{
    public override string ComponentType => "Numeric";

    public string Label { get; set; } = "";
    public int DecimalPlaces { get; set; } = 0;
    public string Suffix { get; set; } = "";
    public Color LabelColor { get; set; } = Color.FromArgb(51, 51, 51); // #333333
    public Color ValueColor { get; set; } = Color.FromArgb(0, 102, 204); // #0066cc
    public Font LabelFont { get; set; } = new Font("Arial", 11);
    public Font ValueFont { get; set; } = new Font("Arial", 18, FontStyle.Bold);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        var labelWidth = 0f;

        // Draw label on the left if present
        if (!string.IsNullOrEmpty(Label))
        {
            var labelBrush = new SolidBrush(LabelColor);
            var labelFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };
            labelWidth = g.MeasureString(Label, LabelFont).Width + 6; // label width + gap
            var labelRect = new Rectangle(rect.X, rect.Y, (int)labelWidth, rect.Height);
            g.DrawString(Label, LabelFont, labelBrush, labelRect, labelFormat);
            labelBrush.Dispose();
        }

        // Draw value to the right of the label (placeholder: "0" or formatted number)
        var valueBrush = new SolidBrush(ValueColor);
        var valueFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        var valueText = "0";
        if (DecimalPlaces > 0)
        {
            valueText = string.Format($"{{0:F{DecimalPlaces}}}", 0.0);
        }
        valueText += Suffix;

        var valueRect = new Rectangle(rect.X + (int)labelWidth, rect.Y, rect.Width - (int)labelWidth, rect.Height);
        g.DrawString(valueText, ValueFont, valueBrush, valueRect, valueFormat);

        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }

        valueBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new NumericComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            Label = Label,
            DecimalPlaces = DecimalPlaces,
            Suffix = Suffix,
            LabelColor = LabelColor,
            ValueColor = ValueColor,
            LabelFont = new Font(LabelFont.FontFamily, LabelFont.Size, LabelFont.Style),
            ValueFont = new Font(ValueFont.FontFamily, ValueFont.Size, ValueFont.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["label"] = Label;
        json["decimalPlaces"] = DecimalPlaces;
        json["suffix"] = Suffix;
        json["labelColor"] = ColorTranslator.ToHtml(LabelColor);
        json["valueColor"] = ColorTranslator.ToHtml(ValueColor);
        json["labelFont"] = LabelFont.Name;
        json["labelFontSize"] = LabelFont.Size;
        json["labelFontStyle"] = LabelFont.Style.ToString();
        json["valueFont"] = ValueFont.Name;
        json["valueFontSize"] = ValueFont.Size;
        json["valueFontStyle"] = ValueFont.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Label = json["label"]?.ToString() ?? "";
        DecimalPlaces = json["decimalPlaces"]?.ToObject<int>() ?? 0;
        Suffix = json["suffix"]?.ToString() ?? "";
        
        if (ColorTranslator.FromHtml(json["labelColor"]?.ToString() ?? "#333333") is Color labelColor)
            LabelColor = labelColor;
        if (ColorTranslator.FromHtml(json["valueColor"]?.ToString() ?? "#0066cc") is Color valueColor)
            ValueColor = valueColor;

        var labelFontName = json["labelFont"]?.ToString() ?? "Arial";
        var labelFontSize = json["labelFontSize"]?.ToObject<float>() ?? 11f;
        var labelFontStyleStr = json["labelFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(labelFontStyleStr, out var labelFontStyle))
        {
            LabelFont = new Font(labelFontName, labelFontSize, labelFontStyle);
        }

        var valueFontName = json["valueFont"]?.ToString() ?? "Arial";
        var valueFontSize = json["valueFontSize"]?.ToObject<float>() ?? 18f;
        var valueFontStyleStr = json["valueFontStyle"]?.ToString() ?? "Bold";
        if (Enum.TryParse<FontStyle>(valueFontStyleStr, out var valueFontStyle))
        {
            ValueFont = new Font(valueFontName, valueFontSize, valueFontStyle);
        }
    }
}
