using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Table component for displaying tabular data.
/// </summary>
public class TableComponent : BaseComponent
{
    public override string ComponentType => "Table";

    public List<string> ColumnHeaders { get; set; } = new List<string> { "Column1", "Column2", "Column3" };
    public List<List<string>> Rows { get; set; } = new List<List<string>>();
    public Color BackColor { get; set; } = Color.White;
    public Color HeaderBackColor { get; set; } = Color.LightGray;
    public Color HeaderForeColor { get; set; } = Color.Black;
    public Color RowBackColor { get; set; } = Color.White;
    public Color AlternateRowBackColor { get; set; } = Color.FromArgb(240, 240, 240);
    public Color ForeColor { get; set; } = Color.Black;
    public Color BorderColor { get; set; } = Color.Gray;
    public bool ShowHeader { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public bool AlternateRows { get; set; } = true;
    public Font HeaderFont { get; set; } = new Font("Arial", 9, FontStyle.Bold);
    public Font RowFont { get; set; } = new Font("Arial", 8);

    /// <summary>Data source: "TagData" = live tag table, "Historian" = historical query.</summary>
    public string DataSource { get; set; } = "TagData";
    /// <summary>When DataSource is TagData: name of the tag table to bind (rows/columns from tags).</summary>
    public string TagTableName { get; set; } = string.Empty;
    /// <summary>When DataSource is Historian: tag name to query history for.</summary>
    public string HistorianTagName { get; set; } = string.Empty;
    /// <summary>When DataSource is Historian: time range in minutes to query.</summary>
    public int HistorianTimeRangeMinutes { get; set; } = 60;

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
            var pen = new Pen(BorderColor, 1);
            g.DrawRectangle(pen, rect);
            pen.Dispose();
        }

        int yPos = rect.Y;
        int rowHeight = 20;
        int headerHeight = ShowHeader ? rowHeight : 0;

        // Draw header
        if (ShowHeader && ColumnHeaders.Count > 0)
        {
            var headerBrush = new SolidBrush(HeaderBackColor);
            var headerRect = new Rectangle(rect.X, yPos, rect.Width, headerHeight);
            g.FillRectangle(headerBrush, headerRect);
            
            if (ShowGrid)
            {
                var gridPen = new Pen(BorderColor, 1);
                g.DrawRectangle(gridPen, headerRect);
                gridPen.Dispose();
            }
            
            // Draw column headers
            int colWidth = rect.Width / ColumnHeaders.Count;
            var headerTextBrush = new SolidBrush(HeaderForeColor);
            for (int i = 0; i < ColumnHeaders.Count; i++)
            {
                var colRect = new Rectangle(rect.X + i * colWidth, yPos, colWidth, headerHeight);
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(ColumnHeaders[i], HeaderFont, headerTextBrush, colRect, stringFormat);
                
                // Draw column separator
                if (ShowGrid && i < ColumnHeaders.Count - 1)
                {
                    var sepPen = new Pen(BorderColor, 1);
                    g.DrawLine(sepPen, colRect.Right, yPos, colRect.Right, yPos + headerHeight);
                    sepPen.Dispose();
                }
            }
            
            yPos += headerHeight;
            headerBrush.Dispose();
            headerTextBrush.Dispose();
        }

        // Draw rows
        var rowTextBrush = new SolidBrush(ForeColor);
        int colWidth2 = rect.Width / ColumnHeaders.Count;
        int visibleRows = Math.Min(Rows.Count, (rect.Height - (yPos - rect.Y)) / rowHeight);
        
        for (int row = 0; row < visibleRows; row++)
        {
            var rowRect = new Rectangle(rect.X, yPos, rect.Width, rowHeight);
            
            // Alternate row background
            if (AlternateRows && row % 2 == 1)
            {
                var altBrush = new SolidBrush(AlternateRowBackColor);
                g.FillRectangle(altBrush, rowRect);
                altBrush.Dispose();
            }
            else
            {
                var rowBrush = new SolidBrush(RowBackColor);
                g.FillRectangle(rowBrush, rowRect);
                rowBrush.Dispose();
            }
            
            // Draw row data
            if (row < Rows.Count)
            {
                var rowData = Rows[row];
                for (int col = 0; col < Math.Min(ColumnHeaders.Count, rowData.Count); col++)
                {
                    var colRect = new Rectangle(rect.X + col * colWidth2, yPos, colWidth2, rowHeight);
                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(rowData[col], RowFont, rowTextBrush, colRect, stringFormat);
                    
                    // Draw column separator
                    if (ShowGrid && col < ColumnHeaders.Count - 1)
                    {
                        var sepPen = new Pen(BorderColor, 1);
                        g.DrawLine(sepPen, colRect.Right, yPos, colRect.Right, yPos + rowHeight);
                        sepPen.Dispose();
                    }
                }
            }
            
            // Draw row separator
            if (ShowGrid)
            {
                var rowPen = new Pen(BorderColor, 1);
                g.DrawLine(rowPen, rect.X, yPos + rowHeight, rect.Right, yPos + rowHeight);
                rowPen.Dispose();
            }
            
            yPos += rowHeight;
            
            if (yPos >= rect.Bottom)
                break;
        }
        
