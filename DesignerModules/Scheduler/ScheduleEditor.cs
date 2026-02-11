using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Scheduler;
using Designer.Modules.Project;

namespace Designer.Modules.Scheduler;

/// <summary>
/// Editor for schedules configuration.
/// </summary>
public partial class ScheduleEditor : UserControl
{
    private DataGridView _schedulesGrid;
    private Schedules? _schedules;
    private ScadaProject? _scadaProject;
    private List<string> _availableScripts = new List<string>();
    private bool _isModified = false;

    public bool IsModified => _isModified;

    public ScheduleEditor()
    {
        InitializeComponent();
    }

    public ScheduleEditor(List<string> availableScripts) : this()
    {
        _availableScripts = availableScripts ?? new List<string>();
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

        var addButton = new ToolStripButton("Add Schedule");
        addButton.Click += (s, e) => AddSchedule();
        toolbar.Items.Add(addButton);

        var removeButton = new ToolStripButton("Remove Schedule");
        removeButton.Click += (s, e) => RemoveSelectedSchedule();
        toolbar.Items.Add(removeButton);

        var selectScriptButton = new ToolStripButton("Select Script...");
        selectScriptButton.Click += (s, e) => SelectScriptForSelectedSchedule();
        toolbar.Items.Add(selectScriptButton);

        // Schedules grid
        _schedulesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _schedulesGrid.Columns.Add("Name", "Name");
        _schedulesGrid.Columns.Add("ScriptName", "Script");
        _schedulesGrid.Columns.Add("Recurrence", "Recurrence");
        _schedulesGrid.Columns.Add("Time", "Time");
        _schedulesGrid.Columns.Add("DayOfWeek", "Day of Week");
        _schedulesGrid.Columns.Add("DayOfMonth", "Day of Month");
        _schedulesGrid.Columns.Add("Enabled", "Enabled");
        _schedulesGrid.Columns.Add("Description", "Description");

        // Make ScriptName a combo box with available scripts
        var scriptColumn = new DataGridViewComboBoxColumn
        {
            Name = "ScriptName",
            HeaderText = "Script",
            DataPropertyName = "ScriptName"
        };
        scriptColumn.Items.AddRange(_availableScripts.ToArray());
        _schedulesGrid.Columns.Remove("ScriptName");
        _schedulesGrid.Columns.Insert(1, scriptColumn);

        // Make Recurrence a combo box
        var recurrenceColumn = new DataGridViewComboBoxColumn
        {
            Name = "Recurrence",
            HeaderText = "Recurrence",
            DataPropertyName = "Recurrence"
        };
        recurrenceColumn.Items.AddRange(new[] { "Once", "Daily", "Weekly", "Monthly", "Custom" });
        _schedulesGrid.Columns.Remove("Recurrence");
        _schedulesGrid.Columns.Insert(2, recurrenceColumn);

        // Make DayOfWeek a combo box
        var dayOfWeekColumn = new DataGridViewComboBoxColumn
        {
            Name = "DayOfWeek",
            HeaderText = "Day of Week",
            DataPropertyName = "DayOfWeek"
        };
        dayOfWeekColumn.Items.AddRange(new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
        _schedulesGrid.Columns.Remove("DayOfWeek");
        _schedulesGrid.Columns.Insert(4, dayOfWeekColumn);

        // Make Enabled a checkbox column
        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _schedulesGrid.Columns.Remove("Enabled");
        _schedulesGrid.Columns.Insert(6, enabledColumn);

        _schedulesGrid.CellValueChanged += OnCellValueChanged;
        _schedulesGrid.CellEndEdit += (s, e) => _isModified = true;

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_schedulesGrid, 0, 1);
        mainLayout.SetRowSpan(_schedulesGrid, 1);

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Sets the schedules to edit.
    /// </summary>
    public void SetSchedules(Schedules schedules)
    {
        _schedules = schedules;
        LoadSchedules();
        _isModified = false;
    }

    /// <summary>
    /// Sets the SCADA project context.
    /// </summary>
    public void SetScadaProject(ScadaProject project)
    {
        _scadaProject = project;
    }

    /// <summary>
    /// Updates the list of available scripts.
    /// </summary>
    public void SetAvailableScripts(List<string> scriptNames)
    {
        _availableScripts = scriptNames ?? new List<string>();
        
        // Update the combo box column
        if (_schedulesGrid.Columns["ScriptName"] is DataGridViewComboBoxColumn scriptColumn)
        {
            scriptColumn.Items.Clear();
            scriptColumn.Items.AddRange(_availableScripts.ToArray());
        }
    }

    private void LoadSchedules()
    {
        _schedulesGrid.Rows.Clear();

        if (_schedules == null)
            return;

        foreach (var schedule in _schedules.ScheduleDefinitions)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_schedulesGrid);
            row.Cells[0].Value = schedule.Name;
            row.Cells[1].Value = schedule.ScriptName;
            row.Cells[2].Value = schedule.Recurrence;
            row.Cells[3].Value = schedule.Time;
            row.Cells[4].Value = GetDayOfWeekName(schedule.DayOfWeek);
            row.Cells[5].Value = schedule.DayOfMonth;
            row.Cells[6].Value = schedule.Enabled;
            row.Cells[7].Value = schedule.Description;
            row.Tag = schedule;
            _schedulesGrid.Rows.Add(row);
        }
    }

    private string GetDayOfWeekName(int dayOfWeek)
    {
        var days = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        return dayOfWeek >= 0 && dayOfWeek < days.Length ? days[dayOfWeek] : "Sunday";
    }

    private int GetDayOfWeekIndex(string dayName)
    {
        var days = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        return Array.IndexOf(days, dayName);
    }

    private void AddSchedule()
    {
        if (_schedules == null)
            _schedules = new Schedules();

        var newSchedule = new ScheduleDefinition
        {
            Name = $"Schedule_{_schedules.ScheduleDefinitions.Count + 1}",
            ScriptName = _availableScripts.FirstOrDefault() ?? string.Empty,
            Recurrence = "Daily",
            Time = DateTime.Now.ToString("HH:mm"),
            Enabled = true
        };

        _schedules.ScheduleDefinitions.Add(newSchedule);
        LoadSchedules();
        _isModified = true;
    }

    private void RemoveSelectedSchedule()
    {
        if (_schedulesGrid.SelectedRows.Count == 0)
            return;

        var selectedRow = _schedulesGrid.SelectedRows[0];
        if (selectedRow.Tag is ScheduleDefinition schedule && _schedules != null)
        {
            _schedules.ScheduleDefinitions.Remove(schedule);
            LoadSchedules();
            _isModified = true;
        }
    }

    private void SelectScriptForSelectedSchedule()
    {
        if (_schedulesGrid.SelectedRows.Count == 0)
        {
            MessageBox.Show("Please select a schedule first.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using (var dialog = new ScriptSelectionDialog(_availableScripts))
        {
            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedScript))
            {
                var selectedRow = _schedulesGrid.SelectedRows[0];
                if (selectedRow.Tag is ScheduleDefinition schedule)
                {
                    schedule.ScriptName = dialog.SelectedScript;
                    selectedRow.Cells[1].Value = dialog.SelectedScript;
                    _isModified = true;
                }
            }
        }
    }

    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _schedulesGrid.Rows[e.RowIndex];
        if (row.Tag is not ScheduleDefinition schedule)
            return;

        var column = _schedulesGrid.Columns[e.ColumnIndex];
        var value = row.Cells[e.ColumnIndex].Value;

        switch (column.Name)
        {
            case "Name":
                schedule.Name = value?.ToString() ?? string.Empty;
                break;
            case "ScriptName":
                schedule.ScriptName = value?.ToString() ?? string.Empty;
                break;
            case "Recurrence":
                schedule.Recurrence = value?.ToString() ?? "Once";
                break;
            case "Time":
                schedule.Time = value?.ToString() ?? "00:00";
                break;
            case "DayOfWeek":
                schedule.DayOfWeek = GetDayOfWeekIndex(value?.ToString() ?? "Sunday");
                break;
            case "DayOfMonth":
                if (int.TryParse(value?.ToString(), out int dayOfMonth))
                    schedule.DayOfMonth = dayOfMonth;
                break;
            case "Enabled":
                schedule.Enabled = value is bool enabled ? enabled : true;
                break;
            case "Description":
                schedule.Description = value?.ToString() ?? string.Empty;
                break;
        }

        _isModified = true;
    }

    /// <summary>
    /// Gets the current schedules.
    /// </summary>
    public Schedules? GetSchedules()
    {
        return _schedules;
    }
}
