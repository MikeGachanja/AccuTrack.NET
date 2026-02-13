using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Communication;
using Designer.Modules.Project;

namespace Designer.Modules.Communication;

/// <summary>
/// Editor for communication modules configuration.
/// </summary>
public partial class CommunicationModuleEditor : UserControl
{
    private TabControl _mainTabs;
    private DataGridView _modulesGrid;
    private DataGridView _tagMappingsGrid;
    private CommunicationModules? _communicationModules;
    private ScadaProject? _scadaProject;
    private ProjectManager? _projectManager;
    private string? _scadaName;
    private bool _isModified = false;
    private CommunicationModule? _selectedModule;

    public bool IsModified => _isModified;

    public CommunicationModuleEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };

        // Toolbar
        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addButton = new ToolStripButton("Add Module");
        addButton.Click += (s, e) => AddModule();
        toolbar.Items.Add(addButton);
        var removeButton = new ToolStripButton("Remove Module");
        removeButton.Click += (s, e) => RemoveSelectedModule();
        toolbar.Items.Add(removeButton);
        var configureButton = new ToolStripButton("Configure...");
        configureButton.Click += (s, e) => ConfigureSelectedModule();
        toolbar.Items.Add(configureButton);

        // Modules grid
        _modulesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _modulesGrid.Columns.Add("Name", "Name");
        _modulesGrid.Columns.Add("Type", "Type");
        _modulesGrid.Columns.Add("Enabled", "Enabled");
        _modulesGrid.Columns.Add("Description", "Description");

        var typeColumn = new DataGridViewComboBoxColumn
        {
            Name = "Type",
            HeaderText = "Type",
            DataPropertyName = "Type"
        };
        typeColumn.Items.AddRange(new[] { "ModbusTCP", "ModbusRTU", "OPCUA", "EthernetIP", "BACnet", "DNP3" });
        _modulesGrid.Columns.Remove("Type");
        _modulesGrid.Columns.Insert(1, typeColumn);

        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _modulesGrid.Columns.Remove("Enabled");
        _modulesGrid.Columns.Insert(2, enabledColumn);

        _modulesGrid.CellValueChanged += OnCellValueChanged;
        _modulesGrid.SelectionChanged += OnModuleSelectionChanged;
        _modulesGrid.DoubleClick += (s, e) => ConfigureSelectedModule();

        // Create tab control for modules and tag mappings
        _mainTabs = new TabControl { Dock = DockStyle.Fill };
        _mainTabs.TabPages.Add("Modules");
        _mainTabs.TabPages.Add("Tag Mappings");

        // Modules tab
        var modulesPanel = new Panel { Dock = DockStyle.Fill };
        modulesPanel.Controls.Add(_modulesGrid);
        _mainTabs.TabPages[0].Controls.Add(modulesPanel);

        // Tag mappings tab
        var mappingsPanel = new Panel { Dock = DockStyle.Fill };
        mappingsPanel.Controls.Add(CreateTagMappingsPanel());
        _mainTabs.TabPages[1].Controls.Add(mappingsPanel);

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_mainTabs, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }
    
    /// <summary>
    /// Creates the tag mappings panel with grid and buttons.
    /// </summary>
    private Panel CreateTagMappingsPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(5)
        };

        // Toolbar for tag mappings
        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addMappingButton = new ToolStripButton("Add Mapping");
        addMappingButton.Click += (s, e) => AddTagMapping();
        toolbar.Items.Add(addMappingButton);
        var removeMappingButton = new ToolStripButton("Remove Mapping");
        removeMappingButton.Click += (s, e) => RemoveSelectedTagMapping();
        toolbar.Items.Add(removeMappingButton);
        var autoMapButton = new ToolStripButton("Auto-Map");
        autoMapButton.Click += (s, e) => AutoMapTagMappings();
        toolbar.Items.Add(autoMapButton);
        var validateButton = new ToolStripButton("Validate");
        validateButton.Click += (s, e) => ValidateTagMappings();
        toolbar.Items.Add(validateButton);

        // Tag mappings grid
        _tagMappingsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _tagMappingsGrid.Columns.Add("TagName", "Tag Name");
        _tagMappingsGrid.Columns.Add("Address", "Address");
        _tagMappingsGrid.Columns.Add("DataType", "Data Type");
        _tagMappingsGrid.Columns.Add("Enabled", "Enabled");
        _tagMappingsGrid.Columns.Add("Description", "Description");

        var dataTypeColumn = new DataGridViewComboBoxColumn
        {
            Name = "DataType",
            HeaderText = "Data Type",
            DataPropertyName = "DataType"
        };
        dataTypeColumn.Items.AddRange(new[] { "Bit", "Byte", "Word", "DWord", "Int16", "UInt16", "Int32", "UInt32", "Float32", "Float64" });
        _tagMappingsGrid.Columns.Remove("DataType");
        _tagMappingsGrid.Columns.Insert(2, dataTypeColumn);

        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _tagMappingsGrid.Columns.Remove("Enabled");
        _tagMappingsGrid.Columns.Insert(3, enabledColumn);

        _tagMappingsGrid.CellValueChanged += OnTagMappingCellValueChanged;

        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(_tagMappingsGrid, 0, 1);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(layout);
        return panel;
    }

    public void SetCommunicationModules(CommunicationModules modules)
    {
        _communicationModules = modules;
        LoadModules();
        _isModified = false;
    }

    public void SetScadaProject(ScadaProject project)
    {
        _scadaProject = project;
    }

    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;
    }

    public void SetScadaName(string scadaName)
    {
        _scadaName = scadaName;
    }

    private void LoadModules()
    {
        _modulesGrid.Rows.Clear();

        if (_communicationModules == null)
            _communicationModules = new CommunicationModules();

        foreach (var module in _communicationModules.Modules)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_modulesGrid);
            row.Cells[0].Value = module.Name;
            row.Cells[1].Value = module.Type;
            row.Cells[2].Value = module.Enabled;
            row.Cells[3].Value = module.Description;
            row.Tag = module;
            _modulesGrid.Rows.Add(row);
        }
    }

    private void AddModule()
    {
        if (_communicationModules == null)
            _communicationModules = new CommunicationModules();

        var newModule = new CommunicationModule
        {
            Name = $"Module{_communicationModules.Modules.Count + 1}",
            Type = "ModbusTCP",
            Enabled = true
        };

        _communicationModules.Modules.Add(newModule);
        LoadModules();
        _isModified = true;
    }

    private void RemoveSelectedModule()
    {
        if (_modulesGrid.SelectedRows.Count == 0 || _communicationModules == null)
            return;

        var selectedRow = _modulesGrid.SelectedRows[0];
        if (selectedRow.Tag is CommunicationModule module)
        {
            _communicationModules.Modules.Remove(module);
            LoadModules();
            _isModified = true;
        }
    }

    private void ConfigureSelectedModule()
    {
        if (_modulesGrid.SelectedRows.Count == 0)
            return;

        var selectedRow = _modulesGrid.SelectedRows[0];
        if (selectedRow.Tag is not CommunicationModule module)
            return;

        // Open appropriate configuration dialog based on module type
        DialogResult result = DialogResult.Cancel;
        
        switch (module.Type)
        {
            case "ModbusTCP":
            case "ModbusRTU":
                using (var dialog = new ModbusSettingsDialog(module))
                {
                    result = dialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        module.Settings = dialog.GetSettings();
                        _isModified = true;
                    }
                }
                break;
            case "OPCUA":
                using (var dialog = new OPCUAConnectionDialog(module))
                {
                    result = dialog.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        module.Settings = dialog.GetSettings();
                        _isModified = true;
                    }
                }
                break;
            default:
                MessageBox.Show($"Configuration dialog for {module.Type} not yet implemented.", "Info", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                break;
        }

        if (result == DialogResult.OK)
        {
            LoadModules();
        }
    }

    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _modulesGrid.Rows[e.RowIndex];
        if (row.Tag is not CommunicationModule module)
            return;

        var column = _modulesGrid.Columns[e.ColumnIndex];
        var value = row.Cells[e.ColumnIndex].Value;

        switch (column.Name)
        {
            case "Name":
                module.Name = value?.ToString() ?? string.Empty;
                break;
            case "Type":
                module.Type = value?.ToString() ?? "ModbusTCP";
                break;
            case "Enabled":
                module.Enabled = value is bool enabled ? enabled : true;
                break;
            case "Description":
                module.Description = value?.ToString() ?? string.Empty;
                break;
        }

        _isModified = true;
    }
    
    private void OnModuleSelectionChanged(object? sender, EventArgs e)
    {
        if (_modulesGrid.SelectedRows.Count == 0)
        {
            _selectedModule = null;
            LoadTagMappings();
            return;
        }

        var row = _modulesGrid.SelectedRows[0];
        if (row.Tag is CommunicationModule module)
        {
            _selectedModule = module;
            LoadTagMappings();
        }
    }
    
    private void LoadTagMappings()
    {
        _tagMappingsGrid.Rows.Clear();

        if (_selectedModule == null)
            return;

        foreach (var mapping in _selectedModule.TagMappings)
        {
            int rowIndex = _tagMappingsGrid.Rows.Add(
                mapping.TagName,
                mapping.Address,
                mapping.DataType,
                mapping.Enabled,
                mapping.Description
            );

            _tagMappingsGrid.Rows[rowIndex].Tag = mapping;
        }
    }
    
    private void AddTagMapping()
    {
        if (_selectedModule == null)
        {
            MessageBox.Show("Please select a module first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var mapping = new TagMapping
        {
            TagName = "NewTag",
            Address = "0",
            DataType = "Float32",
            Enabled = true
        };

        _selectedModule.TagMappings.Add(mapping);
        LoadTagMappings();
        _isModified = true;
    }
    
    private void RemoveSelectedTagMapping()
    {
        if (_tagMappingsGrid.SelectedRows.Count == 0 || _selectedModule == null)
            return;

        var row = _tagMappingsGrid.SelectedRows[0];
        if (row.Tag is TagMapping mapping)
        {
            _selectedModule.TagMappings.Remove(mapping);
            LoadTagMappings();
            _isModified = true;
        }
    }
    
    private void OnTagMappingCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _selectedModule == null)
            return;

        var row = _tagMappingsGrid.Rows[e.RowIndex];
        if (row.Tag is TagMapping mapping)
        {
            var column = _tagMappingsGrid.Columns[e.ColumnIndex];
            var value = row.Cells[e.ColumnIndex].Value;

            switch (column.Name)
            {
                case "TagName":
                    mapping.TagName = value?.ToString() ?? string.Empty;
                    break;
                case "Address":
                    mapping.Address = value?.ToString() ?? string.Empty;
                    break;
                case "DataType":
                    mapping.DataType = value?.ToString() ?? "Float32";
                    break;
                case "Enabled":
                    mapping.Enabled = value is bool enabled ? enabled : true;
                    break;
                case "Description":
                    mapping.Description = value?.ToString() ?? string.Empty;
                    break;
            }

            _isModified = true;
        }
    }
    
    /// <summary>
    /// Auto-maps tags to addresses based on tag addresses.
    /// </summary>
    private void AutoMapTagMappings()
    {
        if (_selectedModule == null || _projectManager == null || string.IsNullOrEmpty(_scadaName))
        {
            MessageBox.Show("Please select a module first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Get all tags from tag tables
        var tagTables = _projectManager.GetTagTables(_scadaName);
        var allTags = new List<Designer.Modules.TagEngine.Tag>();
        foreach (var table in tagTables)
        {
            if (table is Designer.Modules.TagEngine.TagTable tagTable)
            {
                allTags.AddRange(tagTable.GetTags());
            }
        }

        if (allTags.Count == 0)
        {
            MessageBox.Show("No tags found to map.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Clear existing mappings
        _selectedModule.TagMappings.Clear();

        // Auto-map tags based on their addresses
        int addressOffset = 0;
        foreach (var tag in allTags)
        {
            var mapping = new TagMapping
            {
                TagName = tag.Name,
                Address = tag.Address ?? addressOffset.ToString(),
                DataType = tag.DataType ?? "Float32",
                Enabled = true,
                Description = tag.Description
            };

            _selectedModule.TagMappings.Add(mapping);
            
            // Increment address offset for next tag
            if (string.IsNullOrEmpty(tag.Address))
            {
                addressOffset++;
            }
        }

        LoadTagMappings();
        _isModified = true;
        MessageBox.Show($"Auto-mapped {allTags.Count} tags.", "Auto-Map Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    
    /// <summary>
    /// Validates tag mappings and shows validation results.
    /// </summary>
    private void ValidateTagMappings()
    {
        if (_selectedModule == null)
        {
            MessageBox.Show("Please select a module first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate each mapping
        foreach (var mapping in _selectedModule.TagMappings)
        {
            // Check if tag name is empty
            if (string.IsNullOrWhiteSpace(mapping.TagName))
            {
                errors.Add($"Mapping has empty tag name (Address: {mapping.Address})");
            }

            // Check if address is empty
            if (string.IsNullOrWhiteSpace(mapping.Address))
            {
                errors.Add($"Tag '{mapping.TagName}' has empty address");
            }

            // Validate address format based on module type
            if (!ValidateAddressFormat(mapping.Address, _selectedModule.Type))
            {
                warnings.Add($"Tag '{mapping.TagName}' has invalid address format '{mapping.Address}' for {_selectedModule.Type}");
            }

            // Check if data type matches address format
            if (!IsAddressMatchingDataType(mapping.Address, mapping.DataType))
            {
                warnings.Add($"Tag '{mapping.TagName}': Address '{mapping.Address}' may not match data type '{mapping.DataType}'");
            }
        }

        // Show validation results
        if (errors.Count == 0 && warnings.Count == 0)
        {
            MessageBox.Show("All tag mappings are valid.", "Validation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            var message = "";
            if (errors.Count > 0)
            {
                message += $"Errors ({errors.Count}):\n" + string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                    message += $"\n... and {errors.Count - 10} more errors";
            }
            if (warnings.Count > 0)
            {
                if (!string.IsNullOrEmpty(message))
                    message += "\n\n";
                message += $"Warnings ({warnings.Count}):\n" + string.Join("\n", warnings.Take(10));
                if (warnings.Count > 10)
                    message += $"\n... and {warnings.Count - 10} more warnings";
            }

            MessageBox.Show(message, "Validation Results", MessageBoxButtons.OK, 
                errors.Count > 0 ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
        }
    }
    
    /// <summary>
    /// Validates address format based on protocol type.
    /// </summary>
    private bool ValidateAddressFormat(string address, string protocolType)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        address = address.ToUpper();

        // Modbus addresses: 0-65535 (holding registers), 0-65535 (coils)
        if (protocolType == "ModbusTCP" || protocolType == "ModbusRTU")
        {
            // Allow numeric addresses or formatted addresses like "40001" (holding register), "00001" (coil)
            return System.Text.RegularExpressions.Regex.IsMatch(address, @"^\d+$") || 
                   System.Text.RegularExpressions.Regex.IsMatch(address, @"^[0-9]+\.[0-9]+$");
        }

        // OPC UA: NodeId format (ns=2;s=MyNode)
        if (protocolType == "OPCUA")
        {
            return address.Contains("=") || System.Text.RegularExpressions.Regex.IsMatch(address, @"^ns=\d+");
        }

        // Default: allow alphanumeric addresses
        return true;
    }
    
    /// <summary>
    /// Checks if address format matches data type.
    /// </summary>
    private bool IsAddressMatchingDataType(string address, string dataType)
    {
        if (string.IsNullOrEmpty(address))
            return true;

        address = address.ToUpper();

        // Bit addresses should contain a dot
        if (dataType == "Bit")
        {
            return address.Contains(".");
        }

        // Word addresses
        if (dataType == "Word" || dataType == "Int16" || dataType == "UInt16")
        {
            return address.Contains("W") || System.Text.RegularExpressions.Regex.IsMatch(address, @"^\d+$");
        }

        // Double word addresses
        if (dataType == "DWord" || dataType == "Float32" || dataType == "Int32" || dataType == "UInt32")
        {
            return address.Contains("D") || System.Text.RegularExpressions.Regex.IsMatch(address, @"^\d+$");
        }

        return true;
    }

    public CommunicationModules? GetCommunicationModules()
    {
        return _communicationModules;
    }
}
