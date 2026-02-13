using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using Svg;

namespace Designer.Modules.Components;

/// <summary>
/// SVG view component for displaying SVG files.
/// </summary>
public class SvgViewComponent : BaseComponent
{
    public override string ComponentType => "SVGView";

    public string SvgPath { get; set; } = string.Empty;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;

    /// <summary>
    /// Gets the base SVG directory path (executable directory/svg/).
    /// </summary>
    private static string GetSvgBasePath()
    {
        string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Application.StartupPath;
        return Path.Combine(exeDir, "svg");
    }

    /// <summary>
    /// Gets the full path to an SVG file from a relative path.
    /// </summary>
    private static string GetFullSvgPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return string.Empty;
        
        // If it's already an absolute path, return as-is
        if (Path.IsPathRooted(relativePath))
            return relativePath;
        
        // Otherwise, combine with SVG base path
        return Path.Combine(GetSvgBasePath(), relativePath);
    }

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Draw background
        var backBrush = new SolidBrush(Color.White);
        g.FillRectangle(backBrush, rect);
        
        // Try to render the actual SVG file
        bool svgRendered = false;
        if (!string.IsNullOrEmpty(SvgPath))
        {
            try
            {
                string fullSvgPath = GetFullSvgPath(SvgPath);
                if (File.Exists(fullSvgPath))
                {
                    var svgDoc = SvgDocument.Open(fullSvgPath);
                    if (svgDoc != null)
                    {
                        // Calculate draw size with padding
                        var drawSize = new SizeF(
                            Math.Max(1, rect.Width - 4),
                            Math.Max(1, rect.Height - 4)
                        );
                        
                        // Save graphics state
                        var state = g.Save();
                        
                        // Translate to draw position with padding
                        g.TranslateTransform(rect.X + 2, rect.Y + 2);
                        
                        // Render SVG to fit the component bounds
                        svgDoc.Draw(g, drawSize);
                        
                        // Restore graphics state
                        g.Restore(state);
                        svgRendered = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // If SVG rendering fails, fall back to placeholder
                System.Diagnostics.Debug.WriteLine($"Failed to render SVG '{SvgPath}': {ex.Message}");
            }
        }
        
        // Draw placeholder if SVG wasn't rendered
        if (!svgRendered)
        {
            var placeholderBrush = new SolidBrush(Color.LightBlue);
            g.FillRectangle(placeholderBrush, rect);
            
            var textBrush = new SolidBrush(Color.DarkBlue);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            string displayText = string.IsNullOrEmpty(SvgPath) ? "SVG" : Path.GetFileNameWithoutExtension(SvgPath);
            g.DrawString(displayText, SystemFonts.DefaultFont, textBrush, rect, stringFormat);
            
            placeholderBrush.Dispose();
            textBrush.Dispose();
        }

        // Draw border
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);
        g.DrawRectangle(borderPen, rect);
        borderPen.Dispose();

        backBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new SvgViewComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            SvgPath = SvgPath,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["svgPath"] = SvgPath;
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        SvgPath = json["svgPath"]?.ToString() ?? string.Empty;
        
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;
        
        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
    }
}
