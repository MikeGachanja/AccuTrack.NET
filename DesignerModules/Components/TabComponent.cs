using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Tab component for organizing content into tabs.
/// </summary>
public class TabComponent : BaseComponent
{
    public override string ComponentType => "Tab";

    public List<string> TabLabels { get; set; } = new List<string> { "Tab1", "Tab2", "Tab3" };
    public int SelectedTabIndex { get; set; } = 0;
    public Color TabBackColor { get; set; } = Color.LightGray;
    public Color SelectedTabBackColor { get; set; } = Color.White;
    public Color TabForeColor { get; set; } = Color.Black;
    public Color ContentBackColor { get; set; } = Color.White;
    public Color BorderColor { get; set; } = Color.Gray;
    public Font TabFont { get; set; } = new Font("Arial", 9);
    public int TabHeight { get; set; } = 25;

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        if (TabLabels.Count == 0)
        {
            TabLabels.Add("Tab1");
        }
        
        // Draw tabs
        int tabWidth = rect.Width / TabLabels.Count;
        int tabY = rect.Y;
        
        for (int i = 0; i < TabLabels.Count; i++)
        {
            var tabRect = new Rectangle(rect.X + i * tabWidth, tabY, tabWidth, TabHeight);
            bool isSelectedTab = i == SelectedTabIndex;
            
            // Draw tab background
            var tabBrush = new SolidBrush(isSelectedTab ? SelectedTabBackColor : TabBackColor);
            g.FillRectangle(tabBrush, tabRect);
            
            // Draw tab border
            var tabPen = new Pen(BorderColor, 1);
            g.DrawRectangle(tabPen, tabRect);
            
            // Draw tab text
            var textBrush = new SolidBrush(TabForeColor);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(TabLabels[i], TabFont, textBrush, tabRect, stringFormat);
            
            tabBrush.Dispose();
            tabPen.Dispose();
            textBrush.Dispose();
        }
        
        // Draw content area
        var contentRect = new Rectangle(
            rect.X,
            rect.Y + TabHeight,
            rect.Width,
            rect.Height - TabHeight
        );
        
        var contentBrush = new SolidBrush(ContentBackColor);
        g.FillRectangle(contentBrush, contentRect);
        
        var contentPen = new Pen(isSelected ? Color.Blue : BorderColor, 1);
        g.DrawRectangle(contentPen, contentRect);
        
        // Draw placeholder content
        var placeholderBrush = new SolidBrush(Color.LightGray);
        var placeholderFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString($"Content: {TabLabels[SelectedTabIndex]}", SystemFonts.DefaultFont, placeholderBrush, contentRect, placeholderFormat);
        
        contentBrush.Dispose();
        contentPen.Dispose();
        placeholderBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new TabComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            TabLabels = new List<string>(TabLabels),
            SelectedTabIndex = SelectedTabIndex,
            TabBackColor = TabBackColor,
            SelectedTabBackColor = SelectedTabBackColor,
            TabForeColor = TabForeColor,
            ContentBackColor = ContentBackColor,
            BorderColor = BorderColor,
            TabFont = new Font(TabFont.FontFamily, TabFont.Size, TabFont.Style),
            TabHeight = TabHeight
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        var tabLabelsArray = new JArray();
        foreach (var label in TabLabels)
        {
            tabLabelsArray.Add(label);
        }
        json["tabLabels"] = tabLabelsArray;
        json["selectedTabIndex"] = SelectedTabIndex;
        json["tabBackColor"] = ColorTranslator.ToHtml(TabBackColor);
        json["selectedTabBackColor"] = ColorTranslator.ToHtml(SelectedTabBackColor);
        json["tabForeColor"] = ColorTranslator.ToHtml(TabForeColor);
        json["contentBackColor"] = ColorTranslator.ToHtml(ContentBackColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["tabFont"] = TabFont.Name;
        json["tabFontSize"] = TabFont.Size;
        json["tabFontStyle"] = TabFont.Style.ToString();
        json["tabHeight"] = TabHeight;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        TabLabels.Clear();
        var tabLabelsArray = json["tabLabels"] as JArray;
        if (tabLabelsArray != null)
        {
            foreach (var item in tabLabelsArray)
            {
                TabLabels.Add(item?.ToString() ?? string.Empty);
            }
        }
        if (TabLabels.Count == 0)
        {
            TabLabels.AddRange(new[] { "Tab1", "Tab2", "Tab3" });
        }
        
        SelectedTabIndex = json["selectedTabIndex"]?.ToObject<int>() ?? 0;
        if (SelectedTabIndex >= TabLabels.Count)
        {
            SelectedTabIndex = 0;
        }
        
        if (ColorTranslator.FromHtml(json["tabBackColor"]?.ToString() ?? "#D3D3D3") is Color tabBackColor)
            TabBackColor = tabBackColor;
        if (ColorTranslator.FromHtml(json["selectedTabBackColor"]?.ToString() ?? "#FFFFFF") is Color selectedTabBackColor)
            SelectedTabBackColor = selectedTabBackColor;
        if (ColorTranslator.FromHtml(json["tabForeColor"]?.ToString() ?? "#000000") is Color tabForeColor)
            TabForeColor = tabForeColor;
        if (ColorTranslator.FromHtml(json["contentBackColor"]?.ToString() ?? "#FFFFFF") is Color contentBackColor)
            ContentBackColor = contentBackColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;
        
        TabHeight = json["tabHeight"]?.ToObject<int>() ?? 25;
        
        var fontName = json["tabFont"]?.ToString() ?? "Arial";
        var fontSize = json["tabFontSize"]?.ToObject<float>() ?? 9f;
        var fontStyleStr = json["tabFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(fontStyleStr, out var fontStyle))
        {
            TabFont = new Font(fontName, fontSize, fontStyle);
        }
    }
}