        rowTextBrush.Dispose();
        backBrush.Dispose();
    }

    public override BaseComponent Clone()
    {
        return new TableComponent
        {
            Id = Guid.NewGuid(),
            Name = $"{Name}_Copy",
            Location = Location,
            Size = Size,
            Visible = Visible,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TagName = TagName,
            ColumnHeaders = new List<string>(ColumnHeaders),
            Rows = Rows.Select(r => new List<string>(r)).ToList(),
            BackColor = BackColor,
            HeaderBackColor = HeaderBackColor,
            HeaderForeColor = HeaderForeColor,
            RowBackColor = RowBackColor,
            AlternateRowBackColor = AlternateRowBackColor,
            ForeColor = ForeColor,
            BorderColor = BorderColor,
            ShowHeader = ShowHeader,
            ShowGrid = ShowGrid,
            AlternateRows = AlternateRows,
            HeaderFont = new Font(HeaderFont.FontFamily, HeaderFont.Size, HeaderFont.Style),
            RowFont = new Font(RowFont.FontFamily, RowFont.Size, RowFont.Style),
            DataSource = DataSource,
            TagTableName = TagTableName,
            HistorianTagName = HistorianTagName,
            HistorianTimeRangeMinutes = HistorianTimeRangeMinutes
        };
    }

    public override JObject ToJson()
    {
        var json = base.ToJson();
        
        var headersArray = new JArray();
        foreach (var header in ColumnHeaders)
        {
            headersArray.Add(header);
        }
        json["columnHeaders"] = headersArray;
        
        var rowsArray = new JArray();
        foreach (var row in Rows)
        {
            var rowArray = new JArray();
            foreach (var cell in row)
            {
                rowArray.Add(cell);
            }
            rowsArray.Add(rowArray);
        }
        json["rows"] = rowsArray;
        
        json["backColor"] = ColorTranslator.ToHtml(BackColor);
        json["headerBackColor"] = ColorTranslator.ToHtml(HeaderBackColor);
        json["headerForeColor"] = ColorTranslator.ToHtml(HeaderForeColor);
        json["rowBackColor"] = ColorTranslator.ToHtml(RowBackColor);
        json["alternateRowBackColor"] = ColorTranslator.ToHtml(AlternateRowBackColor);
        json["foreColor"] = ColorTranslator.ToHtml(ForeColor);
        json["borderColor"] = ColorTranslator.ToHtml(BorderColor);
        json["showHeader"] = ShowHeader;
        json["showGrid"] = ShowGrid;
        json["alternateRows"] = AlternateRows;
        json["headerFont"] = HeaderFont.Name;
        json["headerFontSize"] = HeaderFont.Size;
        json["headerFontStyle"] = HeaderFont.Style.ToString();
        json["rowFont"] = RowFont.Name;
        json["rowFontSize"] = RowFont.Size;
        json["rowFontStyle"] = RowFont.Style.ToString();
        json["dataSource"] = DataSource;
        json["tagTableName"] = TagTableName;
        json["historianTagName"] = HistorianTagName;
        json["historianTimeRangeMinutes"] = HistorianTimeRangeMinutes;
        return json;
    }

    public override void FromJson(JObject json)
    {
        base.FromJson(json);
        
        ColumnHeaders.Clear();
        var headersArray = json["columnHeaders"] as JArray;
        if (headersArray != null)
        {
            foreach (var item in headersArray)
            {
                ColumnHeaders.Add(item?.ToString() ?? string.Empty);
            }
        }
        if (ColumnHeaders.Count == 0)
        {
            ColumnHeaders.AddRange(new[] { "Column1", "Column2", "Column3" });
        }
        
        Rows.Clear();
        var rowsArray = json["rows"] as JArray;
        if (rowsArray != null)
        {
            foreach (var rowItem in rowsArray)
            {
                if (rowItem is JArray rowArray)
                {
                    var row = new List<string>();
                    foreach (var cell in rowArray)
                    {
                        row.Add(cell?.ToString() ?? string.Empty);
                    }
                    Rows.Add(row);
                }
            }
        }
        
        if (ColorTranslator.FromHtml(json["backColor"]?.ToString() ?? "#FFFFFF") is Color backColor)
            BackColor = backColor;
        if (ColorTranslator.FromHtml(json["headerBackColor"]?.ToString() ?? "#D3D3D3") is Color headerBackColor)
            HeaderBackColor = headerBackColor;
        if (ColorTranslator.FromHtml(json["headerForeColor"]?.ToString() ?? "#000000") is Color headerForeColor)
            HeaderForeColor = headerForeColor;
        if (ColorTranslator.FromHtml(json["rowBackColor"]?.ToString() ?? "#FFFFFF") is Color rowBackColor)
            RowBackColor = rowBackColor;
        if (ColorTranslator.FromHtml(json["alternateRowBackColor"]?.ToString() ?? "#F0F0F0") is Color alternateRowBackColor)
            AlternateRowBackColor = alternateRowBackColor;
        if (ColorTranslator.FromHtml(json["foreColor"]?.ToString() ?? "#000000") is Color foreColor)
            ForeColor = foreColor;
        if (ColorTranslator.FromHtml(json["borderColor"]?.ToString() ?? "#808080") is Color borderColor)
            BorderColor = borderColor;

        ShowHeader = json["showHeader"]?.ToObject<bool>() ?? true;
        ShowGrid = json["showGrid"]?.ToObject<bool>() ?? true;
        AlternateRows = json["alternateRows"]?.ToObject<bool>() ?? true;

        DataSource = json["dataSource"]?.ToString() ?? "TagData";
        TagTableName = json["tagTableName"]?.ToString() ?? string.Empty;
        HistorianTagName = json["historianTagName"]?.ToString() ?? string.Empty;
        HistorianTimeRangeMinutes = json["historianTimeRangeMinutes"]?.ToObject<int>() ?? 60;

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
