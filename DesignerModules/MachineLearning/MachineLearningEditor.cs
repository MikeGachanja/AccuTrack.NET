using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.MachineLearning;
using Designer.Modules.Project;

namespace Designer.Modules.MachineLearning;

/// <summary>
/// Editor for machine learning configuration.
/// </summary>
public partial class MachineLearningEditor : UserControl
{
    private DataGridView _modelsGrid;
    private MachineLearning? _ml;
    private ScadaProject? _scadaProject;
    private List<string> _availableTags = new List<string>();
    private bool _isModified = false;

    private CheckBox _enabledCheckBox;
    private TextBox _trainingDataPathTextBox;
    private NumericUpDown _trainingIntervalNumeric;

    public bool IsModified => _isModified;

    public MachineLearningEditor()
    {
        InitializeComponent();
    }

    public MachineLearningEditor(List<string> availableTags) : this()
    {
        _availableTags = availableTags ?? new List<string>();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };

        // Settings panel
        var settingsPanel = new GroupBox
        {
            Text = "ML Settings",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(5)
        };

        _enabledCheckBox = new CheckBox { Text = "Enable Machine Learning", AutoSize = true };
        _enabledCheckBox.CheckedChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_enabledCheckBox, 0, 0);
        settingsLayout.SetColumnSpan(_enabledCheckBox, 2);

        settingsLayout.Controls.Add(new Label { Text = "Training Data Path:", AutoSize = true }, 0, 1);
        var pathLayout = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _trainingDataPathTextBox = new TextBox { Dock = DockStyle.Fill };
        _trainingDataPathTextBox.TextChanged += (s, e) => _isModified = true;
        var browseButton = new Button { Text = "Browse...", AutoSize = true };
        browseButton.Click += (s, e) =>
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _trainingDataPathTextBox.Text = dialog.SelectedPath;
                }
            }
        };
        pathLayout.Controls.Add(_trainingDataPathTextBox);
        pathLayout.Controls.Add(browseButton);
        settingsLayout.Controls.Add(pathLayout, 1, 1);

        settingsLayout.Controls.Add(new Label { Text = "Training Interval (hours):", AutoSize = true }, 0, 2);
        _trainingIntervalNumeric = new NumericUpDown { Minimum = 1, Maximum = 168, Value = 24, Dock = DockStyle.Fill };
        _trainingIntervalNumeric.ValueChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_trainingIntervalNumeric, 1, 2);

        settingsPanel.Controls.Add(settingsLayout);

        // Models toolbar
        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addButton = new ToolStripButton("Add Model");
        addButton.Click += (s, e) => AddModel();
        toolbar.Items.Add(addButton);
        var removeButton = new ToolStripButton("Remove Model");
        removeButton.Click += (s, e) => RemoveSelectedModel();
        toolbar.Items.Add(removeButton);

        // Models grid
        _modelsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _modelsGrid.Columns.Add("Name", "Name");
        _modelsGrid.Columns.Add("ModelType", "Type");
        _modelsGrid.Columns.Add("InputTags", "Input Tags");
        _modelsGrid.Columns.Add("OutputTag", "Output Tag");
        _modelsGrid.Columns.Add("Enabled", "Enabled");

        var typeColumn = new DataGridViewComboBoxColumn
        {
            Name = "ModelType",
            HeaderText = "Type",
            DataPropertyName = "ModelType"
        };
        typeColumn.Items.AddRange(new[] { "Regression", "Classification", "AnomalyDetection" });
        _modelsGrid.Columns.Remove("ModelType");
        _modelsGrid.Columns.Insert(1, typeColumn);

        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _modelsGrid.Columns.Remove("Enabled");
        _modelsGrid.Columns.Add(enabledColumn);

        _modelsGrid.CellValueChanged += OnCellValueChanged;

        mainLayout.Controls.Add(settingsPanel, 0, 0);
        mainLayout.Controls.Add(toolbar, 0, 1);
        mainLayout.Controls.Add(_modelsGrid, 0, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }

    public void SetMachineLearning(MachineLearning ml)
    {
        _ml = ml;
        LoadML();
        _isModified = false;
    }

    public void SetScadaProject(ScadaProject project)
    {
        _scadaProject = project;
    }

    public void SetAvailableTags(List<string> tags)
    {
        _availableTags = tags ?? new List<string>();
    }

    private void LoadML()
    {
        if (_ml == null)
            _ml = new MachineLearning();

        _enabledCheckBox.Checked = _ml.Enabled;
        _trainingDataPathTextBox.Text = _ml.TrainingDataPath;
        _trainingIntervalNumeric.Value = _ml.TrainingIntervalHours;

        _modelsGrid.Rows.Clear();
        foreach (var model in _ml.Models)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_modelsGrid);
            row.Cells[0].Value = model.Name;
            row.Cells[1].Value = model.ModelType;
            row.Cells[2].Value = string.Join(", ", model.InputTags);
            row.Cells[3].Value = model.OutputTag;
            row.Cells[4].Value = model.Enabled;
            row.Tag = model;
            _modelsGrid.Rows.Add(row);
        }
    }

    private void AddModel()
    {
        if (_ml == null)
            _ml = new MachineLearning();

        var newModel = new MLModel
        {
            Name = $"Model{_ml.Models.Count + 1}",
            ModelType = "Regression",
            Enabled = true
        };

        _ml.Models.Add(newModel);
        LoadML();
        _isModified = true;
    }

    private void RemoveSelectedModel()
    {
        if (_modelsGrid.SelectedRows.Count == 0 || _ml == null)
            return;

        var selectedRow = _modelsGrid.SelectedRows[0];
        if (selectedRow.Tag is MLModel model)
        {
            _ml.Models.Remove(model);
            LoadML();
            _isModified = true;
        }
    }

    private void OnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _modelsGrid.Rows[e.RowIndex];
        if (row.Tag is not MLModel model)
            return;

        var column = _modelsGrid.Columns[e.ColumnIndex];
        var value = row.Cells[e.ColumnIndex].Value;

        switch (column.Name)
        {
            case "Name":
                model.Name = value?.ToString() ?? string.Empty;
                break;
            case "ModelType":
                model.ModelType = value?.ToString() ?? "Regression";
                break;
            case "InputTags":
                var tagsStr = value?.ToString() ?? string.Empty;
                model.InputTags = tagsStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim()).ToList();
                break;
            case "OutputTag":
                model.OutputTag = value?.ToString() ?? string.Empty;
                break;
            case "Enabled":
                model.Enabled = value is bool enabled ? enabled : true;
                break;
        }

        _isModified = true;
    }

    public MachineLearning? GetMachineLearning()
    {
        if (_ml == null)
            return null;

        _ml.Enabled = _enabledCheckBox.Checked;
        _ml.TrainingDataPath = _trainingDataPathTextBox.Text;
        _ml.TrainingIntervalHours = (int)_trainingIntervalNumeric.Value;

        return _ml;
    }

    /// <summary>
    /// Resets the modified flag (called after successful save).
    /// </summary>
    public void ResetModified()
    {
        _isModified = false;
    }
}
