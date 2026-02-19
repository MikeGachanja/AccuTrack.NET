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
    private List<(string Id, string Name)> _availableMLModels = new List<(string, string)>();
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

        var selectTargetButton = new ToolStripButton("Select Target...");
        selectTargetButton.Click += (s, e) => SelectTargetForSelectedSchedule();
        toolbar.Items.Add(selectTargetButton);

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
        var targetTypeCol = new DataGridViewComboBoxColumn { Name = "TargetType", HeaderText = "Target Type", DataPropertyName = "TargetType" };
        targetTypeCol.Items.AddRange(new[] { "Script", "ML Model" });
        _schedulesGrid.Columns.Add(targetTypeCol);
        _schedulesGrid.Columns.Add("TargetName", "Target Name");
        _schedulesGrid.Columns.Add("Recurrence", "Recurrence");
        _schedulesGrid.Columns.Add("IntervalSeconds", "Interval (s)");
        _schedulesGrid.Columns.Add("StaggerSeconds", "Stagger (s)");
        _schedulesGrid.Columns.Add("Time", "Time");
        _schedulesGrid.Columns.Add("DayOfWeek", "Day of Week");
        _schedulesGrid.Columns.Add("DayOfMonth", "Day of Month");
        _schedulesGrid.Columns.Add("Enabled", "Enabled");
        _schedulesGrid.Columns.Add("Description", "Description");

        var recurrenceColumn = new DataGridViewComboBoxColumn { Name = "Recurrence", HeaderText = "Recurrence", DataPropertyName = "Recurrence" };
        recurrenceColumn.Items.AddRange(new[] { "Once", "Daily", "Weekly", "Monthly", "Interval", "Custom" });
        _schedulesGrid.Columns.Remove("Recurrence");
        _schedulesGrid.Columns.Insert(3, recurrenceColumn);

        var dayOfWeekColumn = new DataGridViewComboBoxColumn { Name = "DayOfWeek", HeaderText = "Day of Week", DataPropertyName = "DayOfWeek" };
        dayOfWeekColumn.Items.AddRange(new[] { "", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
        _schedulesGrid.Columns.Remove("DayOfWeek");
        _schedulesGrid.Columns.Insert(7, dayOfWeekColumn);

        var enabledColumn = new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "Enabled", DataPropertyName = "Enabled" };
        enabledColumn.FalseValue = false;
        enabledColumn.TrueValue = true;
        _schedulesGrid.Columns.Remove("Enabled");
        _schedulesGrid.Columns.Add(enabledColumn);

        _schedulesGrid.CellValueChanged += OnCellValueChanged;
        _schedulesGrid.CellEndEdit += (s, e) => _isModified = true;
        _schedulesGrid.DataError += OnDataError;
        _schedulesGrid.CellFormatting += OnCellFormatting;

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
    }

    /// <summary>
    /// Updates the list of available ML models (Id, Name) for schedule targets.
    /// </summary>
    public void SetAvailableMLModels(List<(string Id, string Name)> models)
    {
        _availableMLModels = models ?? new List<(string, string)>();
    }

    private void LoadSchedules()
    {
        _schedulesGrid.Rows.Clear();
        if (_schedules == null) return;
        foreach (var schedule in _schedules.ScheduleDefinitions)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_schedulesGrid);
            row.Cells[0].Value = schedule.Name;
            row.Cells[1].Value = schedule.TargetType == ScheduleTargetType.Script ? "Script" : "ML Model";
            row.Cells[2].Value = schedule.TargetType == ScheduleTargetType.Script ? schedule.ScriptName : schedule.ModelName;
            row.Cells[3].Value = schedule.Recurrence;
            row.Cells[4].Value = schedule.IntervalSeconds;
            row.Cells[5].Value = schedule.StaggerSeconds;
            row.Cells[6].Value = schedule.Time;
            row.Cells[7].Value = GetDayOfWeekName(schedule.DayOfWeek);
            row.Cells[8].Value = schedule.DayOfMonth > 0 ? schedule.DayOfMonth.ToString() : "";
            row.Cells[9].Value = schedule.Enabled ? true : false;
            row.Cells[10].Value = schedule.Description;
            row.Tag = schedule;
            _schedulesGrid.Rows.Add(row);
        }
    }

    private string GetDayOfWeekName(int dayOfWeek)
    {
        // -1 means not set (empty)
        if (dayOfWeek < 0)
            return "";
        var days = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        return dayOfWeek >= 0 && dayOfWeek < days.Length ? days[dayOfWeek] : "";
    }

    private int GetDayOfWeekIndex(string dayName)
    {
        // Empty string means not set (-1)
        if (string.IsNullOrWhiteSpace(dayName))
            return -1;
        var days = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        var index = Array.IndexOf(days, dayName);
        return index >= 0 ? index : -1;
    }

    private void AddSchedule()
    {
        if (_schedules == null)
            _schedules = new Schedules();

        var newSchedule = new ScheduleDefinition
        {
            Name = $"Schedule_{_schedules.ScheduleDefinitions.Count + 1}",
            TargetType = ScheduleTargetType.Script,
            ScriptName = _availableScripts.FirstOrDefault() ?? string.Empty,
            Recurrence = "Daily",
            Time = DateTime.Now.ToString("HH:mm"),
            IntervalSeconds = 60,
            DayOfWeek = -1, // Not set by default
            DayOfMonth = -1, // Not set by default
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

    private void SelectTargetForSelectedSchedule()
    {
        if (_schedulesGrid.SelectedRows.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("[ScheduleEditor] No schedule selected for target selection");
            return;
        }

        using var dialog = new TargetSelectionDialog(_availableScripts, _availableMLModels);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var selectedRow = _schedulesGrid.SelectedRows[0];
        if (selectedRow.Tag is ScheduleDefinition schedule)
        {
            schedule.TargetType = dialog.SelectedTargetType;
            schedule.ScriptName = dialog.SelectedScriptName;
            schedule.ModelId = dialog.SelectedModelId;
            schedule.ModelName = dialog.SelectedModelName;

            // Update grid display by column name so order is correct
            var targetTypeDisplay = dialog.SelectedTargetType == ScheduleTargetType.Script ? "Script" : "ML Model";
            var targetNameDisplay = dialog.SelectedTargetType == ScheduleTargetType.Script
                ? dialog.SelectedScriptName
                : dialog.SelectedModelName;

            selectedRow.Cells["TargetType"].Value = targetTypeDisplay;
            selectedRow.Cells["TargetName"].Value = targetNameDisplay;

            // If ML Model, default to Interval recurrence
            if (dialog.SelectedTargetType == ScheduleTargetType.MLModel && string.IsNullOrEmpty(schedule.Recurrence))
            {
                schedule.Recurrence = "Interval";
                selectedRow.Cells["Recurrence"].Value = "Interval";
            }

            _schedulesGrid.Refresh();
            _isModified = true;
        }
    }

    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        try
        {
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
                case "TargetType":
                    var newTargetType = string.Equals(value?.ToString(), "ML Model", StringComparison.OrdinalIgnoreCase) ? ScheduleTargetType.MLModel : ScheduleTargetType.Script;
                    // Clear the target name when switching types
                    if (schedule.TargetType != newTargetType)
                    {
                        schedule.TargetType = newTargetType;
                        schedule.ScriptName = "";
                        schedule.ModelId = "";
                        schedule.ModelName = "";
                        row.Cells[2].Value = ""; // Clear TargetName column
                    }
                    else
                    {
                        schedule.TargetType = newTargetType;
                    }
                    break;
                case "TargetName":
                // When TargetName is edited directly, try to match it to either scripts or ML models
                var targetName = value?.ToString() ?? string.Empty;
                if (schedule.TargetType == ScheduleTargetType.Script)
                {
                    schedule.ScriptName = targetName;
                    schedule.ModelId = "";
                    schedule.ModelName = "";
                }
                else
                {
                    schedule.ModelName = targetName;
                    schedule.ScriptName = "";
                    if (!string.IsNullOrEmpty(schedule.ModelName) && _availableMLModels != null)
                    {
                        var match = _availableMLModels.FirstOrDefault(m => string.Equals(m.Name, schedule.ModelName, StringComparison.Ordinal));
                        if (match.Name != null) schedule.ModelId = match.Id;
                    }
                }
                break;
                case "IntervalSeconds":
                    if (int.TryParse(value?.ToString(), out int intervalSec)) schedule.IntervalSeconds = Math.Max(1, intervalSec);
                    else System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Invalid IntervalSeconds value: {value}");
                    break;
                case "StaggerSeconds":
                    if (int.TryParse(value?.ToString(), out int staggerSec)) schedule.StaggerSeconds = Math.Max(0, staggerSec);
                    else System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Invalid StaggerSeconds value: {value}");
                    break;
                case "Recurrence":
                    schedule.Recurrence = value?.ToString() ?? "Once";
                    break;
                case "Time":
                    schedule.Time = value?.ToString() ?? "00:00";
                    break;
                case "DayOfWeek":
                    schedule.DayOfWeek = GetDayOfWeekIndex(value?.ToString() ?? "");
                    break;
                case "DayOfMonth":
                    var dayOfMonthStr = value?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(dayOfMonthStr))
                    {
                        schedule.DayOfMonth = -1; // -1 means not set
                    }
                    else if (int.TryParse(dayOfMonthStr, out int dayOfMonth))
                    {
                        schedule.DayOfMonth = dayOfMonth;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Invalid DayOfMonth value: {value}");
                    }
                    break;
                case "Enabled":
                    if (value is bool enabled)
                        schedule.Enabled = enabled;
                    else if (value is string str && bool.TryParse(str, out bool parsedBool))
                        schedule.Enabled = parsedBool;
                    else
                        schedule.Enabled = true; // Default to true if value is null/empty/invalid
                    break;
                case "Description":
                    schedule.Description = value?.ToString() ?? string.Empty;
                    break;
            }

            _isModified = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Error in OnCellValueChanged at Row {e.RowIndex}, Column {e.ColumnIndex}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Stack trace: {ex.StackTrace}");
        }
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        // Ensure Enabled column always has a valid boolean value
        if (e.ColumnIndex >= 0 && e.ColumnIndex < _schedulesGrid.Columns.Count)
        {
            var column = _schedulesGrid.Columns[e.ColumnIndex];
            if (column.Name == "Enabled")
            {
                if (e.Value == null || e.Value == DBNull.Value || (e.Value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    e.Value = true; // Default to true for empty/null values
                    e.FormattingApplied = true;
                }
                else if (!(e.Value is bool))
                {
                    // Try to convert to bool, default to true if conversion fails
                    if (bool.TryParse(e.Value?.ToString(), out bool result))
                        e.Value = result;
                    else
                        e.Value = true;
                    e.FormattingApplied = true;
                }
            }
        }
    }

    private void OnDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        // Log error to debug terminal instead of showing popup
        var columnName = e.ColumnIndex >= 0 && e.ColumnIndex < _schedulesGrid.Columns.Count
            ? _schedulesGrid.Columns[e.ColumnIndex].Name
            : "Unknown";
        var rowIndex = e.RowIndex >= 0 ? e.RowIndex.ToString() : "Unknown";
        var errorMsg = e.Exception?.Message ?? "Unknown error";
        
        System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] DataGrid error at Row {rowIndex}, Column '{columnName}': {errorMsg}");
        System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Exception: {e.Exception}");
        
        // Handle Boolean conversion errors specifically for Enabled column
        if (columnName == "Enabled" && e.Exception is System.FormatException)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < _schedulesGrid.Rows.Count)
                {
                    var row = _schedulesGrid.Rows[e.RowIndex];
                    var cell = row.Cells[e.ColumnIndex];
                    // Set a default boolean value
                    cell.Value = true;
                    System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Fixed Boolean conversion error by setting Enabled to true");
                }
            }
            catch (Exception fixEx)
            {
                System.Diagnostics.Debug.WriteLine($"[ScheduleEditor] Failed to fix Boolean conversion error: {fixEx.Message}");
            }
        }
        
        // Suppress the default error dialog
        e.ThrowException = false;
    }

    /// <summary>
    /// Gets the current schedules.
    /// </summary>
    public Schedules? GetSchedules()
    {
        return _schedules;
    }

    /// <summary>
    /// Resets the modified flag (called after successful save).
    /// </summary>
    public void ResetModified()
    {
        _isModified = false;
    }
}
