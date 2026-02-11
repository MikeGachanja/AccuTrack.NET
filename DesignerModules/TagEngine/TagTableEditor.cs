using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Designer.Modules.TagEngine;

namespace Designer.Modules.TagEngine;

/// <summary>
/// Editor for tag tables.
/// </summary>
public partial class TagTableEditor : UserControl
{
    private DataGridView _tagsGrid;
    private List<TagTable> _tagTables = new List<TagTable>();
    private bool _modified;
    private bool _isAllTagsView;

    // Available data types for dropdown
    private static readonly string[] DataTypes = new[]
    {
        "Bit", "Byte", "Word", "DWord",
        "Int8", "UInt8", "Int16", "UInt16", "Int32", "UInt32",
        "Float32", "Float64",
        "String8", "String16"
    };

    public event EventHandler<bool>? ModifiedChanged;

    public TagTableEditor()
    {
        InitializeComponent();
    }

    public bool IsModified => _modified;

    private void InitializeComponent()
    {
        _tagsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EditMode = DataGridViewEditMode.EditOnEnter
        };

        // Add columns
        _tagsGrid.Columns.Add("Name", "Name");
        
        // Add TagTable column (only shown in All Tags view)
        _tagsGrid.Columns.Add("TagTable", "Tag Table");
        
        // Add DataType as ComboBox column (Excel-style dropdown)
        var dataTypeColumn = new DataGridViewComboBoxColumn
        {
            Name = "DataType",
            HeaderText = "Data Type",
            Width = 100,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Flat
        };
        dataTypeColumn.Items.AddRange(DataTypes);
        _tagsGrid.Columns.Add(dataTypeColumn);
        
        _tagsGrid.Columns.Add("Address", "Address");
        _tagsGrid.Columns.Add("Value", "Value");
        _tagsGrid.Columns.Add("Description", "Description");
        _tagsGrid.Columns.Add("Unit", "Unit");
        _tagsGrid.Columns.Add("MinValue", "Min");
        _tagsGrid.Columns.Add("MaxValue", "Max");
        _tagsGrid.Columns.Add("ReadOnly", "Read Only");

        // Set column widths
        _tagsGrid.Columns["Name"].Width = 150;
        _tagsGrid.Columns["TagTable"].Width = 120;
        _tagsGrid.Columns["Address"].Width = 120;
        _tagsGrid.Columns["Value"].Width = 100;
        _tagsGrid.Columns["Description"].Width = 200;
        _tagsGrid.Columns["Unit"].Width = 80;
        _tagsGrid.Columns["MinValue"].Width = 80;
        _tagsGrid.Columns["MaxValue"].Width = 80;
        _tagsGrid.Columns["ReadOnly"].Width = 80;
        
        // Hide TagTable column initially (will be shown in All Tags view)
        _tagsGrid.Columns["TagTable"].Visible = false;

        // Make ReadOnly column a checkbox
        var readOnlyColumn = _tagsGrid.Columns["ReadOnly"] as DataGridViewCheckBoxColumn;
        if (readOnlyColumn == null)
        {
            _tagsGrid.Columns.Remove("ReadOnly");
            readOnlyColumn = new DataGridViewCheckBoxColumn
            {
                Name = "ReadOnly",
                HeaderText = "Read Only"
            };
            _tagsGrid.Columns.Add(readOnlyColumn);
        }

        // Handle cell value changes and formatting
        _tagsGrid.CellValueChanged += OnCellValueChanged;
        _tagsGrid.CellFormatting += OnCellFormatting;
        _tagsGrid.RowsAdded += OnRowsAdded;
        _tagsGrid.RowsRemoved += OnRowsRemoved;
        _tagsGrid.UserDeletingRow += OnUserDeletingRow;

