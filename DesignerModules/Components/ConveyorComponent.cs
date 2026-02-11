using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Conveyor component for displaying conveyor belt status.
/// </summary>
public class ConveyorComponent : BaseComponent
{
    public override string ComponentType => "Conveyor";

    public ConveyorState State { get; set; } = ConveyorState.Off;
    public bool Running { get; set; } = false;
    public bool Faulted { get; set; } = false;
    public ConveyorDirection Direction { get; set; } = ConveyorDirection.LeftToRight;
    public Color OnColor { get; set; } = Color.Green;
    public Color OffColor { get; set; } = Color.Gray;
    public Color FaultColor { get; set; } = Color.Red;
    public Color BorderColor { get; set; } = Color.Black;
    public string Label { get; set; } = "Conveyor";
    public Font LabelFont { get; set; } = new Font("Arial", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        // Determine conveyor color based on state
        Color conveyorColor = Faulted ? FaultColor : (Running ? OnColor : OffColor);
        
        // Draw conveyor belt (horizontal rectangle)
        int beltHeight = Math.Min(rect.Height - 20, 30);
        int beltY = rect.Y + (rect.Height - beltHeight) / 2;
        var beltRect = new Rectangle(rect.X + 5, beltY, rect.Width - 10, beltHeight);
        
        var beltBrush = new SolidBrush(conveyorColor);
        g.FillRectangle(beltBrush, beltRect);
        
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, 2);
        g.DrawRectangle(borderPen, beltRect);
        
        // Draw belt segments/pattern
        int segmentWidth = 10;
        var segmentPen = new Pen(Color.FromArgb(100, Color.Black), 1);
        for (int x = beltRect.X; x < beltRect.Right; x += segmentWidth * 2)
        {
            g.DrawLine(segmentPen, x, beltRect.Y, x, beltRect.Bottom);
        }
        
        // Draw direction arrow
        if (Running)
        {
            var arrowBrush = new SolidBrush(Color.White);
            int arrowSize = beltHeight / 2;
            int arrowX = Direction == ConveyorDirection.LeftToRight ? beltRect.Right - arrowSize - 5 : beltRect.X + 5;
            int arrowY = beltRect.Y + (beltHeight - arrowSize) / 2;
            
            Point[] arrowPoints = Direction == ConveyorDirection.LeftToRight
                ? new[]
                {
                    new Point(arrowX, arrowY + arrowSize / 2),
                    new Point(arrowX + arrowSize, arrowY),
                    new Point(arrowX + arrowSize, arrowY + arrowSize / 3),
                    new Point(arrowX + arrowSize * 2, arrowY + arrowSize / 3),
                    new Point(arrowX + arrowSize * 2, arrowY + arrowSize * 2 / 3),
                    new Point(arrowX + arrowSize, arrowY + arrowSize * 2 / 3),
                    new Point(arrowX + arrowSize, arrowY + arrowSize)
                }
                : new[]
                {
                    new Point(arrowX + arrowSize, arrowY + arrowSize / 2),
                    new Point(arrowX, arrowY),
                    new Point(arrowX, arrowY + arrowSize / 3),
                    new Point(arrowX - arrowSize, arrowY + arrowSize / 3),
                    new Point(arrowX - arrowSize, arrowY + arrowSize * 2 / 3),
                    new Point(arrowX, arrowY + arrowSize * 2 / 3),
                    new Point(arrowX, arrowY + arrowSize)
                };
            
            g.FillPolygon(arrowBrush, arrowPoints);
            arrowBrush.Dispose();
        }
        
        // Draw label
        if (!string.IsNullOrEmpty(Label))
        {
            var labelBrush = new SolidBrush(Color.Black);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            var labelRect = new Rectangle(rect.X, rect.Bottom - 15, rect.Width, 15);
            g.DrawString(Label, LabelFont, labelBrush, labelRect, stringFormat);
            labelBrush.Dispose();
        }
        
        beltBrush.Dispose();
        borderPen.Dispose();
        segmentPen.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new ConveyorComponent
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
            Direction = Direction,
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
        json["direction"] = Direction.ToString();
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
        
        if (Enum.TryParse<ConveyorState>(json["state"]?.ToString() ?? "Off", out var state))
        {
            State = state;
        }
        
        Running = json["running"]?.ToObject<bool>() ?? false;
        Faulted = json["faulted"]?.ToObject<bool>() ?? false;
        
        if (Enum.TryParse<ConveyorDirection>(json["direction"]?.ToString() ?? "LeftToRight", out var direction))
        {
            Direction = direction;
        }
        
        if (ColorTranslator.FromHtml(json["onColor"]?.ToString() ?? "#008000") is Color onColor)
            OnColor = onColor;
        if (ColorTranslator.FromHtml(json["offColor"]?.ToString() ?? "#808080") is Color offColor)
            OffColor = offColor;
        if (ColorTranslator.FromHtml(json["faultColor"]?.ToString() ?? "#FF0000") is Color faultColor)
            FaultColor = faultColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#000000") is Color borderColor)
            BorderColor = borderColor;
        
        Label = json["label"]?.ToString() ?? "Conveyor";
        
        var fontName = json["labelFont"]?.ToString() ?? "Arial";
        var fontSize = json["labelFontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["labelFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            LabelFont = new Font(fontName, fontSize, fontStyle);
        }
    }
}

public enum ConveyorState
{
    Off,
    On,
    Starting,
    Stopping,
    Faulted
}

public enum ConveyorDirection
{
    LeftToRight,
    RightToLeft
}
