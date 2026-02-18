using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Designer.Modules.MachineLearning;

/// <summary>
/// Dialog to select trained model data: choose from models available in the system (and in this project)
/// or import from a folder on the computer. Imported models are stored in the system folder for future use.
/// </summary>
public class TrainingDataSelectionDialog : Form
{
    private ListView _listView;
    private Button _importButton;
    private Button _okButton;
    private Button _cancelButton;
    private Label _infoLabel;
    private string _systemFolder = "";
    private string _projectMachineLearningPath = "";

    private const string SourceSystem = "System";
    private const string SourceProject = "This project";

    /// <summary>Selected project-relative path (e.g. machine_learning/data/Model.zip), or null if cancelled.</summary>
    public string? SelectedProjectRelativePath { get; private set; }

    public TrainingDataSelectionDialog()
    {
        InitializeComponent();
    }

    /// <summary>Sets the system folder where imported models are stored (always available for future use).</summary>
    public void SetSystemFolder(string path)
    {
        _systemFolder = path ?? "";
        RefreshList();
    }

    /// <summary>Sets the project's machine_learning path (for "This project" list and copying system selection into project).</summary>
    public void SetProjectMachineLearningPath(string path)
    {
        _projectMachineLearningPath = path ?? "";
        RefreshList();
    }

    /// <summary>Sets the currently selected path (e.g. to preselect in list).</summary>
    public void SetSelectedPath(string? projectRelativePath)
    {
        if (string.IsNullOrEmpty(projectRelativePath)) return;
        var fileName = Path.GetFileName(projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        foreach (ListViewItem item in _listView.Items)
            if (item.SubItems.Count >= 1 && string.Equals(item.SubItems[0].Text, fileName, StringComparison.OrdinalIgnoreCase))
            {
                item.Selected = true;
                item.EnsureVisible();
                break;
            }
    }

    private void InitializeComponent()
    {
        Text = "Select training data";
        Size = new System.Drawing.Size(600, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _infoLabel = new Label
        {
            AutoSize = true,
            Text = "Models available in the system and in this project. Use \"Import from computer...\" to add a trained model so it is always available for future use."
        };
        mainLayout.Controls.Add(_infoLabel, 0, 0);

        _listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false
        };
        _listView.Columns.Add("Name", 220);
        _listView.Columns.Add("Source", 100);
        _listView.Columns.Add("Size", 80);
        _listView.DoubleClick += (s, e) => TryAcceptSelection();
        mainLayout.Controls.Add(_listView, 0, 1);

        var importPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _importButton = new Button { Text = "Import from computer...", AutoSize = true };
        _importButton.Click += OnImportFromComputer;
        importPanel.Controls.Add(_importButton);
        mainLayout.Controls.Add(importPanel, 0, 2);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        _okButton = new Button { Text = "OK", Width = 80 };
        _okButton.Click += (s, e) => TryAcceptSelection();
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_okButton);
        mainLayout.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(mainLayout);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    private void RefreshList()
    {
        _listView.Items.Clear();
        var items = new List<ListViewItem>();

        if (!string.IsNullOrEmpty(_systemFolder) && Directory.Exists(_systemFolder))
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(_systemFolder, "*.*")
                    .Where(p => p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".mlnet", StringComparison.OrdinalIgnoreCase)))
                {
                    var name = Path.GetFileName(path);
                    var len = new FileInfo(path).Length;
                    var item = new ListViewItem(name) { Tag = (SourceSystem, path) };
                    item.SubItems.Add(SourceSystem);
                    item.SubItems.Add(FormatSize(len));
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrainingDataSelection] System folder: {ex.Message}");
            }
        }

        var projectDataPath = string.IsNullOrEmpty(_projectMachineLearningPath) ? null : Path.Combine(_projectMachineLearningPath, "data");
        if (!string.IsNullOrEmpty(projectDataPath) && Directory.Exists(projectDataPath))
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(projectDataPath, "*.*")
                    .Where(p => p.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".mlnet", StringComparison.OrdinalIgnoreCase)))
                {
                    var name = Path.GetFileName(path);
                    if (items.Any(i => string.Equals(i.Text, name, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    var len = new FileInfo(path).Length;
                    var item = new ListViewItem(name) { Tag = (SourceProject, path) };
                    item.SubItems.Add(SourceProject);
                    item.SubItems.Add(FormatSize(len));
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrainingDataSelection] Project data folder: {ex.Message}");
            }
        }

        foreach (var item in items.OrderBy(i => i.Text, StringComparer.OrdinalIgnoreCase))
            _listView.Items.Add(item);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    private void OnImportFromComputer(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select trained model file to import",
            Filter = "ML model (*.zip;*.mlnet)|*.zip;*.mlnet|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        if (string.IsNullOrEmpty(_systemFolder))
        {
            MessageBox.Show(this, "System folder is not set.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (!Directory.Exists(_systemFolder))
                Directory.CreateDirectory(_systemFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not create system folder: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var fileName = Path.GetFileName(dlg.FileName);
        if (string.IsNullOrEmpty(fileName))
            fileName = Path.GetFileNameWithoutExtension(dlg.FileName) + ".zip";
        var destPath = Path.Combine(_systemFolder, fileName);
        if (File.Exists(destPath))
        {
            var overwrite = MessageBox.Show(this, $"A model named \"{fileName}\" already exists. Overwrite?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (overwrite != DialogResult.Yes)
                return;
        }

        try
        {
            File.Copy(dlg.FileName, destPath, true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not import file: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        RefreshList();
        foreach (ListViewItem item in _listView.Items)
            if (string.Equals(item.Text, fileName, StringComparison.OrdinalIgnoreCase))
            {
                item.Selected = true;
                item.EnsureVisible();
                break;
            }
    }

    private void TryAcceptSelection()
    {
        if (_listView.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "Select a model from the list, or import one from your computer.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _listView.SelectedItems[0];
        if (item.Tag is not ValueTuple<string, string> tagData)
            return;
        var (source, fullPath) = tagData;

        var fileName = Path.GetFileName(fullPath);
        var projectRelativePath = Path.Combine("machine_learning", "data", fileName).Replace('\\', '/');

        if (string.Equals(source, SourceSystem, StringComparison.Ordinal))
        {
            var projectDataPath = Path.Combine(_projectMachineLearningPath, "data");
            if (string.IsNullOrEmpty(_projectMachineLearningPath))
            {
                MessageBox.Show(this, "Project path is not set. Cannot use system model.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                if (!Directory.Exists(projectDataPath))
                    Directory.CreateDirectory(projectDataPath);
                var destPath = Path.Combine(projectDataPath, fileName);
                File.Copy(fullPath, destPath, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not copy model into project: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        SelectedProjectRelativePath = projectRelativePath;
        DialogResult = DialogResult.OK;
        Close();
    }
}
