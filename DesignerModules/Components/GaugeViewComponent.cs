using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Gauge component for displaying numeric values in a circular gauge format.
/// </summary>
public class GaugeViewComponent : BaseComponent
{
    public override string ComponentType => "GaugeView";

    public double Value { get; set; } = 50.0;
    public double Minimum { get; set; } = 0.0;
    public double Maximum { get; set; } = 100.0;
    public Color BackColor { get; set; } = Color.White;
    public Color ForeColor { get; set; } = Color.Green;
    public Color NeedleColor { get; set; } = Color.Red;
    public Color TextColor { get; set; } = Color.Black;
    public bool ShowValue { get; set; } = true;
    public bool ShowMinMax { get; set; } = true;
    public string Unit { get; set; } = string.Empty;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Draw background
        var backBrush = new SolidBrush(BackColor);
        g.FillEllipse(backBrush, rect);
        
        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawEllipse(pen, rect);
            pen.Dispose();
        }
        else
        {
            var pen = new Pen(Color.Gray, 1);
            g.DrawEllipse(pen, rect);
            pen.Dispose();
        }

        // Calculate angle for needle
        double range = Maximum - Minimum;
        double normalizedValue = range > 0 ? (Value - Minimum) / range : 0;
        normalizedValue = Math.Max(0, Math.Min(1, normalizedValue));
        
        // Gauge spans 270 degrees (from -135 to +135 degrees)
        double angle = -135 + (normalizedValue * 270);
        double angleRad = angle * Math.PI / 180.0;

        // Draw gauge arc
        var forePen = new Pen(ForeColor, 8);
        forePen.StartCap = LineCap.Round;
        forePen.EndCap = LineCap.Round;
        
        int margin = 10;
        var arcRect = new Rectangle(
            rect.X + margin,
            rect.Y + margin,
            rect.Width - margin * 2,
            rect.Height - margin * 2
        );
        
        g.DrawArc(forePen, arcRect, -135, (float)(normalizedValue * 270));

        // Draw needle
        int centerX = rect.X + rect.Width / 2;
        int centerY = rect.Y + rect.Height / 2;
        int radius = Math.Min(rect.Width, rect.Height) / 2 - margin;
        
        int needleLength = (int)(radius * 0.7);
        int needleEndX = centerX + (int)(needleLength * Math.Cos(angleRad));
        int needleEndY = centerY + (int)(needleLength * Math.Sin(angleRad));
        
        var needlePen = new Pen(NeedleColor, 3);
        g.DrawLine(needlePen, centerX, centerY, needleEndX, needleEndY);
        
        // Draw center dot
        var centerBrush = new SolidBrush(NeedleColor);
        g.FillEllipse(centerBrush, centerX - 5, centerY - 5, 10, 10);

        // Draw value text
        if (ShowValue)
        {
            var textBrush = new SolidBrush(TextColor);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            string valueText = $"{Value:F1}";
            if (!string.IsNullOrEmpty(Unit))
            {
                valueText += $" {Unit}";
            }
            var textRect = new Rectangle(rect.X, rect.Y + rect.Height / 2 + 10, rect.Width, 30);
            g.DrawString(valueText, SystemFonts.DefaultFont, textBrush, textRect, stringFormat);
            textBrush.Dispose();
        }

        // Draw min/max labels
        if (ShowMinMax)
        {
            var labelBrush = new SolidBrush(TextColor);
            var smallFont = new Font(SystemFonts.DefaultFont.FontFamily, 8);
            g.DrawString(Minimum.ToString("F0"), smallFont, labelBrush, rect.X + 5, rect.Bottom - 20);
            g.DrawString(Maximum.ToString("F0"), smallFont, labelBrush, rect.Right - 30, rect.Bottom - 20);
            smallFont.Dispose();
            labelBrush.Dispose();
        }

        backBrush.Dispose();
        forePen.Dispose();
        needlePen.Dispose();
        centerBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new GaugeViewComponent
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
            NeedleColor = NeedleColor,
            TextColor = TextColor,
            ShowValue = ShowValue,
            ShowMinMax = ShowMinMax,
            Unit = Unit
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
        json["needleColor"] = ColorTranslator.ToHtml(NeedleColor);
        json["textColor"] = ColorTranslator.ToHtml(TextColor);
        json["showValue"] = ShowValue;
        json["showMinMax"] = ShowMinMax;
        json["unit"] = Unit;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Value = json["value"]?.ToObject<double>() ?? 50.0;
        Minimum = json["minimum"]?.ToObject<double>() ?? 0.0;
        Maximum = json["maximum"]?.ToObject<double>() ?? 100.0;
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#FFFFFF") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#008000") is Color foreColor)
            ForeColor = foreColor;
        if (ColorTranslator.FromHtml(json["needleColor"]?.ToString() ?? "#FF0000") is Color needleColor)
            NeedleColor = needleColor;
        if (ColorTranslator.FromHtml(json["textColor"]?.ToString() ?? "#000000") is Color textColor)
            TextColor = textColor;

        ShowValue = json["showValue"]?.ToObject<bool>() ?? true;
        ShowMinMax = json["showMinMax"]?.ToObject<bool>() ?? true;
        Unit = json["unit"]?.ToString() ?? string.Empty;
    }
}
