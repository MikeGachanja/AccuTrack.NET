using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Slider component for numeric input.
/// </summary>
public class SliderComponent : BaseComponent
{
    public override string ComponentType => "Slider";

    public double Value { get; set; } = 50.0;
    public double Minimum { get; set; } = 0.0;
    public double Maximum { get; set; } = 100.0;
    public Color TrackColor { get; set; } = Color.LightGray;
    public Color FillColor { get; set; } = Color.Blue;
    public Color ThumbColor { get; set; } = Color.DarkBlue;
    public Orientation Orientation { get; set; } = Orientation.Horizontal;
    public bool ShowValue { get; set; } = true;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Calculate progress
        double range = Maximum - Minimum;
        double progress = range > 0 ? (Value - Minimum) / range : 0;
        progress = Math.Max(0, Math.Min(1, progress));

        if (Orientation == Orientation.Horizontal)
        {
            // Draw track
            int trackHeight = Math.Min(10, rect.Height / 3);
            var trackRect = new Rectangle(
                rect.X,
                rect.Y + (rect.Height - trackHeight) / 2,
                rect.Width,
                trackHeight
            );
            var trackBrush = new SolidBrush(TrackColor);
            g.FillRectangle(trackBrush, trackRect);

            // Draw filled portion
            var fillRect = new Rectangle(
                trackRect.X,
                trackRect.Y,
                (int)(trackRect.Width * progress),
                trackRect.Height
            );
            var fillBrush = new SolidBrush(FillColor);
            g.FillRectangle(fillBrush, fillRect);

            // Draw thumb
            int thumbSize = Math.Min(20, rect.Height);
            var thumbRect = new Rectangle(
                trackRect.X + (int)(trackRect.Width * progress) - thumbSize / 2,
                rect.Y + (rect.Height - thumbSize) / 2,
                thumbSize,
                thumbSize
            );
            var thumbBrush = new SolidBrush(ThumbColor);
            g.FillEllipse(thumbBrush, thumbRect);
            g.DrawEllipse(new Pen(isSelected ? Color.Blue : Color.Black, 1), thumbRect);

            trackBrush.Dispose();
            fillBrush.Dispose();
            thumbBrush.Dispose();

            // Draw value
            if (ShowValue)
            {
                var textBrush = new SolidBrush(Color.Black);
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Far
                };
                var textRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height - thumbSize);
                g.DrawString($"{Value:F1}", SystemFonts.DefaultFont, textBrush, textRect, stringFormat);
                textBrush.Dispose();
            }
        }
        else // Vertical
        {
            // Similar logic for vertical orientation
            int trackWidth = Math.Min(10, rect.Width / 3);
            var trackRect = new Rectangle(
                rect.X + (rect.Width - trackWidth) / 2,
                rect.Y,
                trackWidth,
                rect.Height
            );
            var trackBrush = new SolidBrush(TrackColor);
            g.FillRectangle(trackBrush, trackRect);

            // Draw filled portion (from bottom)
            var fillRect = new Rectangle(
                trackRect.X,
                trackRect.Y + (int)(trackRect.Height * (1 - progress)),
                trackRect.Width,
                (int)(trackRect.Height * progress)
            );
            var fillBrush = new SolidBrush(FillColor);
            g.FillRectangle(fillBrush, fillRect);

            // Draw thumb
            int thumbSize = Math.Min(20, rect.Width);
            var thumbRect = new Rectangle(
                rect.X + (rect.Width - thumbSize) / 2,
                trackRect.Y + (int)(trackRect.Height * (1 - progress)) - thumbSize / 2,
                thumbSize,
                thumbSize
            );
            var thumbBrush = new SolidBrush(ThumbColor);
            g.FillEllipse(thumbBrush, thumbRect);
            g.DrawEllipse(new Pen(isSelected ? Color.Blue : Color.Black, 1), thumbRect);

            trackBrush.Dispose();
            fillBrush.Dispose();
            thumbBrush.Dispose();
        }
    }

    public override BaseComponent Clone()
    {
        return new SliderComponent
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
            TrackColor = TrackColor,
            FillColor = FillColor,
            ThumbColor = ThumbColor,
            Orientation = Orientation,
            ShowValue = ShowValue
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["value"] = Value;
        json["minimum"] = Minimum;
        json["maximum"] = Maximum;
        json["trackColor"] = ColorTranslator.ToHtml(TrackColor);
        json["fillColor"] = ColorTranslator.ToHtml(FillColor);
        json["thumbColor"] = ColorTranslator.ToHtml(ThumbColor);
        json["orientation"] = Orientation.ToString();
        json["showValue"] = ShowValue;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Value = json["value"]?.ToObject<double>() ?? 50.0;
        Minimum = json["minimum"]?.ToObject<double>() ?? 0.0;
        Maximum = json["maximum"]?.ToObject<double>() ?? 100.0;
        
        if (ColorTranslator.FromHtml(json["trackColor"]?.ToString() ?? "#D3D3D3") is Color trackColor)
            TrackColor = trackColor;
        if (ColorTranslator.FromHtml(json["fillColor"]?.ToString() ?? "#0000FF") is Color fillColor)
            FillColor = fillColor;
        if (ColorTranslator.FromHtml(json["thumbColor"]?.ToString() ?? "#00008B") is Color thumbColor)
            ThumbColor = thumbColor;

        if (Enum.TryParse<Orientation>(json["orientation"]?.ToString() ?? "Horizontal", out var orientation))
        {
            Orientation = orientation;
        }

        ShowValue = json["showValue"]?.ToObject<bool>() ?? true;
    }
}
