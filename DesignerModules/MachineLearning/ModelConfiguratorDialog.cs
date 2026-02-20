using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Components;
using Designer.Modules.Project;
using Designer.Modules.TagEngine;

namespace Designer.Modules.MachineLearning;

/// <summary>
/// Tabbed dialog to configure a single ML model: Model Selection, Historian Connection, Model Configurations.
/// </summary>
public class ModelConfiguratorDialog : Form
{
    private TabControl _tabControl;
    private TabPage _tabModelSelection;
    private TabPage _tabHistorianConnection;
    private TabPage _tabModelConfigurations;
    private Button _okButton;
    private Button _cancelButton;

    // Tab 1: Model selection
    private TextBox _nameTextBox = null!;
    private ComboBox _categoryCombo = null!;
    private ListBox _modelKindListBox = null!;
    private TextBox _trainedDataPathTextBox = null!;
    private Button _selectTrainedDataButton = null!;

    // Tab 2: Historian connection
    private ListBox _inputTagsListBox = null!;
    private Button _addInputTagButton = null!;
    private Button _removeInputTagButton = null!;
    private TextBox _outputTagTextBox = null!;
    private Button _selectOutputTagButton = null!;

    // Tab 3: Model configurations
    private Panel _tab3ParametersPanel = null!;
    private ComboBox _templateCombo = null!;
    private Button _loadTemplateButton = null!;
    private Button _saveAsTemplateButton = null!;
    private CheckBox _useHistorianTimeSeriesCheck = null!;
    private NumericUpDown _historianTimeRangeMinutesNum = null!;
    private NumericUpDown _historianMaxRowsPerTagNum = null!;
    private CheckBox _saveResultsToDbCheck = null!;
    private Dictionary<string, Control> _paramControls = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
    private List<MLTemplate?> _templateListForCombo = new List<MLTemplate?>();

    private MLModel? _model;
    private ScadaProject? _scadaProject;
    private List<TagTable> _tagTables = new List<TagTable>();
    private List<string> _historianTagNames = new List<string>();

    /// <summary>After ShowDialog, if DialogResult is OK, contains the configured model (existing or new).</summary>
    public MLModel? ResultModel { get; private set; }

