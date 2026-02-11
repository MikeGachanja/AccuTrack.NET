using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer;

/// <summary>
/// Startup page dialog for selecting or creating a project.
/// </summary>
public partial class StartupPage : Form
{
    private DataGridView _projectTable;
    private Button _newProjectButton;
    private Button _openProjectButton;
    private Button _removeButton;
    private Button _openButton;
    private Button _cancelButton;
    private Label _titleLabel;

    private string _selectedProjectPath = string.Empty;
    private bool _createNewProject;
    private bool _openProject;

    public StartupPage()
    {
        _createNewProject = false;
        _openProject = false;
        InitializeComponent();
        LoadRecentProjects();
    }

    public string GetSelectedProjectPath() => _selectedProjectPath;
    public bool ShouldCreateNewProject() => _createNewProject;
    public bool ShouldOpenProject() => _openProject;

    private void InitializeComponent()
    {
        Text = "AccuTrack Designer - Startup";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(900, 500);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20)
        };

        // Title label
        _titleLabel = new Label
        {
            Text = "Welcome to AccuTrack Designer",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 40
        };
        mainLayout.Controls.Add(_titleLabel, 0, 0);

        // Recent projects label
        var recentLabel = new Label
        {
            Text = "Recent Projects:",
            Font = new Font("Segoe UI", 10),
            Dock = DockStyle.Fill,
            AutoSize = false,
            Height = 25
        };
        mainLayout.Controls.Add(recentLabel, 0, 1);

        // Project table
        _projectTable = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(240, 240, 240)
            }
        };

        _projectTable.Columns.Add("ProjectName", "Project Name");
        _projectTable.Columns.Add("Author", "Author");
        _projectTable.Columns.Add("Version", "Version");
        _projectTable.Columns.Add("Path", "Path");

        _projectTable.Columns["ProjectName"].Width = 200;
        _projectTable.Columns["Author"].Width = 150;
        _projectTable.Columns["Version"].Width = 100;
        _projectTable.Columns["Path"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        _projectTable.CellDoubleClick += OnProjectDoubleClicked;
        _projectTable.SelectionChanged += OnProjectSelectionChanged;

        mainLayout.Controls.Add(_projectTable, 0, 2);

        // Buttons layout
        var buttonLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 40
        };

        _newProjectButton = new Button
        {
            Text = "New Project",
            Size = new Size(120, 30),
            Margin = new Padding(5)
        };
        _newProjectButton.Click += OnNewProjectClicked;
        buttonLayout.Controls.Add(_newProjectButton);

        _openProjectButton = new Button
        {
            Text = "Open Project...",
            Size = new Size(120, 30),
            Margin = new Padding(5)
        };
        _openProjectButton.Click += OnOpenProjectClicked;
        buttonLayout.Controls.Add(_openProjectButton);

        buttonLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }); // Spacer

        _removeButton = new Button
        {
            Text = "Remove",
            Size = new Size(100, 30),
            Margin = new Padding(5),
            Enabled = false
        };
        _removeButton.Click += OnRemoveProjectClicked;
        buttonLayout.Controls.Add(_removeButton);

        _openButton = new Button
        {
            Text = "Open",
            Size = new Size(100, 30),
            Margin = new Padding(5),
            Enabled = false
        };
        _openButton.Click += OnOpenButtonClicked;
        buttonLayout.Controls.Add(_openButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Size = new Size(100, 30),
            Margin = new Padding(5)
        };
        _cancelButton.Click += (s, e) => DialogResult = DialogResult.Cancel;
        buttonLayout.Controls.Add(_cancelButton);

        mainLayout.Controls.Add(buttonLayout, 0, 3);

        Controls.Add(mainLayout);
    }

    private void LoadRecentProjects()
    {
        _projectTable.Rows.Clear();

        var recentProjects = ProjectDirectory.GetRecentProjects();

        foreach (string path in recentProjects)
        {
            string displayName;
            string displayPath;

            if (Directory.Exists(path))
            {
                displayName = new DirectoryInfo(path).Name;
                displayPath = Path.GetFullPath(path);
            }
            else if (File.Exists(path))
            {
                displayName = Path.GetFileNameWithoutExtension(path);
                displayPath = Path.GetDirectoryName(path) ?? path;
            }
            else
            {
                continue;
            }

            string author = ProjectDirectory.GetProjectAuthor(path);
            if (string.IsNullOrEmpty(author))
                author = "Unknown";

            string version = ProjectDirectory.GetProjectVersion(path);
            if (string.IsNullOrEmpty(version))
                version = "Unknown";

            int rowIndex = _projectTable.Rows.Add(displayName, author, version, displayPath);
            _projectTable.Rows[rowIndex].Tag = path;
        }

        UpdateButtonStates();
    }

    private void OnNewProjectClicked(object? sender, EventArgs e)
    {
        _createNewProject = true;
        DialogResult = DialogResult.OK;
    }

    private void OnOpenProjectClicked(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open Project",
            Filter = "SCADA Projects (*.isc)|*.isc",
            InitialDirectory = ProjectDirectory.GetDefaultProjectsPath()
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _selectedProjectPath = Path.GetDirectoryName(dialog.FileName) ?? dialog.FileName;
            _openProject = true;
            DialogResult = DialogResult.OK;
        }
    }

    private void OnProjectDoubleClicked(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            var row = _projectTable.Rows[e.RowIndex];
            if (row.Tag is string path)
            {
                _selectedProjectPath = path;
                _openProject = true;
                DialogResult = DialogResult.OK;
            }
        }
    }

    private void OnOpenButtonClicked(object? sender, EventArgs e)
    {
        if (_projectTable.SelectedRows.Count > 0)
        {
            var row = _projectTable.SelectedRows[0];
            if (row.Tag is string path)
            {
                _selectedProjectPath = path;
                _openProject = true;
                DialogResult = DialogResult.OK;
            }
        }
    }

    private void OnRemoveProjectClicked(object? sender, EventArgs e)
    {
        if (_projectTable.SelectedRows.Count == 0)
            return;

        var row = _projectTable.SelectedRows[0];
        if (row.Tag is not string path)
            return;

        string projectName = row.Cells["ProjectName"].Value?.ToString() ?? "this project";

        var result = MessageBox.Show(
            $"Remove '{projectName}' from recent projects list?",
            "Remove Project",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            ProjectDirectory.RemoveRecentProject(path);
            LoadRecentProjects();
        }
    }

    private void OnProjectSelectionChanged(object? sender, EventArgs e)
    {
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = _projectTable.SelectedRows.Count > 0;
        _removeButton.Enabled = hasSelection;
        _openButton.Enabled = hasSelection;
    }
}
