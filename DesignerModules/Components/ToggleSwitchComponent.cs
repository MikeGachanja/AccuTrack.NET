using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Toggle switch component for boolean input.
/// </summary>
public class ToggleSwitchComponent : BaseComponent
{
    public override string ComponentType => "ToggleSwitch";

    public bool IsOn { get; set; } = false;
    public Color OnColor { get; set; } = Color.Green;
    public Color OffColor { get; set; } = Color.Gray;
    public Color ThumbColor { get; set; } = Color.White;
    public string OnLabel { get; set; } = "ON";
    public string OffLabel { get; set; } = "OFF";
    public bool ShowLabels { get; set; } = true;
    public Font LabelFont { get; set; } = new Font("Arial", 8);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Calculate switch dimensions
        int switchHeight = Math.Min(rect.Height - 10, 30);
        int switchWidth = switchHeight * 2; // Switch is typically 2:1 ratio
        if (switchWidth > rect.Width - 10)
        {
            switchWidth = rect.Width - 10;
            switchHeight = switchWidth / 2;
        }
        
        int switchX = rect.X + (rect.Width - switchWidth) / 2;
        int switchY = rect.Y + (rect.Height - switchHeight) / 2;
        
        var switchRect = new Rectangle(switchX, switchY, switchWidth, switchHeight);
        int cornerRadius = switchHeight / 2;
        
        // Draw switch track
        var trackColor = IsOn ? OnColor : OffColor;
        var trackBrush = new SolidBrush(trackColor);
        DrawRoundedRectangle(g, trackBrush, switchRect, cornerRadius);
        
        // Draw thumb
        int thumbSize = switchHeight - 4;
        int thumbX = IsOn ? switchRect.Right - thumbSize - 2 : switchRect.X + 2;
        int thumbY = switchRect.Y + 2;
        var thumbRect = new Rectangle(thumbX, thumbY, thumbSize, thumbSize);
        
        var thumbBrush = new SolidBrush(ThumbColor);
        g.FillEllipse(thumbBrush, thumbRect);
        g.DrawEllipse(new Pen(Color.Black, 1), thumbRect);
        
        // Draw labels
        if (ShowLabels)
        {
            var labelBrush = new SolidBrush(Color.Black);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            // Draw ON label
            var onRect = new Rectangle(switchRect.X, switchRect.Y - 15, switchRect.Width / 2, 12);
            g.DrawString(OnLabel, LabelFont, labelBrush, onRect, stringFormat);
            
            // Draw OFF label
            var offRect = new Rectangle(switchRect.X + switchRect.Width / 2, switchRect.Y - 15, switchRect.Width / 2, 12);
            g.DrawString(OffLabel, LabelFont, labelBrush, offRect, stringFormat);
            
            labelBrush.Dispose();
        }
        
        // Draw selection border
        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }
        
        trackBrush.Dispose();
        thumbBrush.Dispose();
    }

    private void DrawRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        
        g.FillPath(brush, path);
        g.DrawPath(new Pen(Color.Black, 1), path);
        path.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new ToggleSwitchComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            IsOn = IsOn,
            OnColor = OnColor,
            OffColor = OffColor,
            ThumbColor = ThumbColor,
            OnLabel = OnLabel,
            OffLabel = OffLabel,
            ShowLabels = ShowLabels,
            LabelFont = new Font(LabelFont.FontFamily, LabelFont.Size, LabelFont.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["isOn"] = IsOn;
        json["onColor"] = ColorTranslator.ToHtml(OnColor);
        json["offColor"] = ColorTranslator.ToHtml(OffColor);
        json["thumbColor"] = ColorTranslator.ToHtml(ThumbColor);
        json["onLabel"] = OnLabel;
        json["offLabel"] = OffLabel;
        json["showLabels"] = ShowLabels;
        json["labelFont"] = LabelFont.Name;
        json["labelFontSize"] = LabelFont.Size;
        json["labelFontStyle"] = LabelFont.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        IsOn = json["isOn"]?.ToObject<bool>() ?? false;
        
        if (ColorTranslator.FromHtml(json["onColor"]?.ToString() ?? "#008000") is Color onColor)
            OnColor = onColor;
        if (ColorTranslator.FromHtml(json["offColor"]?.ToString() ?? "#808080") is Color offColor)
            OffColor = offColor;
        if (ColorTranslator.FromHtml(json["thumbColor"]?.ToString() ?? "#FFFFFF") is Color thumbColor)
            ThumbColor = thumbColor;
        
        OnLabel = json["onLabel"]?.ToString() ?? "ON";
        OffLabel = json["offLabel"]?.ToString() ?? "OFF";
        ShowLabels = json["showLabels"]?.ToObject<bool>() ?? true;
        
        var fontName = json["labelFont"]?.ToString() ?? "Arial";
        var fontSize = json["labelFontSize"]?.ToObject<float>() ?? 8f;
        var fontStyleStr = json["labelFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            LabelFont = new Font(fontName, fontSize, fontStyle);
        }
    }
}
