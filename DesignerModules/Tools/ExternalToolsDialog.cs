using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace Designer.Modules.Tools;

/// <summary>
/// Dialog for configuring external tools.
/// </summary>
public partial class ExternalToolsDialog : Form
{
    private ListBox _toolsList;
    private Button _addButton;
    private Button _editButton;
    private Button _removeButton;
    private List<ExternalTool> _tools = new List<ExternalTool>();

    public ExternalToolsDialog()
    {
        InitializeComponent();
        LoadTools();
    }

    private void InitializeComponent()
    {
        Text = "External Tools";
        Size = new Size(600, 400);
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

        // Info label
        var infoLabel = new Label
        {
            Text = "Configure external tools to run from the Tools menu.",
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 30
        };
        mainLayout.Controls.Add(infoLabel, 0, 0);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        // Tools list
        _toolsList = new ListBox
        {
            Dock = DockStyle.Fill,
            DisplayMember = "DisplayName"
        };
        _toolsList.SelectedIndexChanged += (s, e) =>
        {
            bool hasSelection = _toolsList.SelectedIndex >= 0;
            _editButton.Enabled = hasSelection;
            _removeButton.Enabled = hasSelection;
        };
        _toolsList.DoubleClick += (s, e) => OnEditTool();
        mainLayout.Controls.Add(_toolsList, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 35
        };

        _addButton = new Button { Text = "Add...", Width = 80 };
        _addButton.Click += (s, e) => OnAddTool();
        _editButton = new Button { Text = "Edit...", Width = 80, Enabled = false };
        _editButton.Click += (s, e) => OnEditTool();
        _removeButton = new Button { Text = "Remove", Width = 80, Enabled = false };
        _removeButton.Click += (s, e) => OnRemoveTool();

        buttonPanel.Controls.Add(_addButton);
        buttonPanel.Controls.Add(_editButton);
        buttonPanel.Controls.Add(_removeButton);
        buttonPanel.Controls.Add(new Panel { Dock = DockStyle.Fill }); // Spacer

        mainLayout.Controls.Add(buttonPanel, 0, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

        // Dialog buttons
        var dialogButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 23) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 23) };
        
        dialogButtonPanel.Controls.Add(okButton);
        dialogButtonPanel.Controls.Add(cancelButton);

        var outerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        outerLayout.Controls.Add(mainLayout, 0, 0);
        outerLayout.Controls.Add(dialogButtonPanel, 0, 1);
        outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        Controls.Add(outerLayout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void OnAddTool()
    {
        using (var dialog = new ExternalToolEditDialog())
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var tool = dialog.GetTool();
                _tools.Add(tool);
                RefreshToolsList();
            }
        }
    }

    private void OnEditTool()
    {
        if (_toolsList.SelectedIndex < 0)
            return;

        var tool = _tools[_toolsList.SelectedIndex];
        using (var dialog = new ExternalToolEditDialog(tool))
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _tools[_toolsList.SelectedIndex] = dialog.GetTool();
                RefreshToolsList();
            }
        }
    }

    private void OnRemoveTool()
    {
        if (_toolsList.SelectedIndex < 0)
            return;

        var tool = _tools[_toolsList.SelectedIndex];
        var result = MessageBox.Show(
            $"Are you sure you want to remove '{tool.Name}'?",
            "Remove Tool",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _tools.RemoveAt(_toolsList.SelectedIndex);
            RefreshToolsList();
        }
    }

    private void RefreshToolsList()
    {
        _toolsList.DataSource = null;
        _toolsList.DataSource = _tools;
        _toolsList.DisplayMember = "DisplayName";
    }

    private void LoadTools()
    {
        // Load from settings (can be implemented with Application.UserAppDataPath + settings.json)
        // For now, start with empty list
        RefreshToolsList();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            SaveTools();
        }
        base.OnFormClosing(e);
    }

    private void SaveTools()
    {
        // Save to settings (can be implemented with Application.UserAppDataPath + settings.json)
        // For now, just store in memory
    }

    public List<ExternalTool> GetTools() => new List<ExternalTool>(_tools);
}

