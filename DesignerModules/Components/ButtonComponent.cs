using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Button component for screens.
/// </summary>
public class ButtonComponent : BaseComponent
{
    public override string ComponentType => "Button";

    public string Text { get; set; } = "Button";
    public Color BackColor { get; set; } = Color.FromArgb(240, 240, 240);
    public Color ForeColor { get; set; } = Color.Black;
    public Color BorderColor { get; set; } = Color.Gray;
    public int BorderWidth { get; set; } = 1;
    public Font Font { get; set; } = new Font("Arial", 9);
    public string Action { get; set; } = string.Empty; // Script or action to execute

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        var brush = new SolidBrush(BackColor);
        var pen = new Pen(isSelected ? Color.Blue : BorderColor, BorderWidth);

        // Draw background
        g.FillRectangle(brush, rect);

        // Draw border
        g.DrawRectangle(pen, rect);

        // Draw text
        var textBrush = new SolidBrush(ForeColor);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(Text, Font, textBrush, rect, stringFormat);

        brush.Dispose();
        pen.Dispose();
        textBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new ButtonComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            EventIds = new List<string>(EventIds), // Copy event IDs
            Text = Text,
            BackColor = BackColor,
            ForeColor = ForeColor,
            BorderColor = BorderColor,
            BorderWidth = BorderWidth,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style),
            Action = Action
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        
        // Add button-specific properties to the properties object
        var propsObj = json["properties"] as JObject ?? new JObject();
        propsObj["text"] = Text;
        propsObj["backColor"] = ColorTranslator.ToHtml(BackColor);
        propsObj["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        propsObj["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        propsObj["borderWidth"] = BorderWidth;
        propsObj["font"] = Font.Name;
        propsObj["fontSize"] = Font.Size;
        propsObj["fontStyle"] = Font.Style.ToString();
        propsObj["action"] = Action;
        json["properties"] = propsObj;
        
        // Also keep text at root level for backward compatibility
        json["text"] = Text;
        
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        // Try to get properties from properties object first, then fallback to root level for backward compatibility
        var propsObj = json["properties"] as JObject;
        
        // Text property
        if (propsObj != null && propsObj["text"] != null)
            Text = propsObj["text"]?.ToString() ?? "Button";
        else
            Text = json["text"]?.ToString() ?? "Button";
        
        // Colors
        string backColorStr = propsObj?["backColor"]?.ToString() ?? json["backColor"]?.ToString() ?? "#F0F0F0";
        if (ColorTranslator.FromHtml(backColorStr) is Color backColor)
            BackColor = backColor;
            
        string foreColorStr = propsObj?["foreColor"]?.ToString() ?? json["foreColor"]?.ToString() ?? "#000000";
        if (ColorTranslator.FromHtml(foreColorStr) is Color foreColor)
            ForeColor = foreColor;
            
        string borderColorStr = propsObj?["borderColor"]?.ToString() ?? json["borderColor"]?.ToString() ?? "#808080";
        if (ColorTranslator.FromHtml(borderColorStr) is Color borderColor)
            BorderColor = borderColor;

        // Border width
        if (propsObj != null && propsObj["borderWidth"] != null)
            BorderWidth = propsObj["borderWidth"]?.ToObject<int>() ?? 1;
        else
            BorderWidth = json["borderWidth"]?.ToObject<int>() ?? 1;
            
        // Action
        if (propsObj != null && propsObj["action"] != null)
            Action = propsObj["action"]?.ToString() ?? string.Empty;
        else
            Action = json["action"]?.ToString() ?? string.Empty;

        // Font
        var fontName = propsObj?["font"]?.ToString() ?? json["font"]?.ToString() ?? "Arial";
        var fontSize = propsObj?["fontSize"]?.ToObject<float>() ?? json["fontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = propsObj?["fontStyle"]?.ToString() ?? json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }
    }
}
