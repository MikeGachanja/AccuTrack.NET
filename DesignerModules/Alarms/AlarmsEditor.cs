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
    private DataGridView _alarmsGrid;
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
            RowCount = 2,
            Padding = new Padding(10)
        };

        // Toolbar
        var toolbar = new ToolStrip
        {
            Dock = DockStyle.Top
        };

        var addButton = new ToolStripButton("Add Alarm");
        addButton.Click += (s, e) => AddAlarm();
        toolbar.Items.Add(addButton);

        var removeButton = new ToolStripButton("Remove Alarm");
        removeButton.Click += (s, e) => RemoveSelectedAlarm();
        toolbar.Items.Add(removeButton);

        // Alarms grid
        _alarmsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _alarmsGrid.Columns.Add("Name", "Name");
        _alarmsGrid.Columns.Add("TagName", "Tag");
        _alarmsGrid.Columns.Add("Condition", "Condition");
        _alarmsGrid.Columns.Add("Threshold", "Threshold");
        _alarmsGrid.Columns.Add("Priority", "Priority");
        _alarmsGrid.Columns.Add("Message", "Message");
        _alarmsGrid.Columns.Add("Enabled", "Enabled");

        // Make TagName a combo box with available tags
        var tagColumn = new DataGridViewComboBoxColumn
        {
            Name = "TagName",
            HeaderText = "Tag",
            DataPropertyName = "TagName"
        };
        tagColumn.Items.AddRange(_availableTags.Select(t => t.Name).ToArray());
        _alarmsGrid.Columns.Remove("TagName");
        _alarmsGrid.Columns.Insert(1, tagColumn);

        // Make Condition a combo box
        var conditionColumn = new DataGridViewComboBoxColumn
        {
            Name = "Condition",
            HeaderText = "Condition",
            DataPropertyName = "Condition"
        };
        conditionColumn.Items.AddRange(new[] { "GreaterThan", "LessThan", "EqualTo", "NotEqualTo" });
        _alarmsGrid.Columns.Remove("Condition");
        _alarmsGrid.Columns.Insert(2, conditionColumn);

        // Make Priority a combo box
        var priorityColumn = new DataGridViewComboBoxColumn
        {
            Name = "Priority",
            HeaderText = "Priority",
            DataPropertyName = "Priority"
        };
        priorityColumn.Items.AddRange(new[] { "Low", "Medium", "High", "Critical" });
        _alarmsGrid.Columns.Remove("Priority");
        _alarmsGrid.Columns.Insert(4, priorityColumn);

        // Make Enabled a checkbox
        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _alarmsGrid.Columns.Remove("Enabled");
        _alarmsGrid.Columns.Add(enabledColumn);

        _alarmsGrid.CellValueChanged += OnCellValueChanged;

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_alarmsGrid, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
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
    /// Loads alarms into the grid.
    /// </summary>
    private void LoadAlarms()
    {
        _alarmsGrid.Rows.Clear();

        if (_alarms == null)
            return;

        foreach (var alarm in _alarms.AlarmDefinitions)
        {
            int rowIndex = _alarmsGrid.Rows.Add(
                alarm.Name,
                alarm.TagName,
                alarm.Condition,
                alarm.Threshold,
                alarm.Priority,
                alarm.Message,
                alarm.Enabled
            );

            _alarmsGrid.Rows[rowIndex].Tag = alarm;
        }
    }

    /// <summary>
    /// Adds a new alarm.
    /// </summary>
    private void AddAlarm()
    {
        if (_alarms == null)
            _alarms = new Alarms();

        var alarm = new AlarmDefinition
        {
            Name = "New Alarm",
            Enabled = true
        };

        _alarms.AlarmDefinitions.Add(alarm);
        LoadAlarms();
    }

    /// <summary>
    /// Removes the selected alarm.
    /// </summary>
    private void RemoveSelectedAlarm()
    {
        if (_alarmsGrid.SelectedRows.Count == 0 || _alarms == null)
            return;

        var row = _alarmsGrid.SelectedRows[0];
        if (row.Tag is AlarmDefinition alarm)
        {
            _alarms.AlarmDefinitions.Remove(alarm);
            LoadAlarms();
        }
    }

    /// <summary>
    /// Handles cell value changes.
    /// </summary>
    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || _alarms == null)
            return;

        var row = _alarmsGrid.Rows[e.RowIndex];
        if (row.Tag is AlarmDefinition alarm)
        {
            var columnName = _alarmsGrid.Columns[e.ColumnIndex].Name;

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
        }
    }
}
