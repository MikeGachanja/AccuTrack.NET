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
    public int BorderWidth { get; set; } = 0;

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
        
        // Draw background (transparent by default)
        // No background fill - SVG will render on transparent background
        
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
                        // Calculate draw area with padding
                        float drawWidth = Math.Max(1, rect.Width - 4);
                        float drawHeight = Math.Max(1, rect.Height - 4);
                        
                        // Get original SVG bounds
                        var originalBounds = svgDoc.Bounds;
                        float originalWidth = originalBounds.Width > 0 ? originalBounds.Width : (svgDoc.Width.Value > 0 ? svgDoc.Width.Value : 0);
                        float originalHeight = originalBounds.Height > 0 ? originalBounds.Height : (svgDoc.Height.Value > 0 ? svgDoc.Height.Value : 0);
                        
                        // If original dimensions are invalid, try to calculate from viewBox
                        if (originalWidth <= 0 || originalHeight <= 0)
                        {
                            if (svgDoc.ViewBox.Width > 0 && svgDoc.ViewBox.Height > 0)
                            {
                                originalWidth = svgDoc.ViewBox.Width;
                                originalHeight = svgDoc.ViewBox.Height;
                            }
                            else
                            {
                                // Fallback: use draw size
                                originalWidth = drawWidth;
                                originalHeight = drawHeight;
                            }
                        }
                        
                        // Set SVG document dimensions to fill the component space
                        svgDoc.Width = new Svg.SvgUnit(Svg.SvgUnitType.Pixel, drawWidth);
                        svgDoc.Height = new Svg.SvgUnit(Svg.SvgUnitType.Pixel, drawHeight);
                        
                        // Set ViewBox to original SVG bounds to ensure proper scaling
                        if (originalWidth > 0 && originalHeight > 0)
                        {
                            svgDoc.ViewBox = new Svg.SvgViewBox(0, 0, originalWidth, originalHeight);
                        }
                        
                        // Set aspect ratio to none to allow stretching (fill mode)
                        svgDoc.AspectRatio = new Svg.SvgAspectRatio(Svg.SvgPreserveAspectRatio.none);
                        
                        // Save graphics state
                        var state = g.Save();
                        
                        // Translate to draw position with padding
                        g.TranslateTransform(rect.X + 2, rect.Y + 2);
                        
                        // Render SVG to fill the component bounds
                        svgDoc.Draw(g);
                        
                        // Restore graphics state
                        g.Restore(state);
                        svgRendered = true;
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // Svg.NET can throw when parsing some SVG content (e.g. startIndex -1 from IndexOf)
            }
            catch (ArgumentException)
            {
                // Svg.NET parsing can throw for malformed or unsupported SVG values
            }
            catch (Exception ex)
            {
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
        
        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 0;
    }
}
