using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Spinner/loading indicator component.
/// </summary>
public class SpinnerComponent : BaseComponent
{
    public override string ComponentType => "Spinner";

    public Color SpinnerColor { get; set; } = Color.Blue;
    public int SegmentCount { get; set; } = 8;
    public bool Animated { get; set; } = true;
    public double RotationAngle { get; set; } = 0.0; // For animation

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        
        int centerX = rect.X + rect.Width / 2;
        int centerY = rect.Y + rect.Height / 2;
        int radius = Math.Min(rect.Width, rect.Height) / 2 - 5;
        
        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawEllipse(pen, rect);
            pen.Dispose();
        }
        
        // Draw spinner segments
        double angleStep = 360.0 / SegmentCount;
        double startAngle = RotationAngle;
        
        for (int i = 0; i < SegmentCount; i++)
        {
            double angle = startAngle + (i * angleStep);
            double angleRad = angle * Math.PI / 180.0;
            
            // Calculate segment endpoints
            int x1 = centerX + (int)(radius * 0.7 * Math.Cos(angleRad));
            int y1 = centerY + (int)(radius * 0.7 * Math.Sin(angleRad));
            int x2 = centerX + (int)(radius * Math.Cos(angleRad));
            int y2 = centerY + (int)(radius * Math.Sin(angleRad));
            
            // Fade opacity based on segment index
            int alpha = 255 - (i * 255 / SegmentCount);
            var segmentColor = Color.FromArgb(alpha, SpinnerColor.R, SpinnerColor.G, SpinnerColor.B);
            var pen = new Pen(segmentColor, 3);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            
            g.DrawLine(pen, x1, y1, x2, y2);
            pen.Dispose();
        }
    }

    public override BaseComponent Clone()
    {
        return new SpinnerComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            SpinnerColor = SpinnerColor,
            SegmentCount = SegmentCount,
            Animated = Animated,
            RotationAngle = RotationAngle
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["spinnerColor"] = ColorTranslator.ToHtml(SpinnerColor);
        json["segmentCount"] = SegmentCount;
        json["animated"] = Animated;
        json["rotationAngle"] = RotationAngle;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        if (ColorTranslator.FromHtml(json["spinnerColor"]?.ToString() ?? "#0000FF") is Color spinnerColor)
            SpinnerColor = spinnerColor;
        
        SegmentCount = json["segmentCount"]?.ToObject<int>() ?? 8;
        Animated = json["animated"]?.ToObject<bool>() ?? true;
        RotationAngle = json["rotationAngle"]?.ToObject<double>() ?? 0.0;
    }
}
