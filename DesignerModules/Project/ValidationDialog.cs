using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Designer.Modules.Project;

/// <summary>
/// Dialog for displaying validation results.
/// </summary>
public partial class ValidationDialog : Form
{
    private ListBox _errorsListBox;
    private ListBox _warningsListBox;
    private TabControl _resultsTabs;

    public ValidationDialog(List<string> errors, List<string> warnings)
    {
        InitializeComponent();
        LoadResults(errors, warnings);
    }

    private void InitializeComponent()
    {
        Text = "Project Validation Results";
        Size = new System.Drawing.Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _resultsTabs = new TabControl { Dock = DockStyle.Fill };

        // Errors tab
        var errorsTab = new TabPage($"Errors ({0})");
        _errorsListBox = new ListBox { Dock = DockStyle.Fill };
        errorsTab.Controls.Add(_errorsListBox);
        _resultsTabs.TabPages.Add(errorsTab);

        // Warnings tab
        var warningsTab = new TabPage($"Warnings ({0})");
        _warningsListBox = new ListBox { Dock = DockStyle.Fill };
        warningsTab.Controls.Add(_warningsListBox);
        _resultsTabs.TabPages.Add(warningsTab);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            Padding = new Padding(10)
        };

        mainLayout.Controls.Add(_resultsTabs, 0, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };

        var closeButton = new Button { Text = "Close", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
        buttonPanel.Controls.Add(closeButton);

        mainLayout.Controls.Add(buttonPanel, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        Controls.Add(mainLayout);

        AcceptButton = closeButton;
    }

    private void LoadResults(List<string> errors, List<string> warnings)
    {
        _errorsListBox.Items.Clear();
        _warningsListBox.Items.Clear();

        foreach (var error in errors)
        {
            _errorsListBox.Items.Add(error);
        }

        foreach (var warning in warnings)
        {
            _warningsListBox.Items.Add(warning);
        }

        // Update tab titles with counts
        if (_resultsTabs.TabPages.Count >= 1)
        {
            _resultsTabs.TabPages[0].Text = $"Errors ({errors.Count})";
        }
        if (_resultsTabs.TabPages.Count >= 2)
        {
            _resultsTabs.TabPages[1].Text = $"Warnings ({warnings.Count})";
        }

        // Select appropriate tab
        if (errors.Count > 0)
        {
            _resultsTabs.SelectedIndex = 0;
        }
        else if (warnings.Count > 0)
        {
            _resultsTabs.SelectedIndex = 1;
        }
    }
}