    public ModelConfiguratorDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Configure Model";
        Size = new Size(620, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;

        _tabControl = new TabControl { Dock = DockStyle.Fill };
        _tabModelSelection = new TabPage("Model selection");
        _tabHistorianConnection = new TabPage("Historian connection");
        _tabModelConfigurations = new TabPage("Model configurations");

        BuildTab1ModelSelection();
        BuildTab2HistorianConnection();
        BuildTab3ModelConfigurations();

        _tabControl.TabPages.Add(_tabModelSelection);
        _tabControl.TabPages.Add(_tabHistorianConnection);
        _tabControl.TabPages.Add(_tabModelConfigurations);

        Load += OnDialogLoad;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40,
            Padding = new Padding(0, 6, 12, 0)
        };
        _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        _okButton = new Button { Text = "OK", Width = 80 };
        _okButton.Click += OnOkClick;
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_okButton);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        mainLayout.Controls.Add(_tabControl, 0, 0);
        mainLayout.Controls.Add(buttonPanel, 0, 1);

        Controls.Add(mainLayout);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    private void BuildTab1ModelSelection()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        layout.Controls.Add(new Label { Text = "Model name:", AutoSize = true }, 0, 0);
        _nameTextBox = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_nameTextBox, 1, 0);

        layout.Controls.Add(new Label { Text = "Category:", AutoSize = true }, 0, 1);
        _categoryCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _categoryCombo.Items.Add("All");
        foreach (var cat in ModelKindCatalog.GetCategories())
            _categoryCombo.Items.Add(cat);
        _categoryCombo.SelectedIndex = 0;
        _categoryCombo.SelectedIndexChanged += (s, e) => RefreshModelKindList();
        layout.Controls.Add(_categoryCombo, 1, 1);

        layout.Controls.Add(new Label { Text = "Model kind:", AutoSize = true }, 0, 2);
        _modelKindListBox = new ListBox { Dock = DockStyle.Fill, DisplayMember = "DisplayName" };
        _modelKindListBox.SelectedIndexChanged += (s, e) =>
        {
            if (_modelKindListBox.SelectedItem is ModelKind k)
                EnsureModel().ModelKindId = k.Id;
            RefreshTab3Parameters();
        };
        layout.Controls.Add(_modelKindListBox, 1, 2);

        layout.Controls.Add(new Label { Text = "Trained data:", AutoSize = true }, 0, 3);
        var pathPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _trainedDataPathTextBox = new TextBox { ReadOnly = true, Width = 320 };
        _selectTrainedDataButton = new Button { Text = "Select...", AutoSize = true };
        _selectTrainedDataButton.Click += OnSelectTrainedData;
        pathPanel.Controls.Add(_trainedDataPathTextBox);
        pathPanel.Controls.Add(_selectTrainedDataButton);
        layout.Controls.Add(pathPanel, 1, 3);

        RefreshModelKindList();
        _tabModelSelection.Controls.Add(layout);
    }

    private void RefreshModelKindList()
    {
        var filter = _categoryCombo.SelectedIndex <= 0 ? null : _categoryCombo.SelectedItem?.ToString();
        var kinds = ModelKindCatalog.GetKindsByCategory(filter).ToList();
        _modelKindListBox.DataSource = null;
        _modelKindListBox.Items.Clear();
        foreach (var k in kinds)
            _modelKindListBox.Items.Add(k);
        if (_modelKindListBox.Items.Count > 0)
            _modelKindListBox.SelectedIndex = 0;
    }

    private void OnSelectTrainedData(object? sender, EventArgs e)
    {
        var projectPath = _scadaProject?.Paths.MachineLearningPath;
        if (string.IsNullOrEmpty(projectPath))
        {
            MessageBox.Show(this, "No project set. Cannot select trained data.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        using var dialog = new TrainingDataSelectionDialog();
        dialog.SetSystemFolder(ModelKindCatalog.SystemTrainedModelsFolder);
        dialog.SetProjectMachineLearningPath(projectPath);
        dialog.SetSelectedPath(GetModel()?.TrainedDataPath);
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedProjectRelativePath))
        {
            EnsureModel().TrainedDataPath = dialog.SelectedProjectRelativePath;
            _trainedDataPathTextBox.Text = dialog.SelectedProjectRelativePath;
        }
    }

    private void BuildTab2HistorianConnection()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var infoLabel = new Label
        {
            Text = "Tags are read from the Historian and tag engine; only tags configured in Historian are listed.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText
        };
        layout.SetColumnSpan(infoLabel, 2);
        layout.Controls.Add(infoLabel, 0, 0);

        layout.Controls.Add(new Label { Text = "Input tags:", AutoSize = true }, 0, 1);
        var inputPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _inputTagsListBox = new ListBox { Dock = DockStyle.Fill };
        var inputBtnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _addInputTagButton = new Button { Text = "Add...", AutoSize = true };
        _addInputTagButton.Click += OnAddInputTag;
        _removeInputTagButton = new Button { Text = "Remove", AutoSize = true };
        _removeInputTagButton.Click += OnRemoveInputTag;
        inputBtnPanel.Controls.Add(_addInputTagButton);
        inputBtnPanel.Controls.Add(_removeInputTagButton);
        inputPanel.Controls.Add(_inputTagsListBox, 0, 0);
        inputPanel.Controls.Add(inputBtnPanel, 0, 1);
        layout.Controls.Add(inputPanel, 1, 1);

        layout.Controls.Add(new Label { Text = "Output tag:", AutoSize = true }, 0, 2);
        var outputPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _outputTagTextBox = new TextBox { ReadOnly = true, Width = 260 };
        _selectOutputTagButton = new Button { Text = "Select...", AutoSize = true };
        _selectOutputTagButton.Click += OnSelectOutputTag;
        outputPanel.Controls.Add(_outputTagTextBox);
        outputPanel.Controls.Add(_selectOutputTagButton);
        layout.Controls.Add(outputPanel, 1, 2);

        _tabHistorianConnection.Controls.Add(layout);
    }

    private void OnAddInputTag(object? sender, EventArgs e)
    {
        if (_tagTables.Count == 0)
        {
            MessageBox.Show(this, "No tag tables available. Configure tags and Historian first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(_tagTables);
        if (_historianTagNames.Count > 0)
            dialog.SetAllowedTagNames(_historianTagNames);
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedTagName))
        {
            var name = dialog.SelectedTagName;
            if (_inputTagsListBox.Items.Cast<string>().All(t => !string.Equals(t, name, StringComparison.OrdinalIgnoreCase)))
            {
                _inputTagsListBox.Items.Add(name);
            }
        }
    }

    private void OnRemoveInputTag(object? sender, EventArgs e)
    {
        if (_inputTagsListBox.SelectedIndex >= 0)
            _inputTagsListBox.Items.RemoveAt(_inputTagsListBox.SelectedIndex);
    }

    private void OnSelectOutputTag(object? sender, EventArgs e)
    {
        if (_tagTables.Count == 0)
        {
            MessageBox.Show(this, "No tag tables available. Configure tags first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(_tagTables);
        // Output tag: show all tags from all tag tables (not restricted to historian)
        dialog.SetSelectedTagName(_outputTagTextBox.Text);
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedTagName))
            _outputTagTextBox.Text = dialog.SelectedTagName;
    }

    private void BuildTab3ModelConfigurations()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(8) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        layout.Controls.Add(new Label { Text = "Load template:", AutoSize = true }, 0, 0);
        var templatePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _templateCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
        _loadTemplateButton = new Button { Text = "Load", AutoSize = true };
        _loadTemplateButton.Click += OnLoadTemplate;
        templatePanel.Controls.Add(_templateCombo);
        templatePanel.Controls.Add(_loadTemplateButton);
        layout.Controls.Add(templatePanel, 1, 0);

        layout.Controls.Add(new Label { Text = "Save as template:", AutoSize = true }, 0, 1);
        _saveAsTemplateButton = new Button { Text = "Save as template...", AutoSize = true };
        _saveAsTemplateButton.Click += OnSaveAsTemplate;
        layout.Controls.Add(_saveAsTemplateButton, 1, 1);

        var historianGroup = new GroupBox { Text = "Historian & storage", Dock = DockStyle.Fill, Padding = new Padding(6) };
        var historianLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
        historianLayout.Controls.Add(new Label { Text = "Use historian time-series:", AutoSize = true }, 0, 0);
        _useHistorianTimeSeriesCheck = new CheckBox { Text = "Run model from historian data on a timer", AutoSize = true };
        historianLayout.Controls.Add(_useHistorianTimeSeriesCheck, 1, 0);
        historianLayout.Controls.Add(new Label { Text = "Time range (minutes):", AutoSize = true }, 0, 1);
        _historianTimeRangeMinutesNum = new NumericUpDown { Minimum = 1, Maximum = 10080, Value = 60, Width = 80 };
        historianLayout.Controls.Add(_historianTimeRangeMinutesNum, 1, 1);
        historianLayout.Controls.Add(new Label { Text = "Max rows per tag:", AutoSize = true }, 0, 2);
        _historianMaxRowsPerTagNum = new NumericUpDown { Minimum = 10, Maximum = 10000, Value = 500, Width = 80 };
        historianLayout.Controls.Add(_historianMaxRowsPerTagNum, 1, 2);
        historianLayout.Controls.Add(new Label { Text = "Save results to ml.db:", AutoSize = true }, 0, 3);
        _saveResultsToDbCheck = new CheckBox { Text = "Write predictions to database", AutoSize = true };
        historianLayout.Controls.Add(_saveResultsToDbCheck, 1, 3);
        historianGroup.Controls.Add(historianLayout);
        layout.SetColumnSpan(historianGroup, 2);
        layout.Controls.Add(historianGroup, 0, 2);

        layout.Controls.Add(new Label { Text = "Parameters:", AutoSize = true }, 0, 3);
        _tab3ParametersPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        layout.Controls.Add(_tab3ParametersPanel, 1, 3);

        _tabModelConfigurations.Controls.Add(layout);
    }

    private void RefreshTab3Parameters()
    {
        if (_tab3ParametersPanel == null)
            return;
        _tab3ParametersPanel.Controls.Clear();
        _paramControls.Clear();
        var kind = _modelKindListBox.SelectedItem as ModelKind;
        var m = GetModel();
        if (kind == null || kind.ParameterSchema.Count == 0)
        {
            _tab3ParametersPanel.Controls.Add(new Label { Text = "No parameters defined for this model kind.", AutoSize = true, Location = new Point(0, 4) });
            RefreshTemplateCombo();
            return;
        }
        int y = 4;
        foreach (var def in kind.ParameterSchema)
        {
            var label = new Label { Text = def.DisplayName + ":", AutoSize = true, Location = new Point(0, y) };
            _tab3ParametersPanel.Controls.Add(label);
            Control? edit = null;
            object? current = m?.Parameters != null && m.Parameters.TryGetValue(def.Key, out var v) ? v : def.Default;
            switch ((def.Type ?? "").ToLowerInvariant())
            {
                case "choice":
                    var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Location = new Point(180, y - 2) };
                    foreach (var opt in def.Options)
                        combo.Items.Add(opt);
                    if (current != null && def.Options.Count > 0)
                    {
                        var idx = def.Options.FindIndex(o => string.Equals(o, current.ToString(), StringComparison.OrdinalIgnoreCase));
                        combo.SelectedIndex = idx >= 0 ? idx : 0;
                    }
                    if (combo.SelectedIndex < 0 && combo.Items.Count > 0) combo.SelectedIndex = 0;
                    edit = combo;
                    break;
                case "integer":
                case "number":
                    var num = new NumericUpDown { Width = 120, Location = new Point(180, y - 2), DecimalPlaces = def.Type == "number" ? 2 : 0, Minimum = (decimal)(def.Min ?? 0), Maximum = (decimal)(def.Max ?? 1000000) };
                    if (current != null && (current is int || current is long || current is double))
                        num.Value = Convert.ToDecimal(current);
                    else if (def.Default != null)
                        num.Value = Convert.ToDecimal(def.Default);
                    edit = num;
                    break;
                case "boolean":
                    var check = new CheckBox { Location = new Point(180, y), AutoSize = true };
                    check.Checked = current is bool b && b || (current?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
                    edit = check;
                    break;
                default:
                    var txt = new TextBox { Width = 200, Location = new Point(180, y - 2) };
                    txt.Text = current?.ToString() ?? "";
                    edit = txt;
                    break;
            }
            if (edit != null)
            {
                edit.Tag = def.Key;
                _tab3ParametersPanel.Controls.Add(edit);
                _paramControls[def.Key] = edit;
            }
            y += 28;
        }
        RefreshTemplateCombo();
    }

    private void RefreshTemplateCombo()
    {
        _templateCombo.Items.Clear();
        _templateListForCombo.Clear();
        var kindId = _modelKindListBox.SelectedItem is ModelKind k ? k.Id : null;
        var templates = MLTemplateStorage.LoadTemplatesForKind(kindId).ToList();
        _templateCombo.Items.Add("(none)");
        _templateListForCombo.Add(null);
        foreach (var t in templates)
        {
            _templateCombo.Items.Add(t.Name);
            _templateListForCombo.Add(t);
        }
        if (_templateCombo.Items.Count > 0) _templateCombo.SelectedIndex = 0;
    }

    private void OnLoadTemplate(object? sender, EventArgs e)
    {
        var idx = _templateCombo.SelectedIndex;
        if (idx < 0 || idx >= _templateListForCombo.Count) return;
        var template = _templateListForCombo[idx];
        if (template == null) return;
        var m = EnsureModel();
        m.Parameters.Clear();
        foreach (var p in template.Parameters)
            m.Parameters[p.Key] = p.Value;
        RefreshTab3Parameters();
    }

    private void OnSaveAsTemplate(object? sender, EventArgs e)
    {
        var kind = _modelKindListBox.SelectedItem as ModelKind;
        if (kind == null)
        {
            MessageBox.Show(this, "Select a model kind first (Model selection tab).", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var nameDialog = new Form
        {
            Text = "Save as template",
            Size = new Size(320, 100),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog
        };
        var nameBox = new TextBox { Location = new Point(12, 12), Width = 280, Text = "My template" };
        var okBtn = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(120, 38), Width = 80 };
        var cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(208, 38), Width = 80 };
        nameDialog.Controls.Add(nameBox);
        nameDialog.Controls.Add(okBtn);
        nameDialog.Controls.Add(cancelBtn);
        nameDialog.AcceptButton = okBtn;
        nameDialog.CancelButton = cancelBtn;
        if (nameDialog.ShowDialog(this) != DialogResult.OK) return;
        var name = nameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;
        CollectTab3(); // ensure current UI values are in model
        var m = GetModel();
        if (m == null) return;
        var parameters = new Dictionary<string, object>(m.Parameters);
        if (MLTemplateStorage.SaveTemplate(name, kind.Id, parameters))
            MessageBox.Show(this, "Template saved.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        else
            MessageBox.Show(this, "Could not save template.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        RefreshTemplateCombo();
    }

    private void CollectTab3()
    {
        var m = GetModel();
        if (m == null) return;
        foreach (var kv in _paramControls)
        {
            var key = kv.Key;
            var c = kv.Value;
            if (c is ComboBox combo && combo.Tag?.ToString() == key)
                m.Parameters[key] = combo.SelectedItem?.ToString() ?? "";
            else if (c is NumericUpDown num)
                m.Parameters[key] = num.DecimalPlaces > 0 ? (object)num.Value : (object)(int)num.Value;
            else if (c is CheckBox check)
                m.Parameters[key] = check.Checked;
            else if (c is TextBox txt)
                m.Parameters[key] = txt.Text ?? "";
        }
    }

    private void OnDialogLoad(object? sender, EventArgs e)
    {
        if (_model != null)
        {
            _nameTextBox.Text = _model.Name;
            _trainedDataPathTextBox.Text = _model.TrainedDataPath;
            if (!string.IsNullOrEmpty(_model.ModelKindId))
            {
                var kind = ModelKindCatalog.GetById(_model.ModelKindId);
                if (kind != null)
                {
                    var catDisplay = ModelKindCatalog.ToCategoryDisplayName(kind.Category);
                    for (int i = 0; i < _categoryCombo.Items.Count; i++)
                    {
                        if (string.Equals(_categoryCombo.Items[i]?.ToString(), catDisplay, StringComparison.OrdinalIgnoreCase))
                        {
                            _categoryCombo.SelectedIndex = i;
                            break;
                        }
                    }
                    RefreshModelKindList();
                    for (int i = 0; i < _modelKindListBox.Items.Count; i++)
                    {
                        if (_modelKindListBox.Items[i] is ModelKind k && string.Equals(k.Id, _model.ModelKindId, StringComparison.OrdinalIgnoreCase))
                        {
                            _modelKindListBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            _nameTextBox.Text = "NewModel";
            if (_modelKindListBox.Items.Count > 0) _modelKindListBox.SelectedIndex = 0;
        }
        // Tab 2
        _inputTagsListBox.Items.Clear();
        if (_model != null)
        {
            foreach (var t in _model.InputTags)
                if (!string.IsNullOrEmpty(t)) _inputTagsListBox.Items.Add(t);
            _outputTagTextBox.Text = _model.OutputTag ?? "";
        }
        // Tab 3
        _useHistorianTimeSeriesCheck.Checked = _model?.UseHistorianTimeSeries ?? false;
        _historianTimeRangeMinutesNum.Value = Math.Max(1, Math.Min(10080, _model?.HistorianTimeRangeMinutes ?? 60));
        _historianMaxRowsPerTagNum.Value = Math.Max(10, Math.Min(10000, _model?.HistorianMaxRowsPerTag ?? 500));
        _saveResultsToDbCheck.Checked = _model?.SaveResultsToDb ?? false;
        RefreshTab3Parameters();
    }

    /// <summary>Set the model to edit (or null for new model).</summary>
    public void SetModel(MLModel? model)
    {
        _model = model != null ? CloneModel(model) : null;
    }

    /// <summary>Set project context (for trained data path and templates).</summary>
    public void SetScadaProject(ScadaProject? project)
    {
        _scadaProject = project;
    }

    /// <summary>Set tag tables for tag selector.</summary>
    public void SetTagTables(List<TagTable>? tagTables)
    {
        _tagTables = tagTables ?? new List<TagTable>();
    }

    /// <summary>Set historian tag names (only these tags are shown for input/output).</summary>
    public void SetHistorianTagNames(List<string>? historianTagNames)
    {
        _historianTagNames = historianTagNames ?? new List<string>();
    }

    private static MLModel CloneModel(MLModel source)
    {
        var m = new MLModel
        {
            Id = source.Id,
            Name = source.Name,
            ModelKindId = source.ModelKindId,
            ModelType = source.ModelType,
            TrainedDataPath = source.TrainedDataPath,
            OutputTag = source.OutputTag,
            Enabled = source.Enabled
        };
        m.InputTags.AddRange(source.InputTags);
        foreach (var p in source.Parameters)
            m.Parameters[p.Key] = p.Value;
        return m;
    }

    private void OnOkClick(object? sender, EventArgs e)
    {
        if (!ValidateAndCollect())
            return;
        if (_model == null)
        {
            _model = new MLModel
            {
                Name = "NewModel",
                ModelKindId = ModelKindCatalog.FastForestRegressionId,
                ModelType = "Regression",
                Enabled = true
            };
        }
        ResultModel = _model;
        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>Validate and write back to _model. Return false to prevent OK.</summary>
    protected virtual bool ValidateAndCollect()
    {
        var m = EnsureModel();
        m.Name = string.IsNullOrWhiteSpace(_nameTextBox.Text) ? "NewModel" : _nameTextBox.Text.Trim();
        if (_modelKindListBox.SelectedItem is ModelKind kind)
            m.ModelKindId = kind.Id;
        m.TrainedDataPath = _trainedDataPathTextBox.Text?.Trim() ?? "";
        m.UseHistorianTimeSeries = _useHistorianTimeSeriesCheck.Checked;
        m.HistorianTimeRangeMinutes = (int)_historianTimeRangeMinutesNum.Value;
        m.HistorianMaxRowsPerTag = (int)_historianMaxRowsPerTagNum.Value;
        m.SaveResultsToDb = _saveResultsToDbCheck.Checked;
        CollectTab2();
        CollectTab3();
        return true;
    }

    private void CollectTab2()
    {
        var m = GetModel();
        if (m == null) return;
        m.InputTags.Clear();
        foreach (var item in _inputTagsListBox.Items)
            if (item?.ToString() is { } s && !string.IsNullOrWhiteSpace(s))
                m.InputTags.Add(s.Trim());
        m.OutputTag = _outputTagTextBox.Text?.Trim() ?? "";
    }

    /// <summary>Get the current model being edited (may be null for new).</summary>
    protected MLModel? GetModel() => _model;

    /// <summary>Ensure we have a model instance (create default if null).</summary>
    protected MLModel EnsureModel()
    {
        if (_model != null) return _model;
        _model = new MLModel
        {
            Name = "NewModel",
            ModelKindId = ModelKindCatalog.FastForestRegressionId,
            ModelType = "Regression",
            Enabled = true
        };
        return _model;
    }

    protected ScadaProject? GetScadaProject() => _scadaProject;
    protected IReadOnlyList<TagTable> GetTagTables() => _tagTables;
    protected IReadOnlyList<string> GetHistorianTagNames() => _historianTagNames;
}
