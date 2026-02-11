using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Pump component for displaying pump status and control.
/// </summary>
public class PumpComponent : BaseComponent
{
    public override string ComponentType => "Pump";

    public PumpState State { get; set; } = PumpState.Off;
    public bool Running { get; set; } = false;
    public bool Faulted { get; set; } = false;
    public Color OnColor { get; set; } = Color.Blue;
    public Color OffColor { get; set; } = Color.Gray;
    public Color FaultColor { get; set; } = Color.Red;
    public Color BorderColor { get; set; } = Color.Black;
    public string Label { get; set; } = "Pump";
    public Font LabelFont { get; set; } = new Font("Arial", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Determine pump color based on state
        Color pumpColor = Faulted ? FaultColor : (Running ? OnColor : OffColor);
        
        // Draw pump body (circle) - clamp size so font and layout stay valid
        int pumpSize = Math.Max(10, Math.Min(rect.Width - 10, rect.Height - 30));
        int pumpX = rect.X + (rect.Width - pumpSize) / 2;
        int pumpY = rect.Y + 5;
        var pumpRect = new Rectangle(pumpX, pumpY, pumpSize, pumpSize);
        
        var pumpBrush = new SolidBrush(pumpColor);
        g.FillEllipse(pumpBrush, pumpRect);
        
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, 2);
        g.DrawEllipse(borderPen, pumpRect);
        
        // Draw pump symbol (P inside circle) - clamp font size to valid range
        var textBrush = new SolidBrush(Color.White);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        float symbolEmSize = Math.Max(6f, pumpSize / 3f);
        var symbolFont = new Font("Arial", symbolEmSize, FontStyle.Bold);
        g.DrawString("P", symbolFont, textBrush, pumpRect, stringFormat);
        
        // Draw inlet/outlet pipes
        int pipeWidth = pumpSize / 6;
        int pipeHeight = pumpSize / 3;
        
        // Inlet (left)
        var inletRect = new Rectangle(pumpRect.X - pipeWidth, pumpRect.Y + (pumpSize - pipeHeight) / 2, pipeWidth, pipeHeight);
        g.FillRectangle(pumpBrush, inletRect);
        g.DrawRectangle(borderPen, inletRect);
        
        // Outlet (right)
        var outletRect = new Rectangle(pumpRect.Right, pumpRect.Y + (pumpSize - pipeHeight) / 2, pipeWidth, pipeHeight);
        g.FillRectangle(pumpBrush, outletRect);
        g.DrawRectangle(borderPen, outletRect);
        
        // Draw label
        if (!string.IsNullOrEmpty(Label))
        {
            var labelBrush = new SolidBrush(Color.Black);
            var labelRect = new Rectangle(rect.X, pumpRect.Bottom + 5, rect.Width, rect.Height - pumpRect.Bottom - 5);
            g.DrawString(Label, LabelFont, labelBrush, labelRect, stringFormat);
            labelBrush.Dispose();
        }
        
        pumpBrush.Dispose();
        borderPen.Dispose();
        textBrush.Dispose();
        symbolFont.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new PumpComponent
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
        
        if (Enum.TryParse<PumpState>(json["state"]?.ToString() ?? "Off", out var state))
        {
            State = state;
        }
        
        Running = json["running"]?.ToObject<bool>() ?? false;
        Faulted = json["faulted"]?.ToObject<bool>() ?? false;
        
        if (ColorTranslator.FromHtml(json["onColor"]?.ToString() ?? "#0000FF") is Color onColor)
            OnColor = onColor;
        if (ColorTranslator.FromHtml(json["offColor"]?.ToString() ?? "#808080") is Color offColor)
            OffColor = offColor;
        if (ColorTranslator.FromHtml(json["faultColor"]?.ToString() ?? "#FF0000") is Color faultColor)
            FaultColor = faultColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#000000") is Color borderColor)
            BorderColor = borderColor;
        
        Label = json["label"]?.ToString() ?? "Pump";
        
        var fontName = json["labelFont"]?.ToString() ?? "Arial";
        var fontSize = json["labelFontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["labelFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            LabelFont = new Font(fontName, fontSize, fontStyle);
        }
    }
}

public enum PumpState
{
    Off,
    On,
    Starting,
    Stopping,
    Faulted
}