        Controls.Add(_tagsGrid);
    }

    /// <summary>
    /// Sets a single tag table to edit.
    /// </summary>
    public void SetTagTable(TagTable table)
    {
        _tagTables.Clear();
        _isAllTagsView = false;
        if (table != null)
        {
            _tagTables.Add(table);
            table.Modified += (s, e) => SetModified(true);
        }
        LoadTags();
    }

    /// <summary>
    /// Sets multiple tag tables to edit (for "All Tags" view).
    /// </summary>
    public void SetTagTables(List<TagTable> tables)
    {
        _tagTables = tables ?? new List<TagTable>();
        _isAllTagsView = _tagTables.Count > 1;
        foreach (var table in _tagTables)
        {
            table.Modified += (s, e) => SetModified(true);
        }
        LoadTags();
    }

    /// <summary>
    /// Gets the tag tables being edited (for Save All).
    /// </summary>
    public List<TagTable> GetTagTables() => new List<TagTable>(_tagTables);
    
    /// <summary>
    /// Saves all tag tables to their files.
    /// </summary>
    public bool SaveAll()
    {
        bool allSaved = true;
        foreach (var table in _tagTables)
        {
            if (!string.IsNullOrEmpty(table.FilePath))
            {
                if (!table.SaveToFile(table.FilePath))
                {
                    allSaved = false;
                    System.Diagnostics.Debug.WriteLine($"Failed to save tag table {table.Name} to {table.FilePath}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Tag table {table.Name} has no FilePath set");
                allSaved = false;
            }
        }
        if (allSaved)
        {
            _modified = false;
            ModifiedChanged?.Invoke(this, false);
        }
        return allSaved;
    }

    /// <summary>
    /// Loads tags from tag tables into the grid.
    /// </summary>
    private void LoadTags()
    {
        _tagsGrid.Rows.Clear();
        _modified = false;

        // Show/hide TagTable column based on view mode
        bool showTagTableColumn = _isAllTagsView && _tagTables.Count > 1;
        if (_tagsGrid.Columns.Contains("TagTable"))
        {
            _tagsGrid.Columns["TagTable"].Visible = showTagTableColumn;
        }

        foreach (var table in _tagTables)
        {
            foreach (var tag in table.GetTags())
            {
                // Normalize data type to match ComboBox items
                var normalizedDataType = NormalizeDataType(tag.DataType);
                
                // Add row with TagTable column if in All Tags view
                int rowIndex;
                if (showTagTableColumn)
                {
                    rowIndex = _tagsGrid.Rows.Add(
                        tag.Name,
                        table.Name,  // TagTable name
                        normalizedDataType,
                        tag.Address,
                        tag.Value?.ToString() ?? "",
                        tag.Description,
                        tag.Unit,
                        tag.MinValue,
                        tag.MaxValue,
                        tag.ReadOnly
                    );
                }
                else
                {
                    rowIndex = _tagsGrid.Rows.Add(
                        tag.Name,
                        normalizedDataType,
                        tag.Address,
                        tag.Value?.ToString() ?? "",
                        tag.Description,
                        tag.Unit,
                        tag.MinValue,
                        tag.MaxValue,
                        tag.ReadOnly
                    );
                }

                // Update tag with normalized data type
                tag.DataType = normalizedDataType;
                
                _tagsGrid.Rows[rowIndex].Tag = new TagRowData { Tag = tag, TagTable = table };
            }
        }
        
        // Refresh formatting after loading
        _tagsGrid.Invalidate();
    }

    /// <summary>
    /// Normalizes data type string to match ComboBox items.
    /// </summary>
    private string NormalizeDataType(string dataType)
    {
        if (string.IsNullOrEmpty(dataType))
            return "Float32";

        var normalized = dataType.Trim();
        
        // Map common variations to standard types
        return normalized.ToLower() switch
        {
            "float" or "double" => "Float32",
            "float64" => "Float64",
            "int" or "integer" => "Int32",
            "bool" or "boolean" => "Bit",
            "string" => "String8",
            _ => DataTypes.Contains(normalized) ? normalized : "Float32"
        };
    }

    /// <summary>
    /// Handles cell formatting to highlight mismatches.
    /// </summary>
    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _tagsGrid.Rows[e.RowIndex];
        var rowData = row.Tag as TagRowData;
        var tag = rowData?.Tag;

        if (tag == null)
            return;

        var columnName = _tagsGrid.Columns[e.ColumnIndex].Name;

        // Highlight Address column if it doesn't match DataType
        if (columnName == "Address")
        {
            if (!IsAddressMatchingDataType(tag.Address, tag.DataType))
            {
                e.CellStyle.BackColor = Color.LightCoral; // Red highlight for mismatch
                e.CellStyle.ForeColor = Color.DarkRed;
            }
            else
            {
                e.CellStyle.BackColor = _tagsGrid.DefaultCellStyle.BackColor;
                e.CellStyle.ForeColor = _tagsGrid.DefaultCellStyle.ForeColor;
            }
        }
        // Highlight DataType column if it doesn't match Address
        else if (columnName == "DataType")
        {
            if (!IsAddressMatchingDataType(tag.Address, tag.DataType))
            {
                e.CellStyle.BackColor = Color.LightCoral; // Red highlight for mismatch
                e.CellStyle.ForeColor = Color.DarkRed;
            }
            else
            {
                e.CellStyle.BackColor = _tagsGrid.DefaultCellStyle.BackColor;
                e.CellStyle.ForeColor = _tagsGrid.DefaultCellStyle.ForeColor;
            }
        }
    }

    /// <summary>
    /// Handles cell value changes.
    /// </summary>
    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _tagsGrid.Rows[e.RowIndex];
        var rowData = row.Tag as TagRowData;

        if (rowData?.Tag != null)
        {
            var tag = rowData.Tag;
            var columnName = _tagsGrid.Columns[e.ColumnIndex].Name;

            try
            {
                switch (columnName)
                {
                    case "Name":
                        tag.Name = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
                        break;
                    case "TagTable":
                        // TagTable column is read-only in All Tags view
                        break;
                    case "DataType":
                        var newDataType = row.Cells[e.ColumnIndex].Value?.ToString() ?? "Float32";
                        tag.DataType = newDataType;
                        
                        // If address doesn't match new data type, suggest a new address
                        if (!IsAddressMatchingDataType(tag.Address, newDataType))
                        {
                            var suggestedAddress = FindNextAvailableAddress(newDataType);
                            if (!string.IsNullOrEmpty(suggestedAddress))
                            {
                                tag.Address = suggestedAddress;
                                row.Cells["Address"].Value = suggestedAddress;
                            }
                        }
                        break;
                    case "Address":
                        var newAddress = row.Cells[e.ColumnIndex].Value?.ToString()?.ToUpper() ?? string.Empty;
                        tag.Address = newAddress;
                        
                        // If address doesn't match data type, adjust data type to match address
                        if (!IsAddressMatchingDataType(newAddress, tag.DataType))
                        {
                            var suggestedDataType = GetDataTypeFromAddress(newAddress);
                            if (!string.IsNullOrEmpty(suggestedDataType))
                            {
                                tag.DataType = suggestedDataType;
                                row.Cells["DataType"].Value = suggestedDataType;
                            }
                        }
                        break;
                    case "Value":
                        var valueStr = row.Cells[e.ColumnIndex].Value?.ToString();
                        tag.Value = ParseValue(valueStr, tag.DataType);
                        break;
                    case "Description":
                        tag.Description = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
                        break;
                    case "Unit":
                        tag.Unit = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
                        break;
                    case "MinValue":
                        if (double.TryParse(row.Cells[e.ColumnIndex].Value?.ToString(), out double min))
                            tag.MinValue = min;
                        break;
                    case "MaxValue":
                        if (double.TryParse(row.Cells[e.ColumnIndex].Value?.ToString(), out double max))
                            tag.MaxValue = max;
                        break;
                    case "ReadOnly":
                        if (row.Cells[e.ColumnIndex].Value is bool readOnly)
                            tag.ReadOnly = readOnly;
                        break;
                }
                
                // Save the tag table to file when modified
                // Note: We save immediately when FilePath is set, otherwise it will be saved via SaveAll/SaveTagTableFromEditor
                if (rowData.TagTable != null && !string.IsNullOrEmpty(rowData.TagTable.FilePath))
                {
                    try
                    {
                        bool saved = rowData.TagTable.SaveToFile(rowData.TagTable.FilePath);
                        if (!saved)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to save tag table {rowData.TagTable.Name} to {rowData.TagTable.FilePath}");
                            // Don't show error message on every keystroke, but log it
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Exception saving tag table {rowData.TagTable.Name}: {ex.Message}");
                    }
                }

                // Refresh formatting after value change
                _tagsGrid.InvalidateRow(e.RowIndex);
                SetModified(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating tag: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else if (e.RowIndex == _tagsGrid.Rows.Count - 1 && _tagsGrid.Rows[e.RowIndex].IsNewRow)
        {
            // New row - create new tag
            CreateNewTagFromRow(row);
        }
    }

    /// <summary>
    /// Handles rows added event.
    /// </summary>
    private void OnRowsAdded(object? sender, DataGridViewRowsAddedEventArgs e)
    {
        SetModified(true);
    }

    /// <summary>
    /// Handles rows removed event.
    /// </summary>
    private void OnRowsRemoved(object? sender, DataGridViewRowsRemovedEventArgs e)
    {
        SetModified(true);
    }

    /// <summary>
    /// Handles user deleting row.
    /// </summary>
    private void OnUserDeletingRow(object? sender, DataGridViewRowCancelEventArgs e)
    {
        var rowData = e.Row.Tag as TagRowData;
        if (rowData?.Tag != null && rowData.TagTable != null)
        {
            rowData.TagTable.RemoveTag(rowData.Tag);
            
            // Save the tag table to file after deletion
            if (!string.IsNullOrEmpty(rowData.TagTable.FilePath))
            {
                bool saved = rowData.TagTable.SaveToFile(rowData.TagTable.FilePath);
                if (!saved)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save tag table {rowData.TagTable.Name} after deletion");
                }
            }
        }
    }

    /// <summary>
    /// Creates a new tag from a grid row.
    /// </summary>
    private void CreateNewTagFromRow(DataGridViewRow row)
    {
        if (_tagTables.Count == 0)
            return;

        var dataType = row.Cells["DataType"].Value?.ToString() ?? "Float32";
        var address = row.Cells["Address"].Value?.ToString();
        
        // If no address provided, find next available one
        if (string.IsNullOrEmpty(address))
        {
            address = FindNextAvailableAddress(dataType);
        }

        var tag = new Tag
        {
            Name = row.Cells["Name"].Value?.ToString() ?? "NewTag",
            DataType = dataType,
            Address = address ?? string.Empty,
            Description = row.Cells["Description"].Value?.ToString() ?? string.Empty,
            Unit = row.Cells["Unit"].Value?.ToString() ?? string.Empty
        };
        
        // Update the row with the assigned address
        row.Cells["Address"].Value = address;

        if (double.TryParse(row.Cells["MinValue"].Value?.ToString(), out double min))
            tag.MinValue = min;
        if (double.TryParse(row.Cells["MaxValue"].Value?.ToString(), out double max))
            tag.MaxValue = max;
        if (row.Cells["ReadOnly"].Value is bool readOnly)
            tag.ReadOnly = readOnly;

        var valueStr = row.Cells["Value"].Value?.ToString();
        tag.Value = ParseValue(valueStr, tag.DataType);

        // Add to first tag table (or appropriate table based on context)
        var targetTable = _tagTables[0];
        targetTable.AddTag(tag);
        row.Tag = new TagRowData { Tag = tag, TagTable = targetTable };
        
        // Update TagTable column if visible
        if (_isAllTagsView && _tagsGrid.Columns["TagTable"].Visible)
        {
            row.Cells["TagTable"].Value = targetTable.Name;
        }
        
        // Save the tag table to file
        if (!string.IsNullOrEmpty(targetTable.FilePath))
        {
            bool saved = targetTable.SaveToFile(targetTable.FilePath);
            if (!saved)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save tag table {targetTable.Name} to {targetTable.FilePath}");
            }
        }
    }

    /// <summary>
    /// Parses a value string based on data type.
    /// </summary>
    private object? ParseValue(string? valueStr, string dataType)
    {
        if (string.IsNullOrEmpty(valueStr))
            return null;

        try
        {
            return dataType.ToLower() switch
            {
                "int" or "integer" => int.Parse(valueStr),
                "float" or "double" => double.Parse(valueStr),
                "bool" or "boolean" => bool.Parse(valueStr),
                "string" => valueStr,
                _ => valueStr
            };
        }
        catch
        {
            return valueStr;
        }
    }

    /// <summary>
    /// Checks if an address matches the given data type.
    /// </summary>
    private bool IsAddressMatchingDataType(string address, string dataType)
    {
        if (string.IsNullOrEmpty(address))
            return true; // Empty address is valid

        address = address.ToUpper().Trim();

        // Bit addresses (M0.0, I0.0, O0.0, etc.)
        if (address.Contains('.'))
        {
            return dataType == "Bit";
        }

        // Word addresses (MW0, MW2, IW0, OW0, etc.)
        if (address.StartsWith("MW") || address.StartsWith("IW") || address.StartsWith("OW"))
        {
            return dataType == "Word" || dataType == "Int16" || dataType == "UInt16";
        }

        // Double word addresses (MD0, MD4, ID0, OD0, etc.)
        if (address.StartsWith("MD") || address.StartsWith("ID") || address.StartsWith("OD"))
        {
            return dataType == "DWord" || dataType == "Float32" || 
                   dataType == "Int32" || dataType == "UInt32";
        }

        // Byte addresses (MB0, IB0, OB0, M0, I0, O0, etc.)
        if (address.StartsWith("MB") || address.StartsWith("IB") || address.StartsWith("OB") ||
            Regex.IsMatch(address, @"^[MIO]\d+$"))
        {
            return dataType == "Byte" || dataType == "Int8" || dataType == "UInt8" ||
                   dataType == "String8" || dataType == "String16";
        }

        // Default: allow any address format
        return true;
    }

    /// <summary>
    /// Gets the appropriate data type from an address format.
    /// </summary>
    private string GetDataTypeFromAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return "Float32"; // Default

        address = address.ToUpper().Trim();

        // Bit addresses
        if (address.Contains('.'))
        {
            return "Bit";
        }

        // Word addresses
        if (address.StartsWith("MW") || address.StartsWith("IW") || address.StartsWith("OW"))
        {
            return "Word";
        }

        // Double word addresses
        if (address.StartsWith("MD") || address.StartsWith("ID") || address.StartsWith("OD"))
        {
            return "Float32";
        }

        // Byte addresses
        if (address.StartsWith("MB") || address.StartsWith("IB") || address.StartsWith("OB") ||
            Regex.IsMatch(address, @"^[MIO]\d+$"))
        {
            return "Byte";
        }

        return "Float32"; // Default
    }

    /// <summary>
    /// Finds the next available address for a given data type.
    /// </summary>
    private string FindNextAvailableAddress(string dataType)
    {
        // Collect all used addresses
        var usedAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in _tagTables)
        {
            foreach (var tag in table.GetTags())
            {
                if (!string.IsNullOrEmpty(tag.Address))
                {
                    usedAddresses.Add(tag.Address.ToUpper());
                }
            }
        }

        // Also check addresses in the grid
        foreach (DataGridViewRow row in _tagsGrid.Rows)
        {
            if (row.IsNewRow) continue;
            var addr = row.Cells["Address"].Value?.ToString();
            if (!string.IsNullOrEmpty(addr))
            {
                usedAddresses.Add(addr.ToUpper());
            }
        }

        // Find next available address based on data type
        int offset = 0;
        string prefix = "M"; // Default to Memory

        while (true)
        {
            string address;
            
            if (dataType == "Bit")
            {
                int byteOffset = offset / 8;
                int bitOffset = offset % 8;
                address = $"{prefix}{byteOffset}.{bitOffset}";
                offset++;
            }
            else if (dataType == "Word" || dataType == "Int16" || dataType == "UInt16")
            {
                address = $"{prefix}W{offset}";
                offset += 2; // Words are 2 bytes
            }
            else if (dataType == "DWord" || dataType == "Float32" || 
                     dataType == "Int32" || dataType == "UInt32")
            {
                address = $"{prefix}D{offset}";
                offset += 4; // DWords are 4 bytes
            }
            else // Byte, Int8, UInt8, String8, String16
            {
                address = $"{prefix}B{offset}";
                offset++;
            }

            if (!usedAddresses.Contains(address))
            {
                return address;
            }

            // Prevent infinite loop
            if (offset > 10000)
            {
                return $"{prefix}W0"; // Fallback
            }
        }
    }

    /// <summary>
    /// Sets the modified state.
    /// </summary>
    private void SetModified(bool modified)
    {
        if (_modified != modified)
        {
            _modified = modified;
            ModifiedChanged?.Invoke(this, modified);
        }
    }
}

/// <summary>
/// Data structure for tag row information.
/// </summary>
internal class TagRowData
{
    public Tag? Tag { get; set; }
    public TagTable? TagTable { get; set; }
}
