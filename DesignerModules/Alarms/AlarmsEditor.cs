using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Alarms;
using Designer.Modules.TagEngine;
using Designer.Modules.Project;
using Designer.Modules.Components;

namespace Designer.Modules.Alarms;

/// <summary>
/// Editor for alarms configuration.
/// </summary>
public partial class AlarmsEditor : UserControl
{
    private TabControl _alarmsTabs;
    private DataGridView _hmiDigitalGrid;
    private DataGridView _hmiAnalogGrid;
    private DataGridView _controllerDigitalGrid;
    private DataGridView _controllerAnalogGrid;
    private Alarms? _alarms;
    private ScadaProject? _scadaProject;
    private string? _scadaName;
    private ProjectManager? _projectManager;
    private List<Tag> _availableTags = new List<Tag>();
    private bool _isModified = false;

    public bool IsModified => _isModified;

    public AlarmsEditor()
    {
        InitializeComponent();
    }

    public AlarmsEditor(List<Tag> availableTags) : this()
    {
        _availableTags = availableTags ?? new List<Tag>();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(10)
        };

        // Create tab control for different alarm types
        _alarmsTabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Create tabs for each alarm type
        _alarmsTabs.TabPages.Add("HMI Digital");
        _alarmsTabs.TabPages.Add("HMI Analog");
        _alarmsTabs.TabPages.Add("Controller Digital");
        _alarmsTabs.TabPages.Add("Controller Analog");

        // Create grids for each tab
        _hmiDigitalGrid = CreateAlarmsGrid("HMI", "Digital");
        _hmiAnalogGrid = CreateAlarmsGrid("HMI", "Analog");
        _controllerDigitalGrid = CreateAlarmsGrid("Controller", "Digital");
        _controllerAnalogGrid = CreateAlarmsGrid("Controller", "Analog");

        _alarmsTabs.TabPages[0].Controls.Add(_hmiDigitalGrid);
        _alarmsTabs.TabPages[1].Controls.Add(_hmiAnalogGrid);
        _alarmsTabs.TabPages[2].Controls.Add(_controllerDigitalGrid);
        _alarmsTabs.TabPages[3].Controls.Add(_controllerAnalogGrid);