/// <summary>
/// Represents an external tool configuration.
/// </summary>
public class ExternalTool
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public bool UseOutputWindow { get; set; } = true;

    public string DisplayName => string.IsNullOrEmpty(Name) ? Command : Name;
}

/// <summary>
/// Dialog for editing an external tool.
/// </summary>
public class ExternalToolEditDialog : Form
{
    private TextBox _nameTextBox;
    private TextBox _commandTextBox;
    private Button _browseButton;
    private TextBox _argumentsTextBox;
    private TextBox _workingDirTextBox;
    private Button _browseDirButton;
    private CheckBox _useOutputWindowCheckBox;
    private ExternalTool _tool;

    public ExternalToolEditDialog(ExternalTool? tool = null)
    {
        _tool = tool ?? new ExternalTool();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = _tool.Name == string.Empty ? "Add External Tool" : "Edit External Tool";
        Size = new Size(500, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 6,
            Padding = new Padding(10)
        };

        int row = 0;

        // Name
        layout.Controls.Add(new Label { Text = "Name:", AutoSize = true }, 0, row);
        _nameTextBox = new TextBox { Text = _tool.Name, Dock = DockStyle.Fill };
        layout.Controls.Add(_nameTextBox, 1, row);
        layout.SetColumnSpan(_nameTextBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Command
        layout.Controls.Add(new Label { Text = "Command:", AutoSize = true }, 0, row);
        _commandTextBox = new TextBox { Text = _tool.Command, Dock = DockStyle.Fill };
        layout.Controls.Add(_commandTextBox, 1, row);
        _browseButton = new Button { Text = "Browse...", Width = 80 };
        _browseButton.Click += (s, e) =>
        {
            using (var dialog = new OpenFileDialog { Filter = "Executable Files|*.exe;*.bat;*.cmd|All Files|*.*" })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _commandTextBox.Text = dialog.FileName;
                }
            }
        };
        layout.Controls.Add(_browseButton, 2, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Arguments
        layout.Controls.Add(new Label { Text = "Arguments:", AutoSize = true }, 0, row);
        _argumentsTextBox = new TextBox { Text = _tool.Arguments, Dock = DockStyle.Fill };
        layout.Controls.Add(_argumentsTextBox, 1, row);
        layout.SetColumnSpan(_argumentsTextBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Working Directory
        layout.Controls.Add(new Label { Text = "Working Directory:", AutoSize = true }, 0, row);
        _workingDirTextBox = new TextBox { Text = _tool.WorkingDirectory, Dock = DockStyle.Fill };
        layout.Controls.Add(_workingDirTextBox, 1, row);
        _browseDirButton = new Button { Text = "Browse...", Width = 80 };
        _browseDirButton.Click += (s, e) =>
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _workingDirTextBox.Text = dialog.SelectedPath;
                }
            }
        };
        layout.Controls.Add(_browseDirButton, 2, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Use Output Window
        _useOutputWindowCheckBox = new CheckBox { Text = "Use Output Window", Checked = _tool.UseOutputWindow };
        layout.Controls.Add(_useOutputWindowCheckBox, 0, row);
        layout.SetColumnSpan(_useOutputWindowCheckBox, 3);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 23) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 23) };
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        layout.Controls.Add(buttonPanel, 0, row);
        layout.SetColumnSpan(buttonPanel, 3);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Controls.Add(layout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public ExternalTool GetTool()
    {
        return new ExternalTool
        {
            Name = _nameTextBox.Text,
            Command = _commandTextBox.Text,
            Arguments = _argumentsTextBox.Text,
            WorkingDirectory = _workingDirTextBox.Text,
            UseOutputWindow = _useOutputWindowCheckBox.Checked
        };
    }
}
