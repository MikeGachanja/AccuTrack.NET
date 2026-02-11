using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.ScriptEditor;

namespace Designer.Modules.Scheduler;

/// <summary>
/// Dialog for selecting a script from available scripts.
/// </summary>
public partial class ScriptSelectionDialog : Form
{
    private ListBox _scriptsListBox;
    private Button _okButton;
    private Button _cancelButton;
    private List<string> _availableScripts;

    public string SelectedScript { get; private set; } = string.Empty;

    public ScriptSelectionDialog(List<string> availableScripts)
    {
        _availableScripts = availableScripts ?? new List<string>();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Select Script";
        Size = new System.Drawing.Size(400, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };

        // Label
        var label = new Label
        {
            Text = "Select a script to schedule:",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };

        // Scripts list
        _scriptsListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            SelectionMode = SelectionMode.One
        };

        foreach (var script in _availableScripts)
        {
            _scriptsListBox.Items.Add(script);
        }

        if (_scriptsListBox.Items.Count > 0)
        {
            _scriptsListBox.SelectedIndex = 0;
        }

        _scriptsListBox.DoubleClick += (s, e) =>
        {
            if (_scriptsListBox.SelectedItem != null)
            {
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
            DialogResult = DialogResult.OK,
            Size = new System.Drawing.Size(75, 23)
        };
        _okButton.Click += (s, e) =>
        {
            if (_scriptsListBox.SelectedItem != null)
            {
                SelectedScript = _scriptsListBox.SelectedItem.ToString() ?? string.Empty;
                DialogResult = DialogResult.OK;
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

        mainLayout.Controls.Add(label, 0, 0);
        mainLayout.Controls.Add(_scriptsListBox, 0, 1);
        mainLayout.Controls.Add(buttonPanel, 0, 2);

        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(mainLayout);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }
}
