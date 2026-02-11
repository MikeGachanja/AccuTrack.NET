using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Motor component for displaying motor status and control.
/// </summary>
public class MotorComponent : BaseComponent
{
    public override string ComponentType => "Motor";

    public MotorState State { get; set; } = MotorState.Off;
    public bool Running { get; set; } = false;
    public bool Faulted { get; set; } = false;
    public Color OnColor { get; set; } = Color.Green;
    public Color OffColor { get; set; } = Color.Gray;
    public Color FaultColor { get; set; } = Color.Red;
    public Color BorderColor { get; set; } = Color.Black;
    public string Label { get; set; } = "Motor";
    public Font LabelFont { get; set; } = new Font("Arial", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Determine motor color based on state
        Color motorColor = Faulted ? FaultColor : (Running ? OnColor : OffColor);
        
        // Draw motor circle (main body)
        int motorSize = Math.Max(10, Math.Min(rect.Width - 10, rect.Height - 30));
        int motorX = rect.X + (rect.Width - motorSize) / 2;
        int motorY = rect.Y + 5;
        var motorRect = new Rectangle(motorX, motorY, motorSize, motorSize);
        
        var motorBrush = new SolidBrush(motorColor);
        g.FillEllipse(motorBrush, motorRect);
        
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, 2);
        g.DrawEllipse(borderPen, motorRect);
        
        // Draw motor symbol (M inside circle) - clamp font size to valid range
        var textBrush = new SolidBrush(Color.White);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        float symbolEmSize = Math.Max(6f, motorSize / 3f);
        var symbolFont = new Font("Arial", symbolEmSize, FontStyle.Bold);
        g.DrawString("M", symbolFont, textBrush, motorRect, stringFormat);
        
        // Draw status indicator (small circle at top-right)
        int indicatorSize = motorSize / 4;
        var indicatorRect = new Rectangle(
            motorRect.Right - indicatorSize - 2,
            motorRect.Y + 2,
            indicatorSize,
            indicatorSize
        );
        var indicatorBrush = new SolidBrush(motorColor);
        g.FillEllipse(indicatorBrush, indicatorRect);
        g.DrawEllipse(Pens.Black, indicatorRect);
        
        // Draw label
        if (!string.IsNullOrEmpty(Label))
        {
            var labelBrush = new SolidBrush(Color.Black);
            var labelRect = new Rectangle(rect.X, motorRect.Bottom + 5, rect.Width, rect.Height - motorRect.Bottom - 5);
            g.DrawString(Label, LabelFont, labelBrush, labelRect, stringFormat);
            labelBrush.Dispose();
        }
        
        motorBrush.Dispose();
        borderPen.Dispose();
        textBrush.Dispose();
        symbolFont.Dispose();
        indicatorBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new MotorComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            State = State,
            Running = Running,
            Faulted = Faulted,
            OnColor = OnColor,
            OffColor = OffColor,
            FaultColor = FaultColor,
            BorderColor = BorderColor,
            Label = Label,
            LabelFont = new Font(LabelFont.FontFamily, LabelFont.Size, LabelFont.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["state"] = State.ToString();
        json["running"] = Running;
        json["faulted"] = Faulted;
        json["onColor"] = ColorTranslator.ToHtml(OnColor);
        json["offColor"] = ColorTranslator.ToHtml(OffColor);
        json["faultColor"] = ColorTranslator.ToHtml(FaultColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["label"] = Label;
        json["labelFont"] = LabelFont.Name;
        json["labelFontSize"] = LabelFont.Size;
        json["labelFontStyle"] = LabelFont.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        if (Enum.TryParse<MotorState>(json["state"]?.ToString() ?? "Off", out var state))
        {
            State = state;
        }
        
        Running = json["running"]?.ToObject<bool>() ?? false;
        Faulted = json["faulted"]?.ToObject<bool>() ?? false;
        
        if (ColorTranslator.FromHtml(json["onColor"]?.ToString() ?? "#008000") is Color onColor)
            OnColor = onColor;
        if (ColorTranslator.FromHtml(json["offColor"]?.ToString() ?? "#808080") is Color offColor)
            OffColor = offColor;
        if (ColorTranslator.FromHtml(json["faultColor"]?.ToString() ?? "#FF0000") is Color faultColor)
            FaultColor = faultColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#000000") is Color borderColor)
            BorderColor = borderColor;
        
        Label = json["label"]?.ToString() ?? "Motor";
        
        var fontName = json["labelFont"]?.ToString() ?? "Arial";
        var fontSize = json["labelFontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["labelFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            LabelFont = new Font(fontName, fontSize, fontStyle);
        }
    }
}

public enum MotorState
{
    Off,
    On,
    Starting,
    Stopping,
    Faulted
}
