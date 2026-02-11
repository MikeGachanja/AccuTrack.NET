using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Progress bar component for displaying progress values.
/// </summary>
public class ProgressBarComponent : BaseComponent
{
    public override string ComponentType => "ProgressBar";

    public double Value { get; set; } = 0.0;
    public double Minimum { get; set; } = 0.0;
    public double Maximum { get; set; } = 100.0;
    public Color BackColor { get; set; } = Color.LightGray;
    public Color ForeColor { get; set; } = Color.Green;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;
    public ProgressBarStyle Style { get; set; } = ProgressBarStyle.Continuous;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        var pen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);

        // Draw border
        g.DrawRectangle(pen, rect);

        // Calculate progress
        double range = Maximum - Minimum;
        double progress = range > 0 ? (Value - Minimum) / range : 0;
        progress = Math.Max(0, Math.Min(1, progress)); // Clamp between 0 and 1

        // Draw background
        var backBrush = new SolidBrush(BackColor);
        g.FillRectangle(backBrush, rect);

        // Draw progress
        if (progress > 0)
        {
            var progressRect = new Rectangle(
                rect.X + BorderWidth,
                rect.Y + BorderWidth,
                (int)((rect.Width - BorderWidth * 2) * progress),
                rect.Height - BorderWidth * 2
            );

            var foreBrush = new SolidBrush(ForeColor);
            g.FillRectangle(foreBrush, progressRect);
            foreBrush.Dispose();
        }

        // Draw value text
        var textBrush = new SolidBrush(Color.Black);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString($"{Value:F1}%", SystemFonts.DefaultFont, textBrush, rect, stringFormat);

        pen.Dispose();
        backBrush.Dispose();
        textBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new ProgressBarComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            Value = Value,
            Minimum = Minimum,
            Maximum = Maximum,
            BackColor = BackColor,
            ForeColor = ForeColor,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            Style = Style
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["value"] = Value;
        json["minimum"] = Minimum;
        json["maximum"] = Maximum;
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        json["style"] = Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Value = json["value"]?.ToObject<double>() ?? 0.0;
        Minimum = json["minimum"]?.ToObject<double>() ?? 0.0;
        Maximum = json["maximum"]?.ToObject<double>() ?? 100.0;
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#D3D3D3") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#008000") is Color foreColor)
            ForeColor = foreColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;

        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
        
        if (Enum.TryParse<ProgressBarStyle>(json["style"]?.ToString() ?? "Continuous", out var style))
        {
            Style = style;
        }
    }
}

public enum ProgressBarStyle
{
    Continuous,
    Marquee,
    Blocks
}
