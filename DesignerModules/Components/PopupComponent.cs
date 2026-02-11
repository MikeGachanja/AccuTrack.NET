using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Popup component for displaying popup dialogs or overlays.
/// </summary>
public class PopupComponent : BaseComponent
{
    public override string ComponentType => "Popup";

    public string Title { get; set; } = "Popup";
    public bool Modal { get; set; } = false;
    public Color BackColor { get; set; } = Color.White;
    public Color TitleBackColor { get; set; } = Color.LightGray;
    public Color TitleForeColor { get; set; } = Color.Black;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 2;
    public bool ShowTitleBar { get; set; } = true;
    public int TitleBarHeight { get; set; } = 25;
    public Font TitleFont { get; set; } = new Font("Arial", 9, FontStyle.Bold);
    public bool Resizable { get; set; } = true;
    public bool Movable { get; set; } = true;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Draw popup background
        var backBrush = new SolidBrush(BackColor);
        g.FillRectangle(backBrush, rect);
        
        // Draw title bar
        if (ShowTitleBar)
        {
            var titleRect = new Rectangle(rect.X, rect.Y, rect.Width, TitleBarHeight);
            var titleBrush = new SolidBrush(TitleBackColor);
            g.FillRectangle(titleBrush, titleRect);
            
            // Draw title text
            var titleTextBrush = new SolidBrush(TitleForeColor);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };
            var titleTextRect = new Rectangle(rect.X + 5, rect.Y, rect.Width - 10, TitleBarHeight);
            g.DrawString(Title, TitleFont, titleTextBrush, titleTextRect, stringFormat);
            
            titleBrush.Dispose();
            titleTextBrush.Dispose();
        }
        
        // Draw border
        var borderPen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);
        g.DrawRectangle(borderPen, rect);
        
        // Draw content area placeholder
        var contentRect = new Rectangle(
            rect.X + 5,
            rect.Y + (ShowTitleBar ? TitleBarHeight : 0) + 5,
            rect.Width - 10,
            rect.Height - (ShowTitleBar ? TitleBarHeight : 0) - 10
        );
        
        var placeholderBrush = new SolidBrush(Color.FromArgb(240, 240, 240));
        var placeholderFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("Popup Content", SystemFonts.DefaultFont, placeholderBrush, contentRect, placeholderFormat);
        
        backBrush.Dispose();
        borderPen.Dispose();
        placeholderBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new PopupComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            Title = Title,
            Modal = Modal,
            BackColor = BackColor,
            TitleBackColor = TitleBackColor,
            TitleForeColor = TitleForeColor,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            ShowTitleBar = ShowTitleBar,
            TitleBarHeight = TitleBarHeight,
            TitleFont = new Font(TitleFont.FontFamily, TitleFont.Size, TitleFont.Style),
            Resizable = Resizable,
            Movable = Movable
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["title"] = Title;
        json["modal"] = Modal;
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["titleBackColor"] = ColorTranslator.ToHtml(TitleBackColor);
        json["titleForeColor"] = ColorTranslator.ToHtml(TitleForeColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["borderWidth"] = BorderWidth;
        json["showTitleBar"] = ShowTitleBar;
        json["titleBarHeight"] = TitleBarHeight;
        json["titleFont"] = TitleFont.Name;
        json["titleFontSize"] = TitleFont.Size;
        json["titleFontStyle"] = TitleFont.Style.ToString();
        json["resizable"] = Resizable;
        json["movable"] = Movable;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        Title = json["title"]?.ToString() ?? "Popup";
        Modal = json["modal"]?.ToObject<bool>() ?? false;
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#FFFFFF") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["titleBackColor"]?.ToString() ?? "#D3D3D3") is Color titleBackColor)
            TitleBackColor = titleBackColor;
        if (ColorTranslator.FromHtml(json["titleForeColor"]?.ToString() ?? "#000000") is Color titleForeColor)
            TitleForeColor = titleForeColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;
        
        BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 2;
        ShowTitleBar = json["showTitleBar"]?.ToObject<bool>() ?? true;
        TitleBarHeight = json["titleBarHeight"]?.ToObject<int>() ?? 25;
        Resizable = json["resizable"]?.ToObject<bool>() ?? true;
        Movable = json["movable"]?.ToObject<bool>() ?? true;
        
        var fontName = json["titleFont"]?.ToString() ?? "Arial";
        var fontSize = json["titleFontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["titleFontStyle"]?.ToString() ?? "Bold";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            TitleFont = new Font(fontName, fontSize, fontStyle);
        }
    }
}
