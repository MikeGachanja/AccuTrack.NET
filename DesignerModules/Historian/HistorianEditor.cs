using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Historian;
using Designer.Modules.TagEngine;
using Designer.Modules.Project;
using Designer.Modules.Components;

namespace Designer.Modules.Historian;

/// <summary>
/// Editor for historian configuration.
/// </summary>
public partial class HistorianEditor : UserControl
{
    private DataGridView _tagsGrid;
    private Historian? _historian;
    private ScadaProject? _scadaProject;
    private List<Tag> _availableTags = new List<Tag>();
    private bool _isModified = false;

    // Settings controls
    private NumericUpDown _loggingIntervalNumeric;
    private ComboBox _storageTypeCombo;
    private TextBox _storagePathTextBox;
    private NumericUpDown _maxStorageSizeNumeric;
    private ComboBox _retentionPolicyCombo;
    private NumericUpDown _retentionDaysNumeric;
    private CheckBox _enabledCheckBox;

    public bool IsModified => _isModified;

    public HistorianEditor()
    {
        InitializeComponent();
    }

    public HistorianEditor(List<Tag> availableTags) : this()
    {
        _availableTags = availableTags ?? new List<Tag>();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(10)
        };

        // Left panel - Settings
        var settingsPanel = new GroupBox
        {
            Text = "Historian Settings",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(5)
        };

