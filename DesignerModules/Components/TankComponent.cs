using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Tank component for displaying tank level and status.
/// </summary>
public class TankComponent : BaseComponent
{
    public override string ComponentType => "Tank";

    public double Level { get; set; } = 50.0; // Percentage 0-100
    public double Minimum { get; set; } = 0.0;
    public double Maximum { get; set; } = 100.0;
    public Color TankColor { get; set; } = Color.LightBlue;
    public Color FillColor { get; set; } = Color.Blue;
    public Color BorderColor { get; set; } = Color.Black;
    public Color LowLevelColor { get; set; } = Color.Red;
    public Color HighLevelColor { get; set; } = Color.Green;
    public double LowLevelThreshold { get; set; } = 20.0;
    public double HighLevelThreshold { get; set; } = 80.0;
    public string Label { get; set; } = "Tank";
    public bool ShowLevel { get; set; } = true;
    public Font LabelFont { get; set; } = new Font("Arial", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Calculate fill level
        double range = Maximum - Minimum;
        double normalizedLevel = range > 0 ? (Level - Minimum) / range : 0;
        normalizedLevel = Math.Max(0, Math.Min(1, normalizedLevel));
        
        // Determine fill color based on level
        Color currentFillColor = FillColor;
        if (Level < LowLevelThreshold)
            currentFillColor = LowLevelColor;
        else if (Level > HighLevelThreshold)
            currentFillColor = HighLevelColor;
        
        // Draw tank outline (rounded rectangle)
        int cornerRadius = 5;
        var tankPath = new GraphicsPath();
        tankPath.AddArc(rect.X, rect.Y, cornerRadius * 2, cornerRadius * 2, 180, 90);
        tankPath.AddArc(rect.Right - cornerRadius * 2, rect.Y, cornerRadius * 2, cornerRadius * 2, 270, 90);
        tankPath.AddArc(rect.Right - cornerRadius * 2, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
        tankPath.AddArc(rect.X, rect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
        tankPath.CloseFigure();
        
        // Draw tank background
        var tankBrush = new SolidBrush(TankColor);
        g.FillPath(tankBrush, tankPath);
        
        // Draw fill level
        int fillHeight = (int)(rect.Height * normalizedLevel);
        if (fillHeight > 0)
        {
            var fillRect = new Rectangle(
                rect.X + 2,
                rect.Bottom - fillHeight - 2,
                rect.Width - 4,
                fillHeight
            );
            
            // Create fill path with rounded bottom
            var fillPath = new GraphicsPath();
            fillPath.AddArc(fillRect.X, fillRect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 90, 90);
            fillPath.AddLine(fillRect.X + cornerRadius, fillRect.Bottom, fillRect.Right - cornerRadius, fillRect.Bottom);
            fillPath.AddArc(fillRect.Right - cornerRadius * 2, fillRect.Bottom - cornerRadius * 2, cornerRadius * 2, cornerRadius * 2, 0, 90);
            fillPath.AddLine(fillRect.Right, fillRect.Bottom - cornerRadius, fillRect.Right, fillRect.Y);
            fillPath.AddLine(fillRect.X + cornerRadius, fillRect.Y, fillRect.Right - cornerRadius, fillRect.Y);
            fillPath.CloseFigure();
            
            var fillBrush = new SolidBrush(currentFillColor);
            g.FillPath(fillBrush, fillPath);
            fillBrush.Dispose();
            fillPath.Dispose();
        }
        
        // Draw tank border
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, 2);
        g.DrawPath(borderPen, tankPath);
        
        // Draw level text
        if (ShowLevel)
        {
            var textBrush = new SolidBrush(Color.Black);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            string levelText = $"{Level:F1}%";
            g.DrawString(levelText, LabelFont, textBrush, rect, stringFormat);
            textBrush.Dispose();
        }
        
        // Draw label
        if (!string.IsNullOrEmpty(Label))
        {
            var labelBrush = new SolidBrush(Color.Black);
            var labelRect = new Rectangle(rect.X, rect.Bottom - 20, rect.Width, 15);
            var labelFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(Label, LabelFont, labelBrush, labelRect, labelFormat);
            labelBrush.Dispose();
        }
        
        tankBrush.Dispose();
        borderPen.Dispose();
        tankPath.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new TankComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            Level = Level,
            Minimum = Minimum,
            Maximum = Maximum,
            TankColor = TankColor,
            FillColor = FillColor,
            BorderColor = BorderColor,
            LowLevelColor = LowLevelColor,
            HighLevelColor = HighLevelColor,
            LowLevelThreshold = LowLevelThreshold,
            HighLevelThreshold = HighLevelThreshold,
            Label = Label,
            ShowLevel = ShowLevel,
            LabelFont = new Font(LabelFont.FontFamily, LabelFont.Size, LabelFont.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["level"] = Level;
        json["minimum"] = Minimum;
        json["maximum"] = Maximum;
        json["tankColor"] = ColorTranslator.ToHtml(TankColor);
        json["fillColor"] = ColorTranslator.ToHtml(FillColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["lowLevelColor"] = ColorTranslator.ToHtml(LowLevelColor);
        json["highLevelColor"] = ColorTranslator.ToHtml(HighLevelColor);
        json["lowLevelThreshold"] = LowLevelThreshold;
        json["highLevelThreshold"] = HighLevelThreshold;
        json["label"] = Label;
        json["showLevel"] = ShowLevel;
        json["labelFont"] = LabelFont.Name;
        json["labelFontSize"] = LabelFont.Size;
        json["labelFontStyle"] = LabelFont.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        Level = json["level"]?.ToObject<double>() ?? 50.0;
        Minimum = json["minimum"]?.ToObject<double>() ?? 0.0;
        Maximum = json["maximum"]?.ToObject<double>() ?? 100.0;
        
        if (ColorTranslator.FromHtml(json["tankColor"]?.ToString() ?? "#ADD8E6") is Color tankColor)
            TankColor = tankColor;
        if (ColorTranslator.FromHtml(json["fillColor"]?.ToString() ?? "#0000FF") is Color fillColor)
            FillColor = fillColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#000000") is Color borderColor)
            BorderColor = borderColor;
        if (ColorTranslator.FromHtml(json["lowLevelColor"]?.ToString() ?? "#FF0000") is Color lowLevelColor)
            LowLevelColor = lowLevelColor;
        if (ColorTranslator.FromHtml(json["highLevelColor"]?.ToString() ?? "#008000") is Color highLevelColor)
            HighLevelColor = highLevelColor;
        
        LowLevelThreshold = json["lowLevelThreshold"]?.ToObject<double>() ?? 20.0;
        HighLevelThreshold = json["highLevelThreshold"]?.ToObject<double>() ?? 80.0;
        Label = json["label"]?.ToString() ?? "Tank";
        ShowLevel = json["showLevel"]?.ToObject<bool>() ?? true;
        
        var fontName = json["labelFont"]?.ToString() ?? "Arial";
        var fontSize = json["labelFontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["labelFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            LabelFont = new Font(fontName, fontSize, fontStyle);
        }
    }
}
