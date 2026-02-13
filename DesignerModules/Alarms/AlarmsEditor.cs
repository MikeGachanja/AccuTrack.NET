using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Alarms;
using Designer.Modules.TagEngine;
using Designer.Modules.Project;

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
    private List<Tag> _availableTags = new List<Tag>();

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

        // Make TagName a combo box with available tags
        var tagColumn = new DataGridViewComboBoxColumn
        {
            Name = "TagName",
            HeaderText = "Tag",
            DataPropertyName = "TagName"
        };
        tagColumn.Items.AddRange(_availableTags.Select(t => t.Name).ToArray());
        grid.Columns.Remove("TagName");
        grid.Columns.Insert(1, tagColumn);

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
    }

    /// <summary>
    /// Sets the SCADA project.
    /// </summary>
    public void SetScadaProject(ScadaProject project)
    {
        _scadaProject = project;
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
        if (row.Tag is AlarmDefinition alarm)
        {
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
        }
    }
}