        // Logging Interval
        settingsLayout.Controls.Add(new Label { Text = "Logging Interval (seconds):", AutoSize = true }, 0, 0);
        _loggingIntervalNumeric = new NumericUpDown { Minimum = 1, Maximum = 3600, Value = 60, Dock = DockStyle.Fill };
        _loggingIntervalNumeric.ValueChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_loggingIntervalNumeric, 1, 0);

        // Storage Type
        settingsLayout.Controls.Add(new Label { Text = "Storage Type:", AutoSize = true }, 0, 1);
        _storageTypeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _storageTypeCombo.Items.AddRange(new[] { "Database", "File", "Cloud" });
        _storageTypeCombo.SelectedIndex = 0;
        _storageTypeCombo.SelectedIndexChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_storageTypeCombo, 1, 1);

        // Storage Path
        settingsLayout.Controls.Add(new Label { Text = "Storage Path:", AutoSize = true }, 0, 2);
        var pathLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _storagePathTextBox = new TextBox { Dock = DockStyle.Fill, AutoSize = false };
        _storagePathTextBox.TextChanged += (s, e) => _isModified = true;
        var browseButton = new Button { Text = "Browse...", AutoSize = true };
        browseButton.Click += (s, e) =>
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _storagePathTextBox.Text = dialog.SelectedPath;
                }
            }
        };
        pathLayout.Controls.Add(_storagePathTextBox);
        pathLayout.Controls.Add(browseButton);
        settingsLayout.Controls.Add(pathLayout, 1, 2);

        // Max Storage Size
        settingsLayout.Controls.Add(new Label { Text = "Max Storage Size (MB):", AutoSize = true }, 0, 3);
        _maxStorageSizeNumeric = new NumericUpDown { Minimum = 1, Maximum = 100000, Value = 1000, Dock = DockStyle.Fill };
        _maxStorageSizeNumeric.ValueChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_maxStorageSizeNumeric, 1, 3);

        // Retention Policy
        settingsLayout.Controls.Add(new Label { Text = "Retention Policy:", AutoSize = true }, 0, 4);
        _retentionPolicyCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _retentionPolicyCombo.Items.AddRange(new[] { "Days", "Size", "Both" });
        _retentionPolicyCombo.SelectedIndex = 0;
        _retentionPolicyCombo.SelectedIndexChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_retentionPolicyCombo, 1, 4);

        // Retention Days
        settingsLayout.Controls.Add(new Label { Text = "Retention Days:", AutoSize = true }, 0, 5);
        _retentionDaysNumeric = new NumericUpDown { Minimum = 1, Maximum = 3650, Value = 30, Dock = DockStyle.Fill };
        _retentionDaysNumeric.ValueChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_retentionDaysNumeric, 1, 5);

        // Enabled
        _enabledCheckBox = new CheckBox { Text = "Enabled", AutoSize = true };
        _enabledCheckBox.CheckedChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_enabledCheckBox, 0, 6);
        settingsLayout.SetColumnSpan(_enabledCheckBox, 2);

        settingsPanel.Controls.Add(settingsLayout);

        // Right panel - Tags
        var tagsPanel = new GroupBox
        {
            Text = "Tags for Historical Logging",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var tagsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(5)
        };

        // Toolbar
        var toolbar = new ToolStrip
        {
            Dock = DockStyle.Fill
        };

        var addButton = new ToolStripButton("Add Tag");
        addButton.Click += (s, e) => AddTag();
        toolbar.Items.Add(addButton);

        var removeButton = new ToolStripButton("Remove Tag");
        removeButton.Click += (s, e) => RemoveSelectedTag();
        toolbar.Items.Add(removeButton);

        // Tags grid
        _tagsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        _tagsGrid.Columns.Add("TagName", "Tag Name");
        _tagsGrid.Columns.Add("TagTableName", "Tag Table");
        _tagsGrid.Columns.Add("LoggingIntervalSeconds", "Interval (s)");
        _tagsGrid.Columns.Add("Enabled", "Enabled");
        _tagsGrid.Columns.Add("Description", "Description");

        // Make TagName a button column that opens tag selector dialog
        var tagColumn = new DataGridViewButtonColumn
        {
            Name = "TagName",
            HeaderText = "Tag Name",
            Text = "Select Tag...",
            UseColumnTextForButtonValue = false
        };
        _tagsGrid.Columns.Remove("TagName");
        _tagsGrid.Columns.Insert(0, tagColumn);
        _tagsGrid.CellClick += OnTagNameCellClick;
        _tagsGrid.CellFormatting += OnTagNameCellFormatting;

        // Make Enabled a checkbox
        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _tagsGrid.Columns.Remove("Enabled");
        _tagsGrid.Columns.Insert(3, enabledColumn);

        _tagsGrid.CellValueChanged += OnCellValueChanged;

        tagsLayout.Controls.Add(toolbar, 0, 0);
        tagsLayout.Controls.Add(_tagsGrid, 0, 1);
        tagsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        tagsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        tagsPanel.Controls.Add(tagsLayout);

        mainLayout.Controls.Add(settingsPanel, 0, 0);
        mainLayout.Controls.Add(tagsPanel, 1, 0);
        mainLayout.SetRowSpan(settingsPanel, 2);
        mainLayout.SetRowSpan(tagsPanel, 2);
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Sets the historian to edit.
    /// </summary>
    public void SetHistorian(Historian historian)
    {
        _historian = historian;
        LoadHistorian();
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
    /// Updates the list of available tags.
    /// </summary>
    public void SetAvailableTags(List<Tag> tags)
    {
        _availableTags = tags ?? new List<Tag>();
        // Note: Tag selection is now handled by TagSelectorDialog, so no need to update combo box
    }

    private void LoadHistorian()
    {
        if (_historian == null)
        {
            _historian = new Historian();
        }

        // Load settings
        _loggingIntervalNumeric.Value = _historian.LoggingIntervalSeconds;
        _storageTypeCombo.SelectedItem = _historian.StorageType;
        _storagePathTextBox.Text = _historian.StoragePath;
        _maxStorageSizeNumeric.Value = _historian.MaxStorageSizeMB;
        _retentionPolicyCombo.SelectedItem = _historian.RetentionPolicy;
        _retentionDaysNumeric.Value = _historian.RetentionDays;
        _enabledCheckBox.Checked = _historian.Enabled;

        // Load tags
        _tagsGrid.Rows.Clear();
        
        // Ensure columns are properly initialized
        if (_tagsGrid.Columns.Count == 0)
            return;
        
        // Find column indices safely
        int tagNameIndex = -1, tagTableIndex = -1, intervalIndex = -1, enabledIndex = -1, descIndex = -1;
        for (int i = 0; i < _tagsGrid.Columns.Count; i++)
        {
            var col = _tagsGrid.Columns[i];
            switch (col.Name)
            {
                case "TagName": tagNameIndex = i; break;
                case "TagTableName": tagTableIndex = i; break;
                case "LoggingIntervalSeconds": intervalIndex = i; break;
                case "Enabled": enabledIndex = i; break;
                case "Description": descIndex = i; break;
            }
        }
        
        foreach (var tag in _historian.Tags)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_tagsGrid);
            
            // Set values using column indices (safer than names)
            // TagName is a button column, so set the display value
            if (tagNameIndex >= 0 && tagNameIndex < row.Cells.Count)
            {
                var tagNameCell = row.Cells[tagNameIndex];
                if (tagNameCell != null)
                {
                    // For button column, display the tag name or "Select Tag..." if empty
                    tagNameCell.Value = string.IsNullOrEmpty(tag.TagName) ? "Select Tag..." : tag.TagName;
                }
            }
            
            if (tagTableIndex >= 0 && tagTableIndex < row.Cells.Count && row.Cells[tagTableIndex] != null)
                row.Cells[tagTableIndex].Value = tag.TagTableName ?? string.Empty;
            
            if (intervalIndex >= 0 && intervalIndex < row.Cells.Count && row.Cells[intervalIndex] != null)
                row.Cells[intervalIndex].Value = tag.LoggingIntervalSeconds;
            
            if (enabledIndex >= 0 && enabledIndex < row.Cells.Count && row.Cells[enabledIndex] != null)
                row.Cells[enabledIndex].Value = tag.Enabled;
            
            if (descIndex >= 0 && descIndex < row.Cells.Count && row.Cells[descIndex] != null)
                row.Cells[descIndex].Value = tag.Description ?? string.Empty;
            
            row.Tag = tag;
            _tagsGrid.Rows.Add(row);
        }
    }

    private void AddTag()
    {
        if (_historian == null)
            _historian = new Historian();

        var newTag = new HistorianTag
        {
            TagName = _availableTags.FirstOrDefault()?.Name ?? string.Empty,
            Enabled = true,
            LoggingIntervalSeconds = 0 // Use global interval
        };

        _historian.Tags.Add(newTag);
        LoadHistorian();
        _isModified = true;
    }

    private void RemoveSelectedTag()
    {
        if (_tagsGrid.SelectedRows.Count == 0)
            return;

        var selectedRow = _tagsGrid.SelectedRows[0];
        if (selectedRow.Tag is HistorianTag tag && _historian != null)
        {
            _historian.Tags.Remove(tag);
            LoadHistorian();
            _isModified = true;
        }
    }

    private void OnTagNameCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _tagsGrid.Rows.Count)
            return;

        var column = _tagsGrid.Columns[e.ColumnIndex];
        if (column.Name != "TagName")
            return;

        var row = _tagsGrid.Rows[e.RowIndex];
        if (row.Tag is not HistorianTag historianTag)
            return;

        // Get tag tables from the project
        var tagTables = new List<TagTable>();
        if (_scadaProject != null)
        {
            tagTables = _scadaProject.TagTables.OfType<TagTable>().ToList();
        }

        // Open tag selector dialog
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(tagTables);
        dialog.SetSelectedTagName(historianTag.TagName);
        
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedTag != null)
        {
            historianTag.TagName = dialog.SelectedTagName;
            historianTag.TagTableName = dialog.SelectedTagTableName;
            
            // Update the cell display
            var cell = row.Cells[e.ColumnIndex];
            cell.Value = historianTag.TagName;
            
            // Update tag table name cell if it exists
            int tagTableIndex = -1;
            for (int i = 0; i < _tagsGrid.Columns.Count; i++)
            {
                if (_tagsGrid.Columns[i].Name == "TagTableName")
                {
                    tagTableIndex = i;
                    break;
                }
            }
            if (tagTableIndex >= 0 && tagTableIndex < row.Cells.Count)
            {
                row.Cells[tagTableIndex].Value = historianTag.TagTableName;
            }
            
            _isModified = true;
        }
    }

    private void OnTagNameCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _tagsGrid.Rows.Count)
            return;

        var column = _tagsGrid.Columns[e.ColumnIndex];
        if (column.Name != "TagName")
            return;

        var row = _tagsGrid.Rows[e.RowIndex];
        if (row.Tag is HistorianTag tag)
        {
            // Display the tag name in the button, or "Select Tag..." if empty
            if (string.IsNullOrEmpty(tag.TagName))
            {
                e.Value = "Select Tag...";
            }
            else
            {
                e.Value = tag.TagName;
            }
            e.FormattingApplied = true;
        }
    }

    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.RowIndex >= _tagsGrid.Rows.Count)
            return;

        var row = _tagsGrid.Rows[e.RowIndex];
        if (row.Tag is not HistorianTag tag)
            return;

        if (e.ColumnIndex >= _tagsGrid.Columns.Count)
            return;

        var column = _tagsGrid.Columns[e.ColumnIndex];
        if (e.ColumnIndex >= row.Cells.Count)
            return;

        // Skip TagName column as it's handled by button click
        if (column.Name == "TagName")
            return;

        var cell = row.Cells[e.ColumnIndex];
        var value = cell.Value;

        switch (column.Name)
        {
            case "TagTableName":
                tag.TagTableName = value?.ToString() ?? string.Empty;
                break;
            case "LoggingIntervalSeconds":
                if (int.TryParse(value?.ToString(), out int interval))
                    tag.LoggingIntervalSeconds = interval;
                break;
            case "Enabled":
                tag.Enabled = value is bool enabled ? enabled : true;
                break;
            case "Description":
                tag.Description = value?.ToString() ?? string.Empty;
                break;
        }

        _isModified = true;
    }

    /// <summary>
    /// Gets the current historian configuration.
    /// </summary>
    public Historian? GetHistorian()
    {
        if (_historian == null)
            return null;

        // Update settings from controls
        _historian.LoggingIntervalSeconds = (int)_loggingIntervalNumeric.Value;
        _historian.StorageType = _storageTypeCombo.SelectedItem?.ToString() ?? "Database";
        _historian.StoragePath = _storagePathTextBox.Text;
        _historian.MaxStorageSizeMB = (int)_maxStorageSizeNumeric.Value;
        _historian.RetentionPolicy = _retentionPolicyCombo.SelectedItem?.ToString() ?? "Days";
        _historian.RetentionDays = (int)_retentionDaysNumeric.Value;
        _historian.Enabled = _enabledCheckBox.Checked;

        return _historian;
    }
}
