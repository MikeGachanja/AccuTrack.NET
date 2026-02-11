using System;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Project;

/// <summary>
/// Settings editor for project configuration.
/// </summary>
public partial class SettingsEditor : UserControl
{
    private ProjectManager? _projectManager;
    private TextBox _projectNameTextBox;
    private TextBox _descriptionTextBox;
    private TextBox _authorTextBox;
    private TextBox _companyTextBox;
    private TextBox _versionTextBox;
    private Label _createdDateLabel;
    private Label _modifiedDateLabel;

    public SettingsEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(10)
        };

        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Project Name
        mainLayout.Controls.Add(new Label { Text = "Project Name:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = false }, 0, 0);
        _projectNameTextBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        mainLayout.Controls.Add(_projectNameTextBox, 1, 0);

        // Description
        mainLayout.Controls.Add(new Label { Text = "Description:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = false }, 0, 1);
        _descriptionTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 60 };
        mainLayout.Controls.Add(_descriptionTextBox, 1, 1);

        // Author
        mainLayout.Controls.Add(new Label { Text = "Author:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = false }, 0, 2);
        _authorTextBox = new TextBox { Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_authorTextBox, 1, 2);

        // Company
        mainLayout.Controls.Add(new Label { Text = "Company:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = false }, 0, 3);
        _companyTextBox = new TextBox { Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_companyTextBox, 1, 3);

        // Version
        mainLayout.Controls.Add(new Label { Text = "Version:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = false }, 0, 4);
        _versionTextBox = new TextBox { Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_versionTextBox, 1, 4);

        // Created Date
        mainLayout.Controls.Add(new Label { Text = "Created:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = false }, 0, 5);
        _createdDateLabel = new Label { Text = "", Anchor = AnchorStyles.Left, AutoSize = false, Height = 20 };
        mainLayout.Controls.Add(_createdDateLabel, 1, 5);

        // Modified Date
        mainLayout.Controls.Add(new Label { Text = "Modified:", Anchor = AnchorStyles.Left | AnchorStyles.Right, AutoSize = false }, 0, 6);
        _modifiedDateLabel = new Label { Text = "", Anchor = AnchorStyles.Left, AutoSize = false, Height = 20 };
        mainLayout.Controls.Add(_modifiedDateLabel, 1, 6);

        // Save button
        var saveButton = new Button
        {
            Text = "Save",
            Dock = DockStyle.Fill,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        saveButton.Click += OnSaveClicked;
        mainLayout.Controls.Add(saveButton, 1, 7);

        Controls.Add(mainLayout);
        ResumeLayout(false);
    }

    /// <summary>
    /// Sets the project manager for this editor.
    /// </summary>
    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;
        LoadSettings();
    }

    /// <summary>
    /// Loads project settings into the editor.
    /// </summary>
    private void LoadSettings()
    {
        if (_projectManager?.GetCurrentProject() == null)
            return;

        var project = _projectManager.GetCurrentProject();
        var settings = project.Settings;

        _projectNameTextBox.Text = settings.ProjectName;
        _descriptionTextBox.Text = settings.Description;
        _authorTextBox.Text = settings.Author;
        _companyTextBox.Text = settings.Company;
        _versionTextBox.Text = settings.Version;
        _createdDateLabel.Text = settings.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
        _modifiedDateLabel.Text = settings.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Saves the settings.
    /// </summary>
    private void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_projectManager?.GetCurrentProject() == null)
            return;

        var project = _projectManager.GetCurrentProject();
        project.Settings.Description = _descriptionTextBox.Text;
        project.Settings.Author = _authorTextBox.Text;
        project.Settings.Company = _companyTextBox.Text;
        project.Settings.Version = _versionTextBox.Text;
        project.Settings.ModifiedDate = DateTime.Now;

        if (_projectManager.SaveProject())
        {
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadSettings(); // Refresh to show updated modified date
        }
        else
        {
            MessageBox.Show("Failed to save settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
