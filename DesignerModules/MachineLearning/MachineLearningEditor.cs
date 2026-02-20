using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Components;
using Designer.Modules.MachineLearning;
using Designer.Modules.Project;
using Designer.Modules.TagEngine;

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
    private List<TagTable> _tagTables = new List<TagTable>();
    private List<string> _historianTagNames = new List<string>();
    private bool _isModified = false;

    private CheckBox _enabledCheckBox;
    private TextBox _trainingDataPathTextBox;
    private NumericUpDown _trainingIntervalNumeric;

    private const int ColName = 0;
    private const int ColModelKind = 1;
    private const int ColTrainedData = 2;
    private const int ColInputTags = 3;
    private const int ColOutputTag = 4;
    private const int ColEnabled = 5;

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
        addButton.ToolTipText = "Add a new model (opens Configure Model dialog)";
        toolbar.Items.Add(addButton);
        var configureButton = new ToolStripButton("Configure...");
        configureButton.Click += (s, e) => ConfigureSelectedModel();
        configureButton.ToolTipText = "Open Configure Model dialog for the selected row";
        toolbar.Items.Add(configureButton);
        var removeButton = new ToolStripButton("Remove Model");
        removeButton.Click += (s, e) => RemoveSelectedModel();
        toolbar.Items.Add(removeButton);
        toolbar.Items.Add(new ToolStripSeparator());
        var setTrainedDataButton = new ToolStripButton("Set trained data...");
        setTrainedDataButton.Click += (s, e) => SetTrainedDataForSelectedModel();
        toolbar.Items.Add(setTrainedDataButton);
        toolbar.Items.Add(new ToolStripSeparator());
        var selectInputTagButton = new ToolStripButton("Select input tag...");
        selectInputTagButton.ToolTipText = "Add a historian-configured tag as input for the selected model";
        selectInputTagButton.Click += (s, e) => SelectInputTagForSelectedModel();
        toolbar.Items.Add(selectInputTagButton);
        var selectOutputTagButton = new ToolStripButton("Select output tag...");
        selectOutputTagButton.ToolTipText = "Set the historian-configured tag for the selected model output";
        selectOutputTagButton.Click += (s, e) => SelectOutputTagForSelectedModel();
        toolbar.Items.Add(selectOutputTagButton);

        // Models grid
        _modelsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _modelsGrid.Columns.Add("Name", "Name");
        var modelKindColumn = new DataGridViewComboBoxColumn
        {
            Name = "ModelKind",
            HeaderText = "Model kind",
            DataPropertyName = "ModelKindId"
        };
        foreach (var kind in ModelKindCatalog.GetAllKinds())
        {
            modelKindColumn.Items.Add(kind.DisplayName);
        }
        _modelsGrid.Columns.Add(modelKindColumn);
        _modelsGrid.Columns.Add("TrainedDataPath", "Trained data");
        _modelsGrid.Columns.Add("InputTags", "Input Tags");
        _modelsGrid.Columns.Add("OutputTag", "Output Tag");
        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _modelsGrid.Columns.Add(enabledColumn);

        _modelsGrid.CellValueChanged += OnCellValueChanged;
        _modelsGrid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ConfigureSelectedModel(); };

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

    /// <summary>Sets tag tables for the tag selector (used with historian tags for ML input/output).</summary>
    public void SetTagTables(List<TagTable> tagTables)
    {
        _tagTables = tagTables ?? new List<TagTable>();
    }

    /// <summary>When set, the tag selector shows only these tags (e.g. tags configured for Historian).</summary>
    public void SetHistorianTagNames(List<string> historianTagNames)
    {
        _historianTagNames = historianTagNames ?? new List<string>();
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
            if (string.IsNullOrEmpty(model.ModelKindId) && model.ModelType == "Regression")
                model.ModelKindId = ModelKindCatalog.FastForestRegressionId;
            var row = new DataGridViewRow();
            row.CreateCells(_modelsGrid);
            row.Cells[ColName].Value = model.Name;
            row.Cells[ColModelKind].Value = GetDisplayNameForModel(model);
            row.Cells[ColTrainedData].Value = model.TrainedDataPath;
            row.Cells[ColInputTags].Value = string.Join(", ", model.InputTags);
            row.Cells[ColOutputTag].Value = model.OutputTag;
            row.Cells[ColEnabled].Value = model.Enabled;
            row.Tag = model;
            _modelsGrid.Rows.Add(row);
        }
    }

    private static string GetDisplayNameForModel(MLModel model)
    {
        if (!string.IsNullOrEmpty(model.ModelKindId))
        {
            var kind = ModelKindCatalog.GetById(model.ModelKindId);
            return kind?.DisplayName ?? model.ModelKindId;
        }
        return model.ModelType switch
        {
            "Classification" => "Classification",
            "AnomalyDetection" => "Anomaly Detection",
            _ => "Fast Forest (Regression)"
        };
    }

    private static string GetModelKindIdFromDisplayName(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return ModelKindCatalog.FastForestRegressionId;
        foreach (var k in ModelKindCatalog.GetAllKinds())
        {
            if (string.Equals(k.DisplayName, displayName, StringComparison.Ordinal))
                return k.Id;
        }
        return displayName;
    }

    private void AddModel()
    {
        if (_ml == null)
            _ml = new MachineLearning();

        using var dialog = new ModelConfiguratorDialog();
        dialog.SetModel(null);
        dialog.SetScadaProject(_scadaProject);
        dialog.SetTagTables(_tagTables);
        dialog.SetHistorianTagNames(_historianTagNames);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultModel != null)
        {
            _ml.Models.Add(dialog.ResultModel);
            LoadML();
            _isModified = true;
        }
    }

    private void ConfigureSelectedModel()
    {
        if (_modelsGrid.SelectedRows.Count == 0 || _modelsGrid.SelectedRows[0].Tag is not MLModel model || _ml == null)
        {
            MessageBox.Show("Select a model row first.", "ML Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new ModelConfiguratorDialog();
        dialog.SetModel(model);
        dialog.SetScadaProject(_scadaProject);
        dialog.SetTagTables(_tagTables);
        dialog.SetHistorianTagNames(_historianTagNames);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.ResultModel != null)
        {
            var idx = _ml.Models.IndexOf(model);
            if (idx >= 0)
            {
                _ml.Models[idx] = dialog.ResultModel;
                LoadML();
                _isModified = true;
            }
        }
    }

    private void SetTrainedDataForSelectedModel()
    {
        if (_modelsGrid.SelectedRows.Count == 0 || _scadaProject == null || _ml == null)
        {
            if (_scadaProject == null)
                MessageBox.Show("No project set.", "ML Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else if (_modelsGrid.SelectedRows.Count == 0)
                MessageBox.Show("Select a model row first.", "ML Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedRow = _modelsGrid.SelectedRows[0];
        if (selectedRow.Tag is not MLModel model)
            return;

        using var dialog = new TrainingDataSelectionDialog();
        dialog.SetSystemFolder(ModelKindCatalog.SystemTrainedModelsFolder);
        dialog.SetProjectMachineLearningPath(_scadaProject.Paths.MachineLearningPath);
        dialog.SetSelectedPath(model.TrainedDataPath);
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(dialog.SelectedProjectRelativePath))
            return;

        model.TrainedDataPath = dialog.SelectedProjectRelativePath;
        if (selectedRow.Index >= 0 && selectedRow.Index < _modelsGrid.Rows.Count)
            _modelsGrid.Rows[selectedRow.Index].Cells[ColTrainedData].Value = model.TrainedDataPath;
        _isModified = true;
    }

    private void SelectInputTagForSelectedModel()
    {
        if (_modelsGrid.SelectedRows.Count == 0 || _modelsGrid.SelectedRows[0].Tag is not MLModel model)
        {
            MessageBox.Show("Select a model row first.", "ML Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_tagTables.Count == 0)
        {
            MessageBox.Show("No tag tables available. Configure tags in the project first.", "ML Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(_tagTables);
        if (_historianTagNames.Count > 0)
            dialog.SetAllowedTagNames(_historianTagNames);
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedTagName))
        {
            if (!model.InputTags.Contains(dialog.SelectedTagName, StringComparer.OrdinalIgnoreCase))
            {
                model.InputTags.Add(dialog.SelectedTagName);
                var idx = _modelsGrid.SelectedRows[0].Index;
                if (idx >= 0 && idx < _modelsGrid.Rows.Count)
                    _modelsGrid.Rows[idx].Cells[ColInputTags].Value = string.Join(", ", model.InputTags);
                _isModified = true;
            }
        }
    }

    private void SelectOutputTagForSelectedModel()
    {
        if (_modelsGrid.SelectedRows.Count == 0 || _modelsGrid.SelectedRows[0].Tag is not MLModel model)
        {
            MessageBox.Show("Select a model row first.", "ML Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_tagTables.Count == 0)
        {
            MessageBox.Show("No tag tables available. Configure tags in the project first.", "ML Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(_tagTables);
        // Output tag: show all tags from all tag tables (not restricted to historian)
        dialog.SetSelectedTagName(model.OutputTag);
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedTagName))
        {
            model.OutputTag = dialog.SelectedTagName;
            var idx = _modelsGrid.SelectedRows[0].Index;
            if (idx >= 0 && idx < _modelsGrid.Rows.Count)
                _modelsGrid.Rows[idx].Cells[ColOutputTag].Value = model.OutputTag;
            _isModified = true;
        }
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
            case "ModelKind":
                model.ModelKindId = GetModelKindIdFromDisplayName(value?.ToString());
                break;
            case "TrainedDataPath":
                model.TrainedDataPath = value?.ToString() ?? string.Empty;
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
