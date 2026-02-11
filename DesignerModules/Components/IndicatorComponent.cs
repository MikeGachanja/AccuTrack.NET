using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Indicator component for displaying status with color coding.
/// </summary>
public class IndicatorComponent : BaseComponent
{
    public override string ComponentType => "Indicator";

    public IndicatorState State { get; set; } = IndicatorState.Off;
    public string Label { get; set; } = "Status";
    public Color OffColor { get; set; } = Color.Gray;
    public Color OnColor { get; set; } = Color.Green;
    public Color WarningColor { get; set; } = Color.Yellow;
    public Color ErrorColor { get; set; } = Color.Red;
    public IndicatorShape Shape { get; set; } = IndicatorShape.Circle;
    public Font Font { get; set; } = new Font("Arial", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Determine color based on state
        Color indicatorColor = State switch
        {
            IndicatorState.On => OnColor,
            IndicatorState.Warning => WarningColor,
            IndicatorState.Error => ErrorColor,
            _ => OffColor
        };

        // Draw indicator shape
        var brush = new SolidBrush(indicatorColor);
        int indicatorSize = Math.Min(rect.Width - 10, rect.Height - 30);
        int indicatorX = rect.X + (rect.Width - indicatorSize) / 2;
        int indicatorY = rect.Y + 5;

        var indicatorRect = new Rectangle(indicatorX, indicatorY, indicatorSize, indicatorSize);

        if (Shape == IndicatorShape.Circle)
        {
            g.FillEllipse(brush, indicatorRect);
            g.DrawEllipse(new Pen(Color.Black, 1), indicatorRect);
        }
        else // Square
        {
            g.FillRectangle(brush, indicatorRect);
            g.DrawRectangle(new Pen(Color.Black, 1), indicatorRect);
        }

        // Draw label
        if (!string.IsNullOrEmpty(Label))
        {
            var textBrush = new SolidBrush(Color.Black);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            var labelRect = new Rectangle(rect.X, indicatorRect.Bottom + 5, rect.Width, rect.Height - indicatorRect.Bottom - 5);
            g.DrawString(Label, Font, textBrush, labelRect, stringFormat);
            textBrush.Dispose();
        }

        // Draw selection border
        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }

        brush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new IndicatorComponent
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
            Label = Label,
            OffColor = OffColor,
            OnColor = OnColor,
            WarningColor = WarningColor,
            ErrorColor = ErrorColor,
            Shape = Shape,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["state"] = State.ToString();
        json["label"] = Label;
        json["offColor"] = ColorTranslator.ToHtml(OffColor);
        json["onColor"] = ColorTranslator.ToHtml(OnColor);
        json["warningColor"] = ColorTranslator.ToHtml(WarningColor);
        json["errorColor"] = ColorTranslator.ToHtml(ErrorColor);
        json["shape"] = Shape.ToString();
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        if (Enum.TryParse<IndicatorState>(json["state"]?.ToString() ?? "Off", out var state))
        {
            State = state;
        }
        
        Label = json["label"]?.ToString() ?? "Status";
        
        if (ColorTranslator.FromHtml(json["offColor"]?.ToString() ?? "#808080") is Color offColor)
            OffColor = offColor;
        if (ColorTranslator.FromHtml(json["onColor"]?.ToString() ?? "#008000") is Color onColor)
            OnColor = onColor;
        if (ColorTranslator.FromHtml(json["warningColor"]?.ToString() ?? "#FFFF00") is Color warningColor)
            WarningColor = warningColor;
        if (ColorTranslator.FromHtml(json["errorColor"]?.ToString() ?? "#FF0000") is Color errorColor)
            ErrorColor = errorColor;

        if (Enum.TryParse<IndicatorShape>(json["shape"]?.ToString() ?? "Circle", out var shape))
        {
            Shape = shape;
        }

        var fontName = json["font"]?.ToString() ?? "Arial";
        var fontSize = json["fontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }
    }
}

public enum IndicatorState
{
    Off,
    On,
    Warning,
    Error
}

public enum IndicatorShape
{
    Circle,
    Square
}
