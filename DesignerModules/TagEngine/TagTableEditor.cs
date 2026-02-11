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

        // Set equal FillWeight for all columns to make them equal size
        foreach (DataGridViewColumn column in _tagsGrid.Columns)
        {
            column.FillWeight = 100;
        }
        
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
        _tagsGrid.CellEndEdit += OnCellEndEdit;
        _tagsGrid.CellFormatting += OnCellFormatting;
        _tagsGrid.RowsAdded += OnRowsAdded;
        _tagsGrid.RowsRemoved += OnRowsRemoved;
        _tagsGrid.UserDeletingRow += OnUserDeletingRow;
        _tagsGrid.DefaultValuesNeeded += OnDefaultValuesNeeded;
        _tagsGrid.RowValidated += OnRowValidated;

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
                        null, // Set DataType after row is created to avoid ComboBox validation issues
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
                        null, // Set DataType after row is created to avoid ComboBox validation issues
                        tag.Address,
                        tag.Value?.ToString() ?? "",
                        tag.Description,
                        tag.Unit,
                        tag.MinValue,
                        tag.MaxValue,
                        tag.ReadOnly
                    );
                }

                // Set DataType cell value after row is created to ensure ComboBox validation passes
                var dataTypeCell = _tagsGrid.Rows[rowIndex].Cells["DataType"] as DataGridViewComboBoxCell;
                if (dataTypeCell != null)
                {
                    // Ensure the value exists in the ComboBox items
                    if (dataTypeCell.Items.Contains(normalizedDataType))
                    {
                        dataTypeCell.Value = normalizedDataType;
                    }
                    else
                    {
                        // If value doesn't exist, use the first available item or default
                        dataTypeCell.Value = dataTypeCell.Items.Count > 0 ? dataTypeCell.Items[0] : "Bit";
                        System.Diagnostics.Debug.WriteLine($"Warning: DataType '{normalizedDataType}' not found in ComboBox items for tag '{tag.Name}'. Using default.");
                    }
                }
                else
                {
                    // Fallback: set value directly if cell is not a ComboBox cell
                    _tagsGrid.Rows[rowIndex].Cells["DataType"].Value = normalizedDataType;
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
            return "Bit"; // Changed default to Bit to match new default

        var normalized = dataType.Trim();
        
        // Map common variations to standard types
        string result = normalized.ToLower() switch
        {
            "float" or "double" => "Float32",
            "float64" => "Float64",
            "int" or "integer" => "Int32",
            "bool" or "boolean" => "Bit",
            "string" => "String8",
            _ => DataTypes.Contains(normalized) ? normalized : "Bit" // Changed default to Bit
        };
        
        // Ensure the result is in the DataTypes array (case-insensitive check)
        if (!DataTypes.Contains(result))
        {
            // Find case-insensitive match
            var match = DataTypes.FirstOrDefault(dt => dt.Equals(result, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                result = match;
            }
            else
            {
                result = "Bit"; // Fallback to Bit if no match found
            }
        }
        
        return result;
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
                        var newDataType = row.Cells[e.ColumnIndex].Value?.ToString() ?? "Bit";
                        tag.DataType = newDataType;
                        
                        // If address doesn't match new data type, suggest a new address
                        if (!IsAddressMatchingDataType(tag.Address, newDataType))
                        {
                            // Extract prefix from existing address (I, Q, or M), default to M
                            string prefix = "M";
                            if (!string.IsNullOrEmpty(tag.Address))
                            {
                                var upperAddr = tag.Address.ToUpper();
                                if (upperAddr.StartsWith("I"))
                                    prefix = "I";
                                else if (upperAddr.StartsWith("Q"))
                                    prefix = "Q";
                                else if (upperAddr.StartsWith("M"))
                                    prefix = "M";
                            }
                            
                            var suggestedAddress = FindNextAvailableAddress(newDataType, prefix);
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
    }

    /// <summary>
    /// Handles cell edit end - used for auto-populating address/data type on new rows and creating tags.
    /// </summary>
    private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _tagsGrid.Rows[e.RowIndex];
        var rowData = row.Tag as TagRowData;
        
        // Only auto-populate for new rows that don't have a tag yet
        bool isNewRow = row.IsNewRow || (rowData?.Tag == null && e.RowIndex == _tagsGrid.Rows.Count - 1);
        
        if (isNewRow)
        {
            var columnName = _tagsGrid.Columns[e.ColumnIndex].Name;

            if (columnName == "DataType")
            {
                var newDataType = row.Cells["DataType"].Value?.ToString() ?? "Bit";
                var currentAddress = row.Cells["Address"].Value?.ToString();

                // Auto-populate address if empty and we have a valid data type
                if (string.IsNullOrWhiteSpace(currentAddress) && !string.IsNullOrWhiteSpace(newDataType))
                {
                    // Default to Memory (M) prefix for new tags
                    var suggestedAddress = FindNextAvailableAddress(newDataType, "M");
                    if (!string.IsNullOrEmpty(suggestedAddress))
                    {
                        // Temporarily remove event handlers to prevent recursion
                        _tagsGrid.CellValueChanged -= OnCellValueChanged;
                        _tagsGrid.CellEndEdit -= OnCellEndEdit;
                        try
                        {
                            row.Cells["Address"].Value = suggestedAddress;
                        }
                        finally
                        {
                            // Re-add event handlers
                            _tagsGrid.CellValueChanged += OnCellValueChanged;
                            _tagsGrid.CellEndEdit += OnCellEndEdit;
                        }
                    }
                }
            }
            else if (columnName == "Address")
            {
                var newAddress = row.Cells["Address"].Value?.ToString()?.ToUpper() ?? string.Empty;
                var currentDataType = row.Cells["DataType"].Value?.ToString();

                // If the user typed an address but left data type empty, infer data type from address
                if (!string.IsNullOrEmpty(newAddress) && string.IsNullOrWhiteSpace(currentDataType))
                {
                    var inferredType = GetDataTypeFromAddress(newAddress);
                    if (!string.IsNullOrEmpty(inferredType))
                    {
                        // Temporarily remove event handlers to prevent recursion
                        _tagsGrid.CellValueChanged -= OnCellValueChanged;
                        _tagsGrid.CellEndEdit -= OnCellEndEdit;
                        try
                        {
                            row.Cells["DataType"].Value = inferredType;
                        }
                        finally
                        {
                            // Re-add event handlers
                            _tagsGrid.CellValueChanged += OnCellValueChanged;
                            _tagsGrid.CellEndEdit += OnCellEndEdit;
                        }
                    }
                }
            }

            // Try to create tag if row has enough data (name is required)
            // Check if this is a committed row (not the new row placeholder) that doesn't have a tag yet
            if (rowData?.Tag == null)
            {
                var name = row.Cells["Name"].Value?.ToString();
                // Create tag if we have a name and the row is not the new row placeholder
                // (new row placeholder is always the last row and has IsNewRow = true)
                if (!string.IsNullOrWhiteSpace(name) && 
                    (!row.IsNewRow || e.RowIndex < _tagsGrid.Rows.Count - 1))
                {
                    CreateNewTagFromRow(row);
                }
            }
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
    /// Handles row validated event - creates tag when new row is committed.
    /// </summary>
    private void OnRowValidated(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _tagsGrid.Rows.Count)
            return;

        var row = _tagsGrid.Rows[e.RowIndex];
        var rowData = row.Tag as TagRowData;
        
        // If this row doesn't have a tag yet and is not the new row placeholder, create one
        if (rowData?.Tag == null && !row.IsNewRow)
        {
            // Check if row has at least a name before creating tag
            var name = row.Cells["Name"].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                CreateNewTagFromRow(row);
            }
        }
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
    /// Handles default values needed for new row.
    /// </summary>
    private void OnDefaultValuesNeeded(object? sender, DataGridViewRowEventArgs e)
    {
        // Set default values for new row
        var row = e.Row;
        
        // Default name: Tag_1, Tag_2, etc.
        var defaultName = GetNextAvailableTagName();
        row.Cells["Name"].Value = defaultName;
        
        // Default data type: Bit (Boolean)
        row.Cells["DataType"].Value = "Bit";
        
        // Default address: M0.0 for Bit type (Memory)
        var defaultAddress = FindNextAvailableAddress("Bit", "M");
        row.Cells["Address"].Value = defaultAddress;
        
        // Default ReadOnly: false
        row.Cells["ReadOnly"].Value = false;
    }

    /// <summary>
    /// Gets the next available tag name in format Tag_1, Tag_2, etc.
    /// </summary>
    private string GetNextAvailableTagName()
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Collect all existing tag names
        foreach (var table in _tagTables)
        {
            foreach (var tag in table.GetTags())
            {
                if (!string.IsNullOrEmpty(tag.Name))
                {
                    usedNames.Add(tag.Name);
                }
            }
        }
        
        // Also check names in the grid
        foreach (DataGridViewRow row in _tagsGrid.Rows)
        {
            if (row.IsNewRow) continue;
            var name = row.Cells["Name"].Value?.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                usedNames.Add(name);
            }
        }
        
        // Find next available Tag_N
        int counter = 1;
        while (true)
        {
            string candidateName = $"Tag_{counter}";
            if (!usedNames.Contains(candidateName))
            {
                return candidateName;
            }
            counter++;
            
            // Prevent infinite loop
            if (counter > 10000)
            {
                return $"Tag_{counter}";
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

        var dataType = row.Cells["DataType"].Value?.ToString() ?? "Bit";
        var address = row.Cells["Address"].Value?.ToString();
        
        // If no address provided, find next available one (default to Memory/M)
        if (string.IsNullOrEmpty(address))
        {
            address = FindNextAvailableAddress(dataType, "M");
        }

        var tag = new Tag
        {
            Name = row.Cells["Name"].Value?.ToString() ?? GetNextAvailableTagName(),
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
            return "Bit"; // Default to Bit (Boolean)

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
    /// <param name="dataType">The data type (Bit, Word, etc.)</param>
    /// <param name="addressPrefix">Address prefix: "I" for Input, "Q" for Output, "M" for Memory (default)</param>
    private string FindNextAvailableAddress(string dataType, string addressPrefix = "M")
    {
        // Validate and normalize prefix
        addressPrefix = addressPrefix?.ToUpper() ?? "M";
        if (addressPrefix != "I" && addressPrefix != "Q" && addressPrefix != "M")
        {
            addressPrefix = "M"; // Default to Memory if invalid
        }

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

        while (true)
        {
            string address;
            
            if (dataType == "Bit")
            {
                int byteOffset = offset / 8;
                int bitOffset = offset % 8;
                address = $"{addressPrefix}{byteOffset}.{bitOffset}";
                offset++;
            }
            else if (dataType == "Word" || dataType == "Int16" || dataType == "UInt16")
            {
                address = $"{addressPrefix}W{offset}";
                offset += 2; // Words are 2 bytes
            }
            else if (dataType == "DWord" || dataType == "Float32" || 
                     dataType == "Int32" || dataType == "UInt32")
            {
                address = $"{addressPrefix}D{offset}";
                offset += 4; // DWords are 4 bytes
            }
            else // Byte, Int8, UInt8, String8, String16
            {
                address = $"{addressPrefix}B{offset}";
                offset++;
            }

            if (!usedAddresses.Contains(address))
            {
                return address;
            }

            // Prevent infinite loop
            if (offset > 10000)
            {
                return $"{addressPrefix}W0"; // Fallback
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
