using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Alarm view component for displaying alarm lists and status.
/// </summary>
public class AlarmViewComponent : BaseComponent
{
    public override string ComponentType => "AlarmView";

    public Color BackColor { get; set; } = Color.White;
    public Color HeaderBackColor { get; set; } = Color.LightGray;
    public Color HeaderForeColor { get; set; } = Color.Black;
    public Color ActiveAlarmColor { get; set; } = Color.Red;
    public Color AcknowledgedAlarmColor { get; set; } = Color.Yellow;
    public Color NormalColor { get; set; } = Color.Green;
    public bool ShowHeader { get; set; } = true;
    public List<string> Columns { get; set; } = new List<string> { "Time", "Tag", "Message", "Status" };
    public int MaxRows { get; set; } = 20;
    public Font HeaderFont { get; set; } = new Font("Arial", 9, FontStyle.Bold);
    public Font RowFont { get; set; } = new Font("Arial", 8);

    public override void Draw(Graphics g, bool isSelected = false)
    {
        var rect = Bounds;
        
        // Draw background
        var backBrush = new SolidBrush(BackColor);
        g.FillRectangle(backBrush, rect);
        
        if (isSelected)
        {
            var pen = new Pen(Color.Blue, 2);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }
        else
        {
            var pen = new Pen(Color.Gray, 1);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }

        int yPos = rect.Y;
        int rowHeight = 20;

        // Draw header
        if (ShowHeader && Columns.Count > 0)
        {
            var headerBrush = new SolidBrush(HeaderBackColor);
            var headerRect = new Rectangle(rect.X, yPos, rect.Width, rowHeight);
            g.FillRectangle(headerBrush, headerRect);
            
            var headerPen = new Pen(Color.Black, 1);
            g.DrawRectangle(headerPen, headerRect);
            
            // Draw column headers
            int colWidth = rect.Width / Columns.Count;
            var headerTextBrush = new SolidBrush(HeaderForeColor);
            for (int i = 0; i < Columns.Count; i++)
            {
                var colRect = new Rectangle(rect.X + i * colWidth, yPos, colWidth, rowHeight);
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(Columns[i], HeaderFont, headerTextBrush, colRect, stringFormat);
                
                // Draw column separator
                if (i < Columns.Count - 1)
                {
                    g.DrawLine(headerPen, colRect.Right, yPos, colRect.Right, yPos + rowHeight);
                }
            }
            
            yPos += rowHeight;
            headerBrush.Dispose();
            headerPen.Dispose();
            headerTextBrush.Dispose();
        }

        // Draw placeholder rows (would be replaced with actual alarm data)
        var rowTextBrush = new SolidBrush(Color.Black);
        int colWidth2 = rect.Width / Columns.Count;
        
        for (int row = 0; row < Math.Min(5, MaxRows); row++)
        {
            var rowRect = new Rectangle(rect.X, yPos, rect.Width, rowHeight);
            
            // Alternate row background
            if (row % 2 == 0)
            {
                var altBrush = new SolidBrush(Color.FromArgb(240, 240, 240));
                g.FillRectangle(altBrush, rowRect);
                altBrush.Dispose();
            }
            
            // Draw sample alarm data
            var sampleData = new[] { DateTime.Now.AddMinutes(-row).ToString("HH:mm:ss"), $"Tag{row + 1}", $"Alarm Message {row + 1}", row % 3 == 0 ? "Active" : "Normal" };
            
            for (int col = 0; col < Math.Min(Columns.Count, sampleData.Length); col++)
            {
                var colRect = new Rectangle(rect.X + col * colWidth2, yPos, colWidth2, rowHeight);
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                
                // Color code based on status
                if (col == Columns.Count - 1 && sampleData[col] == "Active")
                {
                    rowTextBrush = new SolidBrush(ActiveAlarmColor);
                }
                else if (col == Columns.Count - 1 && sampleData[col] == "Acknowledged")
                {
                    rowTextBrush = new SolidBrush(AcknowledgedAlarmColor);
                }
                else
                {
                    rowTextBrush = new SolidBrush(Color.Black);
                }
                
                g.DrawString(sampleData[col], RowFont, rowTextBrush, colRect, stringFormat);
            }
            
            // Draw row separator
            var rowPen = new Pen(Color.LightGray, 1);
            g.DrawLine(rowPen, rect.X, yPos + rowHeight, rect.Right, yPos + rowHeight);
            rowPen.Dispose();
            
            yPos += rowHeight;
            
            if (yPos >= rect.Bottom)
                break;
        }
        
        rowTextBrush.Dispose();
        backBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new AlarmViewComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            BackColor = BackColor,
            HeaderBackColor = HeaderBackColor,
            HeaderForeColor = HeaderForeColor,
            ActiveAlarmColor = ActiveAlarmColor,
            AcknowledgedAlarmColor = AcknowledgedAlarmColor,
            NormalColor = NormalColor,
            ShowHeader = ShowHeader,
            Columns = new List<string>(Columns),
            MaxRows = MaxRows,
            HeaderFont = new Font(HeaderFont.FontFamily, HeaderFont.Size, HeaderFont.Style),
            RowFont = new Font(RowFont.FontFamily, RowFont.Size, RowFont.Style)
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        var columnsArray = new JArray();
        foreach (var col in Columns)
        {
            columnsArray.Add(col);
        }
        json["columns"] = columnsArray;
        
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["headerBackColor"] = ColorTranslator.ToHtml(HeaderBackColor);
        json["headerForeColor"] = ColorTranslator.ToHtml(HeaderForeColor);
        json["activeAlarmColor"] = ColorTranslator.ToHtml(ActiveAlarmColor);
        json["acknowledgedAlarmColor"] = ColorTranslator.ToHtml(AcknowledgedAlarmColor);
        json["normalColor"] = ColorTranslator.ToHtml(NormalColor);
        json["showHeader"] = ShowHeader;
        json["maxRows"] = MaxRows;
        json["headerFont"] = HeaderFont.Name;
        json["headerFontSize"] = HeaderFont.Size;
        json["headerFontStyle"] = HeaderFont.Style.ToString();
        json["rowFont"] = RowFont.Name;
        json["rowFontSize"] = RowFont.Size;
        json["rowFontStyle"] = RowFont.Style.ToString();
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        Columns.Clear();
        var columnsArray = json["columns"] as JArray;
        if (columnsArray != null)
        {
            foreach (var item in columnsArray)
            {
                Columns.Add(item?.ToString() ?? string.Empty);
            }
        }
        if (Columns.Count == 0)
        {
            Columns.AddRange(new[] { "Time", "Tag", "Message", "Status" });
        }
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#FFFFFF") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["headerBackColor"]?.ToString() ?? "#D3D3D3") is Color headerBackColor)
            HeaderBackColor = headerBackColor;
        if (ColorTranslator.FromHtml(json["headerForeColor"]?.ToString() ?? "#000000") is Color headerForeColor)
            HeaderForeColor = headerForeColor;
        if (ColorTranslator.FromHtml(json["activeAlarmColor"]?.ToString() ?? "#FF0000") is Color activeAlarmColor)
            ActiveAlarmColor = activeAlarmColor;
        if (ColorTranslator.FromHtml(json["acknowledgedAlarmColor"]?.ToString() ?? "#FFFF00") is Color acknowledgedAlarmColor)
            AcknowledgedAlarmColor = acknowledgedAlarmColor;
        if (ColorTranslator.FromHtml(json["normalColor"]?.ToString() ?? "#008000") is Color normalColor)
            NormalColor = normalColor;

        ShowHeader = json["showHeader"]?.ToObject<bool>() ?? true;
        MaxRows = json["maxRows"]?.ToObject<int>() ?? 20;

        var headerFontName = json["headerFont"]?.ToString() ?? "Arial";
        var headerFontSize = json["headerFontSize"]?.ToObject<float>() ?? 9f;
        var headerFontStyleStr = json["headerFontStyle"]?.ToString() ?? "Bold";
        if (Enum.TryParse<FontStyle>(headerFontStyleStr, out var headerFontStyle))
        {
            HeaderFont = new Font(headerFontName, headerFontSize, headerFontStyle);
        }

        var rowFontName = json["rowFont"]?.ToString() ?? "Arial";
        var rowFontSize = json["rowFontSize"]?.ToObject<float>() ?? 8f;
        var rowFontStyleStr = json["rowFontStyle"]?.ToString() ?? "Regular";
        if (Enum.TryParse<FontStyle>(rowFontStyleStr, out var rowFontStyle))
        {
            RowFont = new Font(rowFontName, rowFontSize, rowFontStyle);
        }
    }
}
