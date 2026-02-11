using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Radio button component for single selection from a group.
/// </summary>
public class RadioButtonComponent : BaseComponent
{
    public override string ComponentType => "RadioButton";

    public string Text { get; set; } = "RadioButton";
    public bool Checked { get; set; } = false;
    public string GroupName { get; set; } = "Group1";
    public Color ForeColor { get; set; } = Color.Black;
    public Color CheckColor { get; set; } = Color.Blue;
    public Font Font { get; set; } = new Font("Arial", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        int radioSize = Math.Min(20, Math.Min(rect.Height, rect.Width - 50));
        var radioRect = new Rectangle(rect.X + 5, rect.Y + (rect.Height - radioSize) / 2, radioSize, radioSize);

        // Draw radio button circle
        var pen = new Pen(isSelected ? Color.Blue : Color.Gray, 1);
        g.DrawEllipse(pen, radioRect);

        // Draw filled circle if checked
        if (Checked)
        {
            var checkBrush = new SolidBrush(CheckColor);
            int innerSize = radioSize - 6;
            var innerRect = new Rectangle(
                radioRect.X + 3,
                radioRect.Y + 3,
                innerSize,
                innerSize
            );
            g.FillEllipse(checkBrush, innerRect);
            checkBrush.Dispose();
        }

        // Draw text
        var textRect = new Rectangle(radioRect.Right + 5, rect.Y, rect.Width - radioRect.Width - 10, rect.Height);
        var brush = new SolidBrush(ForeColor);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(Text, Font, brush, textRect, stringFormat);

        pen.Dispose();
        brush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new RadioButtonComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            Text = Text,
            Checked = Checked,
            GroupName = GroupName,
            ForeColor = ForeColor,
            CheckColor = CheckColor,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["text"] = Text;
        json["checked"] = Checked;
        json["groupName"] = GroupName;
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["checkColor"] = ColorTranslator.ToHtml(CheckColor);
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Text = json["text"]?.ToString() ?? "RadioButton";
        Checked = json["checked"]?.ToObject<bool>() ?? false;
        GroupName = json["groupName"]?.ToString() ?? "Group1";
        
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#000000") is Color foreColor)
            ForeColor = foreColor;
        if (ColorTranslator.FromHtml(json["checkColor"]?.ToString() ?? "#0000FF") is Color checkColor)
            CheckColor = checkColor;

        var fontName = json["font"]?.ToString() ?? "Arial";
        var fontSize = json["fontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["fontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            Font = new Font(fontName, fontSize, fontStyle);
        }
    }
}
