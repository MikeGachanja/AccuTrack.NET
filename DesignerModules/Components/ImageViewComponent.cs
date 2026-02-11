using System;
using System.Drawing;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Image view component for displaying images.
/// </summary>
public class ImageViewComponent : BaseComponent
{
    public override string ComponentType => "ImageView";

    public string ImagePath { get; set; } = string.Empty;
    public ImageDisplayMode DisplayMode { get; set; } = ImageDisplayMode.Stretch;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;

    private Image? _cachedImage;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Load image if needed
        if (_cachedImage == null && !string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
        {
            try
            {
                _cachedImage = Image.FromFile(ImagePath);
            }
            catch
            {
                _cachedImage = null;
            }
        }

        // Draw background
        var backBrush = new SolidBrush(Color.White);
        g.FillRectangle(backBrush, rect);

        // Draw image
        if (_cachedImage != null)
        {
            Rectangle imageRect = DisplayMode switch
            {
                ImageDisplayMode.Stretch => rect,
                ImageDisplayMode.Center => new Rectangle(
                    rect.X + (rect.Width - _cachedImage.Width) / 2,
                    rect.Y + (rect.Height - _cachedImage.Height) / 2,
                    _cachedImage.Width,
                    _cachedImage.Height
                ),
                ImageDisplayMode.Fit => CalculateFitRect(rect, _cachedImage.Size),
                _ => rect
            };
            
            g.DrawImage(_cachedImage, imageRect);
        }
        else
        {
            // Draw placeholder
            var placeholderBrush = new SolidBrush(Color.LightGray);
            g.FillRectangle(placeholderBrush, rect);
            
            var textBrush = new SolidBrush(Color.Gray);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            string placeholderText = string.IsNullOrEmpty(ImagePath) ? "No Image" : "Image Not Found";
            g.DrawString(placeholderText, SystemFonts.DefaultFont, textBrush, rect, stringFormat);
            
            placeholderBrush.Dispose();
            textBrush.Dispose();
        }

        // Draw border
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);
        g.DrawRectangle(borderPen, rect);
        borderPen.Dispose();

        backBrush.Dispose();
    }

    private Rectangle CalculateFitRect(Rectangle container, Size imageSize)
    {
        double scaleX = (double)container.Width / imageSize.Width;
        double scaleY = (double)container.Height / imageSize.Height;
        double scale = Math.Min(scaleX, scaleY);

        int width = (int)(imageSize.Width * scale);
        int height = (int)(imageSize.Height * scale);
        int x = container.X + (container.Width - width) / 2;
        int y = container.Y + (container.Height - height) / 2;

        return new Rectangle(x, y, width, height);
    }

    public override BaseComponent Clone()
    {
        return new ImageViewComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            ImagePath = ImagePath,
            DisplayMode = DisplayMode,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["imagePath"] = ImagePath;
        json["displayMode"] = DisplayMode.ToString();
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        ImagePath = json["imagePath"]?.ToString() ?? string.Empty;
        
        if (Enum.TryParse<ImageDisplayMode>(json["displayMode"]?.ToString() ?? "Stretch", out var mode))
        {
            DisplayMode = mode;
        }
        
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;
        
        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
        
        // Clear cached image to reload
        _cachedImage = null;
    }
}

public enum ImageDisplayMode
{
    Stretch,
    Center,
    Fit
}
