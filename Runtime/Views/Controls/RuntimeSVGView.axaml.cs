using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Runtime.Modules.Screens;
using Svg;

namespace Runtime.Views.Controls;

public partial class RuntimeSVGView : UserControl
{
    private string? _currentSvgPath;

    public RuntimeSVGView()
    {
        InitializeComponent();
        // Re-render SVG when control size changes
        SizeChanged += (s, e) =>
        {
            if (!string.IsNullOrEmpty(_currentSvgPath) && e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                SetSvgPath(_currentSvgPath);
            }
        };
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        
        // Note: SVG path loading is handled by CreateSVGView after path resolution
        // Don't load SVG here as the path needs to be resolved first
        
        // Apply border properties if available
        string? borderColor = GetProperty(d, "borderColor", "");
        int borderWidth = GetPropertyInt(d, "borderWidth", 1);
        if (!string.IsNullOrEmpty(borderColor))
        {
            try
            {
                var color = ParseColor(borderColor);
                ContainerBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(
                    Avalonia.Media.Color.FromRgb(color.R, color.G, color.B));
            }
            catch { }
        }
        ContainerBorder.BorderThickness = new Avalonia.Thickness(borderWidth);
    }

    /// <summary>
    /// Sets the SVG file path and loads/renders it.
    /// </summary>
    public void SetSvgPath(string? svgPath)
    {
        _currentSvgPath = svgPath;
        
        if (string.IsNullOrEmpty(svgPath))
        {
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
            return;
        }
        
        if (!File.Exists(svgPath))
        {
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] SVG file not found: '{svgPath}'");
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
            return;
        }
        
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Loading SVG from: '{svgPath}'");

        try
        {
            // Load SVG document
            var svgDoc = SvgDocument.Open(svgPath);
            if (svgDoc == null)
            {
                SvgImage.Source = null;
                PlaceholderText.IsVisible = true;
                return;
            }

            // Get component bounds (use actual control size if available, otherwise use descriptor size or SVG default size)
            // Note: Width/Height might be 0 initially, so use Bounds or descriptor size
            var controlWidth = Width > 0 ? Width : (Bounds.Width > 0 ? Bounds.Width : 0);
            var controlHeight = Height > 0 ? Height : (Bounds.Height > 0 ? Bounds.Height : 0);
            int renderWidth = (int)Math.Max(1, controlWidth > 0 ? controlWidth : (svgDoc.Width.Value > 0 ? svgDoc.Width.Value : 100));
            int renderHeight = (int)Math.Max(1, controlHeight > 0 ? controlHeight : (svgDoc.Height.Value > 0 ? svgDoc.Height.Value : 100));

            // Get original SVG bounds
            var originalBounds = svgDoc.Bounds;
            float originalWidth = originalBounds.Width > 0 ? originalBounds.Width : (svgDoc.Width.Value > 0 ? svgDoc.Width.Value : renderWidth);
            float originalHeight = originalBounds.Height > 0 ? originalBounds.Height : (svgDoc.Height.Value > 0 ? svgDoc.Height.Value : renderHeight);

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
                    originalWidth = renderWidth;
                    originalHeight = renderHeight;
                }
            }

            // Set SVG document dimensions to fill the component space
            svgDoc.Width = new SvgUnit(SvgUnitType.Pixel, renderWidth);
            svgDoc.Height = new SvgUnit(SvgUnitType.Pixel, renderHeight);

            // Set ViewBox to original SVG bounds to ensure proper scaling
            if (originalWidth > 0 && originalHeight > 0)
            {
                svgDoc.ViewBox = new SvgViewBox(0, 0, originalWidth, originalHeight);
            }

            // Set aspect ratio to none to allow stretching (fill mode)
            svgDoc.AspectRatio = new SvgAspectRatio(SvgPreserveAspectRatio.none);

            // Render SVG to bitmap
            using (var bitmap = new System.Drawing.Bitmap(renderWidth, renderHeight))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.Clear(Color.White);

                    // Render SVG
                    svgDoc.Draw(g);
                }

                // Convert System.Drawing.Bitmap to Avalonia Bitmap
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    SvgImage.Source = new Avalonia.Media.Imaging.Bitmap(memory);
                }
            }

            PlaceholderText.IsVisible = false;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Svg.NET can throw when parsing some SVG content (e.g. startIndex -1 from IndexOf)
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
        }
        catch (ArgumentException)
        {
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"Failed to load SVG '{svgPath}': {ex.Message}");
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
        }
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static int GetPropertyInt(ComponentDescriptor d, string key, int fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is long l) return (int)l;
        if (int.TryParse(v?.ToString(), out var parsed)) return parsed;
        return fallback;
    }

    private static Color ParseColor(string colorHex)
    {
        try
        {
            if (colorHex.StartsWith("#"))
            {
                colorHex = colorHex.Substring(1);
            }
            
            if (colorHex.Length == 6)
            {
                int r = Convert.ToInt32(colorHex.Substring(0, 2), 16);
                int g = Convert.ToInt32(colorHex.Substring(2, 2), 16);
                int b = Convert.ToInt32(colorHex.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
        }
        catch { }
        
        return Color.Gray; // Default
    }
}
