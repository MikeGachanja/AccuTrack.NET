using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Designer.Modules.Scheduler;

/// <summary>
/// Dialog for selecting a schedule target (Script or ML Model) and the specific item.
/// </summary>
public partial class TargetSelectionDialog : Form
{
    private RadioButton _scriptRadio;
    private RadioButton _mlModelRadio;
    private ListBox _itemsListBox;
    private Button _okButton;
    private Button _cancelButton;
    private List<string> _availableScripts;
    private List<(string Id, string Name)> _availableMLModels;

    public ScheduleTargetType SelectedTargetType { get; private set; } = ScheduleTargetType.Script;
    public string SelectedScriptName { get; private set; } = string.Empty;
    public string SelectedModelId { get; private set; } = string.Empty;
    public string SelectedModelName { get; private set; } = string.Empty;

    public TargetSelectionDialog(List<string> availableScripts, List<(string Id, string Name)> availableMLModels)
    {
        _availableScripts = availableScripts ?? new List<string>();
        _availableMLModels = availableMLModels ?? new List<(string, string)>();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Select Schedule Target";
        Size = new System.Drawing.Size(450, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10)
        };

        // Target type selection
        var typeLabel = new Label
        {
            Text = "Target Type:",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };

        var typePanel = new Panel { Dock = DockStyle.Fill, Height = 50 };
        _scriptRadio = new RadioButton
        {
            Text = "Script",
            Checked = true,
            Location = new System.Drawing.Point(10, 5),
            AutoSize = true
        };
        _mlModelRadio = new RadioButton
        {
            Text = "ML Model",
            Checked = false,
            Location = new System.Drawing.Point(120, 5),
            AutoSize = true
        };
        _scriptRadio.CheckedChanged += OnTargetTypeChanged;
        _mlModelRadio.CheckedChanged += OnTargetTypeChanged;
        typePanel.Controls.Add(_scriptRadio);
        typePanel.Controls.Add(_mlModelRadio);

        // Items label
        var itemsLabel = new Label
        {
            Text = "Select target:",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };

        // Items list
        _itemsListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            SelectionMode = SelectionMode.One
        };
        LoadItemsForCurrentType();

        _itemsListBox.DoubleClick += (s, e) =>
        {
            if (_itemsListBox.SelectedItem != null)
            {
                SetSelectedValues();
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        _okButton = new Button
        {
            Text = "OK",
            Size = new System.Drawing.Size(75, 23)
        };
        _okButton.Click += (s, e) =>
        {
            if (_itemsListBox.SelectedItem != null)
            {
                SetSelectedValues();
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new System.Drawing.Size(75, 23)
        };

        buttonPanel.Controls.Add(_okButton);
        buttonPanel.Controls.Add(_cancelButton);

        mainLayout.Controls.Add(typeLabel, 0, 0);
        mainLayout.Controls.Add(typePanel, 0, 1);
        mainLayout.Controls.Add(itemsLabel, 0, 2);
        mainLayout.Controls.Add(_itemsListBox, 0, 3);
        mainLayout.Controls.Add(buttonPanel, 0, 4);

        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(mainLayout);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        FormClosing += (s, e) =>
        {
            if (DialogResult == DialogResult.OK && _itemsListBox.SelectedItem != null)
                SetSelectedValues();
        };
    }

    private void OnTargetTypeChanged(object? sender, EventArgs e)
    {
        LoadItemsForCurrentType();
    }

    private void LoadItemsForCurrentType()
    {
        _itemsListBox.Items.Clear();
        
        if (_scriptRadio.Checked)
        {
            foreach (var script in _availableScripts)
            {
                _itemsListBox.Items.Add(script);
            }
            if (_itemsListBox.Items.Count > 0)
            {
                _itemsListBox.SelectedIndex = 0;
            }
        }
        else if (_mlModelRadio.Checked)
        {
            foreach (var (id, name) in _availableMLModels)
            {
                _itemsListBox.Items.Add(name);
            }
            if (_itemsListBox.Items.Count > 0)
            {
                _itemsListBox.SelectedIndex = 0;
            }
        }
    }

    private void SetSelectedValues()
    {
        if (_itemsListBox.SelectedItem == null) return;

        if (_scriptRadio.Checked)
        {
            SelectedTargetType = ScheduleTargetType.Script;
            SelectedScriptName = _itemsListBox.SelectedItem.ToString() ?? string.Empty;
            SelectedModelId = string.Empty;
            SelectedModelName = string.Empty;
        }
        else if (_mlModelRadio.Checked)
        {
            SelectedTargetType = ScheduleTargetType.MLModel;
            SelectedModelName = _itemsListBox.SelectedItem.ToString() ?? string.Empty;
            var match = _availableMLModels.FirstOrDefault(m => string.Equals(m.Name, SelectedModelName, StringComparison.Ordinal));
            SelectedModelId = match.Name != null ? match.Id : string.Empty;
            SelectedScriptName = string.Empty;
        }
    }
}
