using System;
using System.Drawing;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Checkbox component for boolean input.
/// </summary>
public class CheckboxComponent : BaseComponent
{
    public override string ComponentType => "Checkbox";

    public string Text { get; set; } = "Checkbox";
    public bool Checked { get; set; } = false;
    public Color ForeColor { get; set; } = Color.Black;
    /// <summary>Color of the checkbox label text.</summary>
    public Color TextColor { get; set; } = Color.Black;
    public Color CheckColor { get; set; } = Color.Blue;
    public Font Font { get; set; } = new Font("Arial", 9);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        int checkBoxSize = Math.Min(20, Math.Min(rect.Height, rect.Width - 50));
        var checkBoxRect = new Rectangle(rect.X + 5, rect.Y + (rect.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);

        // Draw checkbox border
        var pen = new Pen(isSelected ? Color.Blue : Color.Gray, 1);
        g.DrawRectangle(pen, checkBoxRect);

        // Draw checkmark if checked
        if (Checked)
        {
            var checkPen = new Pen(CheckColor, 2);
            g.DrawLine(checkPen, checkBoxRect.Left + 3, checkBoxRect.Top + checkBoxRect.Height / 2,
                checkBoxRect.Left + checkBoxRect.Width / 2, checkBoxRect.Bottom - 3);
            g.DrawLine(checkPen, checkBoxRect.Left + checkBoxRect.Width / 2, checkBoxRect.Bottom - 3,
                checkBoxRect.Right - 3, checkBoxRect.Top + 3);
            checkPen.Dispose();
        }

        // Draw text (use TextColor)
        var textRect = new Rectangle(checkBoxRect.Right + 5, rect.Y, rect.Width - checkBoxRect.Width - 10, rect.Height);
        var brush = new SolidBrush(TextColor);
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
        return new CheckboxComponent
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
            ForeColor = ForeColor,
            TextColor = TextColor,
            CheckColor = CheckColor,
            Font = new Font(Font.FontFamily, Font.Size, Font.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        json["text"] = Text;
        json["checked"] = Checked;
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["textColor"] = ColorTranslator.ToHtml(TextColor);
        json["checkColor"] = ColorTranslator.ToHtml(CheckColor);
        json["font"] = Font.Name;
        json["fontSize"] = Font.Size;
        json["fontStyle"] = Font.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        Text = json["text"]?.ToString() ?? "Checkbox";
        Checked = json["checked"]?.ToObject<bool>() ?? false;
        
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#000000") is Color foreColor)
            ForeColor = foreColor;
        if (json["textColor"] != null && ColorTranslator.FromHtml(json["textColor"]?.ToString() ?? "") is Color textColor)
            TextColor = textColor;
        else
            TextColor = ForeColor;
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
