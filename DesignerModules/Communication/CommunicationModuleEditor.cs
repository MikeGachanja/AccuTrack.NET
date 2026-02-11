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
    private DataGridView _modulesGrid;
    private CommunicationModules? _communicationModules;
    private ScadaProject? _scadaProject;
    private ProjectManager? _projectManager;
    private string? _scadaName;
    private bool _isModified = false;

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
        _modulesGrid.DoubleClick += (s, e) => ConfigureSelectedModule();

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_modulesGrid, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
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

    public CommunicationModules? GetCommunicationModules()
    {
        return _communicationModules;
    }
}
