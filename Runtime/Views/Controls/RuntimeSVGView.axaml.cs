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
        // Ensure component is visible
        IsVisible = true;
        Opacity = 1.0;
        MinWidth = 50;
        MinHeight = 50;
        
        // Re-render SVG when control size changes
        SizeChanged += (s, e) =>
        {
            if (!string.IsNullOrEmpty(_currentSvgPath) && e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Size changed to {e.NewSize.Width}x{e.NewSize.Height}, re-rendering SVG");
                SetSvgPath(_currentSvgPath);
            }
        };
    }

    /// <summary>Gets the current SVG path for re-loading after size is set.</summary>
    public string? GetCurrentSvgPath() => _currentSvgPath;

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        Opacity = 1.0; // Ensure full opacity
        
        // Ensure SVG image is visible
        SvgImage.IsVisible = true;
        SvgImage.Opacity = 1.0;
        
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
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ===== SetSvgPath called =====");
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Path: '{svgPath}'");
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Control size: {Width}x{Height}");
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Control bounds: {Bounds.Width}x{Bounds.Height}");
        
        _currentSvgPath = svgPath;
        
        if (string.IsNullOrEmpty(svgPath))
        {
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗ Path is null or empty - showing placeholder");
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
            return;
        }
        
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Checking if file exists: '{svgPath}'");
        var fileExists = File.Exists(svgPath);
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] File exists: {fileExists}");
        
        if (!fileExists)
        {
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗ SVG file not found: '{svgPath}'");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Current directory: '{Directory.GetCurrentDirectory()}'");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] File.Exists check failed - showing placeholder");
            SvgImage.Source = null;
            SvgImage.IsVisible = false;
            PlaceholderText.IsVisible = true;
            return;
        }
        
        // Get file info for logging
        try
        {
            var fileInfo = new FileInfo(svgPath);
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] File size: {fileInfo.Length} bytes");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] File last modified: {fileInfo.LastWriteTime}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Could not get file info: {ex.Message}");
        }
        
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✓ File exists, proceeding to load SVG");
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Setting SvgImage.IsVisible = true");
        SvgImage.IsVisible = true;

        try
        {
            // Load SVG document
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Attempting to open SVG document: '{svgPath}'");
            var svgDoc = SvgDocument.Open(svgPath);
            if (svgDoc == null)
            {
                System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗ SvgDocument.Open returned null");
                SvgImage.Source = null;
                PlaceholderText.IsVisible = true;
                return;
            }
            
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✓ SVG document opened successfully");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] SVG document width: {svgDoc.Width.Value} ({svgDoc.Width.Type})");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] SVG document height: {svgDoc.Height.Value} ({svgDoc.Height.Type})");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] SVG bounds: {svgDoc.Bounds}");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] SVG viewBox: {svgDoc.ViewBox}");

            // Get component bounds - ensure minimum size so SVG is always visible
            var controlWidth = Width > 0 ? Width : (Bounds.Width > 0 ? Bounds.Width : 0);
            var controlHeight = Height > 0 ? Height : (Bounds.Height > 0 ? Bounds.Height : 0);
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Control dimensions - Width: {Width}, Height: {Height}, Bounds: {Bounds.Width}x{Bounds.Height}");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Calculated control size: {controlWidth}x{controlHeight}");
            
            // Use control size if available, otherwise use SVG size, otherwise default to 100x100 minimum
            int renderWidth = (int)Math.Max(100, controlWidth > 0 ? controlWidth : (svgDoc.Width.Value > 0 ? svgDoc.Width.Value : 100));
            int renderHeight = (int)Math.Max(100, controlHeight > 0 ? controlHeight : (svgDoc.Height.Value > 0 ? svgDoc.Height.Value : 100));
            
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Final render size: {renderWidth}x{renderHeight}");

            // Get original SVG bounds
            var originalBounds = svgDoc.Bounds;
            float originalWidth = originalBounds.Width > 0 ? originalBounds.Width : (svgDoc.Width.Value > 0 ? svgDoc.Width.Value : renderWidth);
            float originalHeight = originalBounds.Height > 0 ? originalBounds.Height : (svgDoc.Height.Value > 0 ? svgDoc.Height.Value : renderHeight);
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Original SVG bounds: {originalBounds}");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Calculated original size: {originalWidth}x{originalHeight}");

            // If original dimensions are invalid, try to calculate from viewBox
            if (originalWidth <= 0 || originalHeight <= 0)
            {
                System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Original dimensions invalid, checking viewBox...");
                if (svgDoc.ViewBox.Width > 0 && svgDoc.ViewBox.Height > 0)
                {
                    originalWidth = svgDoc.ViewBox.Width;
                    originalHeight = svgDoc.ViewBox.Height;
                    System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Using viewBox dimensions: {originalWidth}x{originalHeight}");
                }
                else
                {
                    originalWidth = renderWidth;
                    originalHeight = renderHeight;
                    System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Using render dimensions as original: {originalWidth}x{originalHeight}");
                }
            }

            // Set SVG document dimensions to fill the component space
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Setting SVG document dimensions to {renderWidth}x{renderHeight}");
            svgDoc.Width = new SvgUnit(SvgUnitType.Pixel, renderWidth);
            svgDoc.Height = new SvgUnit(SvgUnitType.Pixel, renderHeight);

            // Set ViewBox to original SVG bounds to ensure proper scaling
            if (originalWidth > 0 && originalHeight > 0)
            {
                System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Setting viewBox to (0, 0, {originalWidth}, {originalHeight})");
                svgDoc.ViewBox = new SvgViewBox(0, 0, originalWidth, originalHeight);
            }

            // Set aspect ratio to none to allow stretching (fill mode)
            svgDoc.AspectRatio = new SvgAspectRatio(SvgPreserveAspectRatio.none);
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Aspect ratio set to none (fill mode)");

            // Render SVG to bitmap with transparency support
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Creating bitmap: {renderWidth}x{renderHeight}");
            using (var bitmap = new System.Drawing.Bitmap(renderWidth, renderHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Bitmap created successfully (32-bit ARGB for transparency)");
                using (var g = Graphics.FromImage(bitmap))
                {
                    System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Setting graphics quality settings");
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.Clear(Color.Transparent);

                    // Render SVG
                    System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Drawing SVG to bitmap...");
                    svgDoc.Draw(g);
                    System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✓ SVG drawn to bitmap successfully");
                }

                // Convert System.Drawing.Bitmap to Avalonia Bitmap
                System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Converting bitmap to PNG format...");
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Bitmap saved to memory stream, size: {memory.Length} bytes");
                    memory.Position = 0;
                    
                    System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Creating Avalonia bitmap from memory stream...");
                    var avaloniaBitmap = new Avalonia.Media.Imaging.Bitmap(memory);
                    
                    if (avaloniaBitmap != null)
                    {
                        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Avalonia bitmap created, pixel size: {avaloniaBitmap.PixelSize.Width}x{avaloniaBitmap.PixelSize.Height}");
                        
                        if (avaloniaBitmap.PixelSize.Width > 0 && avaloniaBitmap.PixelSize.Height > 0)
                        {
                            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Setting bitmap as Image source...");
                            SvgImage.Source = avaloniaBitmap;
                            SvgImage.IsVisible = true;
                            SvgImage.Opacity = 1.0;
                            // Ensure Image fills the container
                            SvgImage.Width = double.NaN; // Auto/Stretch
                            SvgImage.Height = double.NaN; // Auto/Stretch
                            PlaceholderText.IsVisible = false;
                            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✓✓✓ SVG loaded and displayed successfully ✓✓✓");
                            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Image control - IsVisible: {SvgImage.IsVisible}, Opacity: {SvgImage.Opacity}, Source: {(SvgImage.Source != null ? "Set" : "Null")}");
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗ Avalonia bitmap has invalid pixel size: {avaloniaBitmap.PixelSize.Width}x{avaloniaBitmap.PixelSize.Height}");
                            SvgImage.Source = null;
                            PlaceholderText.IsVisible = true;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗ Failed to create Avalonia bitmap (returned null)");
                        SvgImage.Source = null;
                        PlaceholderText.IsVisible = true;
                    }
                }
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // Svg.NET can throw when parsing some SVG content (e.g. startIndex -1 from IndexOf)
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗ ArgumentOutOfRangeException while loading SVG: {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] StackTrace: {ex.StackTrace}");
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗ ArgumentException while loading SVG: {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] StackTrace: {ex.StackTrace}");
            SvgImage.Source = null;
            PlaceholderText.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ✗✗✗ EXCEPTION while loading SVG '{svgPath}' ✗✗✗");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Exception type: {ex.GetType().Name}");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Exception message: {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] Inner exception: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
            SvgImage.Source = null;
            SvgImage.IsVisible = false;
            PlaceholderText.IsVisible = true;
        }
        
        System.Diagnostics.Trace.WriteLine($"[RuntimeSVGView] ===== SetSvgPath completed =====");
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