        mainLayout.Controls.Add(_alarmsTabs, 0, 0);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }
    
    /// <summary>
    /// Creates a DataGridView for alarms with toolbar.
    /// </summary>
    private DataGridView CreateAlarmsGrid(string type, string source)
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        
        // Toolbar
        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addButton = new ToolStripButton("Add Alarm");
        addButton.Click += (s, e) => AddAlarm(type, source);
        toolbar.Items.Add(addButton);
        var removeButton = new ToolStripButton("Remove Alarm");
        removeButton.Click += (s, e) => RemoveSelectedAlarm(type, source);
        toolbar.Items.Add(removeButton);

        // Alarms grid
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        grid.Columns.Add("Name", "Name");
        grid.Columns.Add("TagName", "Tag");
        grid.Columns.Add("Condition", "Condition");
        grid.Columns.Add("Threshold", "Threshold");
        grid.Columns.Add("Priority", "Priority");
        grid.Columns.Add("Message", "Message");
        grid.Columns.Add("Enabled", "Enabled");

        // TagName column is a text column - clicking it opens the tag selector dialog
        var tagColumn = grid.Columns["TagName"];
        tagColumn.ReadOnly = false; // Allow editing via dialog

        // Make Condition a combo box
        var conditionColumn = new DataGridViewComboBoxColumn
        {
            Name = "Condition",
            HeaderText = "Condition",
            DataPropertyName = "Condition"
        };
        conditionColumn.Items.AddRange(new[] { "GreaterThan", "LessThan", "EqualTo", "NotEqualTo" });
        grid.Columns.Remove("Condition");
        grid.Columns.Insert(2, conditionColumn);

        // Make Priority a combo box
        var priorityColumn = new DataGridViewComboBoxColumn
        {
            Name = "Priority",
            HeaderText = "Priority",
            DataPropertyName = "Priority"
        };
        priorityColumn.Items.AddRange(new[] { "Low", "Medium", "High", "Critical" });
        grid.Columns.Remove("Priority");
        grid.Columns.Insert(4, priorityColumn);

        // Make Enabled a checkbox
        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        grid.Columns.Remove("Enabled");
        grid.Columns.Add(enabledColumn);

        grid.CellValueChanged += (s, e) => OnCellValueChanged(s, e, type, source);
        grid.CellClick += (s, e) => OnCellClick(s, e, type, source);

        panel.Controls.Add(grid);
        panel.Controls.Add(toolbar);
        
        return grid;
    }

    /// <summary>
    /// Sets the alarms to edit.
    /// </summary>
    public void SetAlarms(Alarms alarms)
    {
        _alarms = alarms;
        LoadAlarms();
        _isModified = false;
    }

    /// <summary>
    /// Gets the current alarms configuration.
    /// </summary>
    public Alarms? GetAlarms()
    {
        if (_alarms == null)
            return null;

        // Rebuild alarms from grids
        _alarms.AlarmDefinitions.Clear();
        
        CollectAlarmsFromGrid(_hmiDigitalGrid, "HMI", "Digital");
        CollectAlarmsFromGrid(_hmiAnalogGrid, "HMI", "Analog");
        CollectAlarmsFromGrid(_controllerDigitalGrid, "Controller", "Digital");
        CollectAlarmsFromGrid(_controllerAnalogGrid, "Controller", "Analog");

        return _alarms;
    }

    /// <summary>
    /// Collects alarms from a grid and adds them to the alarms list.
    /// </summary>
    private void CollectAlarmsFromGrid(DataGridView grid, string type, string source)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            
            if (row.Tag is AlarmDefinition alarm)
            {
                // Update alarm from grid cells
                if (row.Cells["Name"].Value != null)
                    alarm.Name = row.Cells["Name"].Value.ToString() ?? string.Empty;
                if (row.Cells["TagName"].Value != null)
                    alarm.TagName = row.Cells["TagName"].Value.ToString() ?? string.Empty;
                if (row.Cells["Condition"].Value != null)
                    alarm.Condition = row.Cells["Condition"].Value.ToString() ?? "GreaterThan";
                if (row.Cells["Threshold"].Value != null && double.TryParse(row.Cells["Threshold"].Value.ToString(), out double threshold))
                    alarm.Threshold = threshold;
                if (row.Cells["Priority"].Value != null)
                    alarm.Priority = row.Cells["Priority"].Value.ToString() ?? "Medium";
                if (row.Cells["Message"].Value != null)
                    alarm.Message = row.Cells["Message"].Value.ToString() ?? string.Empty;
                if (row.Cells["Enabled"].Value is bool enabled)
                    alarm.Enabled = enabled;
                
                alarm.Type = type;
                alarm.Source = source;
                
                if (!_alarms!.AlarmDefinitions.Contains(alarm))
                {
                    _alarms.AlarmDefinitions.Add(alarm);
                }
            }
            else
            {
                // Create new alarm from row
                var newAlarm = new AlarmDefinition
                {
                    Name = row.Cells["Name"].Value?.ToString() ?? "New Alarm",
                    TagName = row.Cells["TagName"].Value?.ToString() ?? string.Empty,
                    Condition = row.Cells["Condition"].Value?.ToString() ?? "GreaterThan",
                    Threshold = double.TryParse(row.Cells["Threshold"].Value?.ToString(), out double t) ? t : 0,
                    Priority = row.Cells["Priority"].Value?.ToString() ?? "Medium",
                    Message = row.Cells["Message"].Value?.ToString() ?? string.Empty,
                    Enabled = row.Cells["Enabled"].Value is bool enabledVal ? enabledVal : true,
                    Type = type,
                    Source = source
                };
                _alarms!.AlarmDefinitions.Add(newAlarm);
                row.Tag = newAlarm;
            }
        }
    }

    /// <summary>
    /// Resets the modified flag (called after successful save).
    /// </summary>
    public void ResetModified()
    {
        _isModified = false;
    }

    /// <summary>
    /// Sets the SCADA project.
    /// </summary>
    public void SetScadaProject(ScadaProject project)
    {
        _scadaProject = project;
    }

    /// <summary>
    /// Sets the SCADA name for accessing tag tables.
    /// </summary>
    public void SetScadaName(string scadaName)
    {
        _scadaName = scadaName;
    }

    /// <summary>
    /// Sets the project manager for accessing tag tables.
    /// </summary>
    public void SetProjectManager(ProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    /// <summary>
    /// Loads alarms into the grids.
    /// </summary>
    private void LoadAlarms()
    {
        _hmiDigitalGrid.Rows.Clear();
        _hmiAnalogGrid.Rows.Clear();
        _controllerDigitalGrid.Rows.Clear();
        _controllerAnalogGrid.Rows.Clear();

        if (_alarms == null)
            return;

        foreach (var alarm in _alarms.AlarmDefinitions)
        {
            DataGridView targetGrid = GetGridForAlarmType(alarm.Type, alarm.Source);
            if (targetGrid != null)
            {
                int rowIndex = targetGrid.Rows.Add(
                    alarm.Name,
                    alarm.TagName,
                    alarm.Condition,
                    alarm.Threshold,
                    alarm.Priority,
                    alarm.Message,
                    alarm.Enabled
                );

                targetGrid.Rows[rowIndex].Tag = alarm;
            }
        }
    }
    
    /// <summary>
    /// Gets the appropriate grid for alarm type and source.
    /// </summary>
    private DataGridView GetGridForAlarmType(string type, string source)
    {
        if (type == "HMI" && source == "Digital")
            return _hmiDigitalGrid;
        else if (type == "HMI" && source == "Analog")
            return _hmiAnalogGrid;
        else if (type == "Controller" && source == "Digital")
            return _controllerDigitalGrid;
        else if (type == "Controller" && source == "Analog")
            return _controllerAnalogGrid;
        return _hmiDigitalGrid; // Default
    }

    /// <summary>
    /// Adds a new alarm.
    /// </summary>
    private void AddAlarm(string type, string source)
    {
        if (_alarms == null)
            _alarms = new Alarms();

        var alarm = new AlarmDefinition
        {
            Name = "New Alarm",
            Enabled = true,
            Type = type,
            Source = source
        };

        _alarms.AlarmDefinitions.Add(alarm);
        LoadAlarms();
        _isModified = true;
    }

    /// <summary>
    /// Removes the selected alarm.
    /// </summary>
    private void RemoveSelectedAlarm(string type, string source)
    {
        var grid = GetGridForAlarmType(type, source);
        if (grid.SelectedRows.Count == 0 || _alarms == null)
            return;

        var row = grid.SelectedRows[0];
        if (row.Tag is AlarmDefinition alarm)
        {
            _alarms.AlarmDefinitions.Remove(alarm);
            LoadAlarms();
            _isModified = true;
        }
    }

    /// <summary>
    /// Handles cell clicks - opens tag selector dialog for TagName column.
    /// </summary>
    private void OnCellClick(object? sender, DataGridViewCellEventArgs e, string type, string source)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var grid = sender as DataGridView;
        if (grid == null)
            return;

        var column = grid.Columns[e.ColumnIndex];
        if (column.Name != "TagName")
            return;

        var row = grid.Rows[e.RowIndex];
        if (row.IsNewRow)
            return;

        // Get or create alarm definition (don't add to list yet - wait for successful tag selection)
        AlarmDefinition alarm;
        if (row.Tag is AlarmDefinition existingAlarm)
        {
            alarm = existingAlarm;
        }
        else
        {
            // Create new alarm from row data (temporary, will be added to list after tag selection)
            alarm = new AlarmDefinition
            {
                Name = row.Cells["Name"].Value?.ToString() ?? "New Alarm",
                TagName = row.Cells["TagName"].Value?.ToString() ?? string.Empty,
                Condition = row.Cells["Condition"].Value?.ToString() ?? "GreaterThan",
                Threshold = double.TryParse(row.Cells["Threshold"].Value?.ToString(), out double t) ? t : 0,
                Priority = row.Cells["Priority"].Value?.ToString() ?? "Medium",
                Message = row.Cells["Message"].Value?.ToString() ?? string.Empty,
                Enabled = row.Cells["Enabled"].Value is bool enabledVal ? enabledVal : true,
                Type = type,
                Source = source
            };
            row.Tag = alarm;
        }

        // Get tag tables from ProjectManager if available, otherwise from scadaProject
        var tagTables = new List<TagTable>();
        
        // Try to get tag tables via ProjectManager (most reliable)
        if (_projectManager != null && !string.IsNullOrEmpty(_scadaName))
        {
            var tables = _projectManager.GetTagTables(_scadaName);
            tagTables = tables.OfType<TagTable>().ToList();
        }
        
        // Fallback to scadaProject if tagTables is still empty
        if (tagTables.Count == 0 && _scadaProject != null)
        {
            tagTables = _scadaProject.TagTables.OfType<TagTable>().ToList();
        }

        if (tagTables.Count == 0)
        {
            MessageBox.Show(this, "No tag tables available. Please configure tag tables first.", "No Tags", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Determine required data type based on alarm source
        string? requiredDataType = null;
        if (source == "Digital")
        {
            // Digital alarms should use Boolean/Bit types
            requiredDataType = "Boolean";
        }
        else if (source == "Analog")
        {
            // Analog alarms should use numeric types (Real, Float32, Float64, Int32, etc.)
            requiredDataType = "Real";
        }

        // Open tag selector dialog
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(tagTables);
        dialog.SetSelectedTagName(alarm.TagName);
        
        if (!string.IsNullOrEmpty(requiredDataType))
        {
            dialog.SetRequiredDataType(requiredDataType);
        }
        
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedTag != null)
        {
            alarm.TagName = dialog.SelectedTagName;
            
            // Update the cell display
            var cell = row.Cells[e.ColumnIndex];
            cell.Value = alarm.TagName;
            
            // Ensure alarm type and source match the grid
            alarm.Type = type;
            alarm.Source = source;
            
            // Add alarm to list if it's not already there (for new alarms)
            if (_alarms == null)
                _alarms = new Alarms();
            if (!_alarms.AlarmDefinitions.Contains(alarm))
                _alarms.AlarmDefinitions.Add(alarm);
            
            _isModified = true;
        }
    }

    /// <summary>
    /// Handles cell value changes.
    /// </summary>
    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e, string type, string source)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _alarms == null)
            return;

        var grid = sender as DataGridView;
        if (grid == null)
            return;

        var row = grid.Rows[e.RowIndex];
        if (row.IsNewRow)
            return;

        // Get or create alarm definition
        AlarmDefinition alarm;
        if (row.Tag is AlarmDefinition existingAlarm)
        {
            alarm = existingAlarm;
        }
        else
        {
            // Create new alarm from row data
            alarm = new AlarmDefinition
            {
                Name = row.Cells["Name"].Value?.ToString() ?? "New Alarm",
                TagName = row.Cells["TagName"].Value?.ToString() ?? string.Empty,
                Condition = row.Cells["Condition"].Value?.ToString() ?? "GreaterThan",
                Threshold = double.TryParse(row.Cells["Threshold"].Value?.ToString(), out double t) ? t : 0,
                Priority = row.Cells["Priority"].Value?.ToString() ?? "Medium",
                Message = row.Cells["Message"].Value?.ToString() ?? string.Empty,
                Enabled = row.Cells["Enabled"].Value is bool enabledVal ? enabledVal : true,
                Type = type,
                Source = source
            };
            row.Tag = alarm;
            if (!_alarms.AlarmDefinitions.Contains(alarm))
                _alarms.AlarmDefinitions.Add(alarm);
        }

        var columnName = grid.Columns[e.ColumnIndex].Name;

        switch (columnName)
        {
            case "Name":
                alarm.Name = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
                break;
            case "TagName":
                alarm.TagName = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
                break;
            case "Condition":
                alarm.Condition = row.Cells[e.ColumnIndex].Value?.ToString() ?? "GreaterThan";
                break;
            case "Threshold":
                if (double.TryParse(row.Cells[e.ColumnIndex].Value?.ToString(), out double threshold))
                    alarm.Threshold = threshold;
                break;
            case "Priority":
                alarm.Priority = row.Cells[e.ColumnIndex].Value?.ToString() ?? "Medium";
                break;
            case "Message":
                alarm.Message = row.Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
                break;
            case "Enabled":
                if (row.Cells[e.ColumnIndex].Value is bool enabled)
                    alarm.Enabled = enabled;
                break;
        }
        
        // Ensure alarm type and source match the grid
        alarm.Type = type;
        alarm.Source = source;
        _isModified = true;
    }
}
