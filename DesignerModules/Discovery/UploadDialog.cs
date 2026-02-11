using System;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Discovery;

/// <summary>
/// Dialog for uploading project files to target devices.
/// </summary>
public partial class UploadDialog : Form
{
    private ComboBox _sourceDeviceCombo;
    private ComboBox _targetDeviceCombo;
    private TextBox _sourcePathTextBox;
    private TextBox _targetPathTextBox;
    private CheckBox _overwriteExistingCheckBox;
    private ProgressBar _progressBar;
    private Label _statusLabel;
    private Button _uploadButton;
    private Button _cancelButton;

    public event EventHandler<UploadRequestedEventArgs>? UploadRequested;

    public UploadDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Upload Files";
        Size = new System.Drawing.Size(500, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(10)
        };

        int row = 0;

        // Source Device
        mainLayout.Controls.Add(new Label { Text = "Source Device:", AutoSize = true }, 0, row);
        _sourceDeviceCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _sourceDeviceCombo.Items.AddRange(new[] { "Local", "Remote Device 1", "Remote Device 2" });
        _sourceDeviceCombo.SelectedIndex = 0;
        mainLayout.Controls.Add(_sourceDeviceCombo, 1, row++);

        // Source Path
        mainLayout.Controls.Add(new Label { Text = "Source Path:", AutoSize = true }, 0, row);
        var sourcePathLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _sourcePathTextBox = new TextBox { Dock = DockStyle.Fill };
        var sourceBrowseButton = new Button { Text = "Browse...", AutoSize = true };
        sourceBrowseButton.Click += (s, e) =>
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _sourcePathTextBox.Text = dialog.SelectedPath;
                }
            }
        };
        sourcePathLayout.Controls.Add(_sourcePathTextBox);
        sourcePathLayout.Controls.Add(sourceBrowseButton);
        mainLayout.Controls.Add(sourcePathLayout, 1, row++);

        // Target Device
        mainLayout.Controls.Add(new Label { Text = "Target Device:", AutoSize = true }, 0, row);
        _targetDeviceCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _targetDeviceCombo.Items.AddRange(new[] { "Local", "Remote Device 1", "Remote Device 2", "Cloud" });
        _targetDeviceCombo.SelectedIndex = 0;
        mainLayout.Controls.Add(_targetDeviceCombo, 1, row++);

        // Target Path
        mainLayout.Controls.Add(new Label { Text = "Target Path:", AutoSize = true }, 0, row);
        var targetPathLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _targetPathTextBox = new TextBox { Dock = DockStyle.Fill };
        var targetBrowseButton = new Button { Text = "Browse...", AutoSize = true };
        targetBrowseButton.Click += (s, e) =>
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _targetPathTextBox.Text = dialog.SelectedPath;
                }
            }
        };
        targetPathLayout.Controls.Add(_targetPathTextBox);
        targetPathLayout.Controls.Add(targetBrowseButton);
        mainLayout.Controls.Add(targetPathLayout, 1, row++);

        // Options
        _overwriteExistingCheckBox = new CheckBox { Text = "Overwrite existing files", AutoSize = true, Checked = true };
        mainLayout.Controls.Add(_overwriteExistingCheckBox, 0, row);
        mainLayout.SetColumnSpan(_overwriteExistingCheckBox, 2);
        row++;

        // Progress
        _progressBar = new ProgressBar { Dock = DockStyle.Fill, Style = ProgressBarStyle.Continuous };
        mainLayout.Controls.Add(_progressBar, 0, row);
        mainLayout.SetColumnSpan(_progressBar, 2);
        row++;

        _statusLabel = new Label { Text = "Ready to upload", AutoSize = true };
        mainLayout.Controls.Add(_statusLabel, 0, row);
        mainLayout.SetColumnSpan(_statusLabel, 2);
        row++;

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        _uploadButton = new Button
        {
            Text = "Upload",
            Size = new System.Drawing.Size(75, 23)
        };
        _uploadButton.Click += OnUploadClick;

        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new System.Drawing.Size(75, 23)
        };

        buttonPanel.Controls.Add(_uploadButton);
        buttonPanel.Controls.Add(_cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, row);
        mainLayout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(mainLayout);

        CancelButton = _cancelButton;
    }

    private void OnUploadClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_sourcePathTextBox.Text))
        {
            MessageBox.Show("Please specify a source path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrEmpty(_targetPathTextBox.Text))
        {
            MessageBox.Show("Please specify a target path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _uploadButton.Enabled = false;
        _progressBar.Value = 0;
        _statusLabel.Text = "Uploading...";

        // Simulate upload
        var timer = new System.Windows.Forms.Timer { Interval = 100 };
        int progress = 0;
        timer.Tick += (s, args) =>
        {
            progress += 5;
            _progressBar.Value = Math.Min(progress, 100);
            
            if (progress >= 100)
            {
                timer.Stop();
                _statusLabel.Text = "Upload completed!";
                _uploadButton.Enabled = true;
                
                UploadRequested?.Invoke(this, new UploadRequestedEventArgs
                {
                    SourceDevice = _sourceDeviceCombo.SelectedItem?.ToString() ?? "Local",
                    TargetDevice = _targetDeviceCombo.SelectedItem?.ToString() ?? "Local",
                    SourcePath = _sourcePathTextBox.Text,
                    TargetPath = _targetPathTextBox.Text,
                    OverwriteExisting = _overwriteExistingCheckBox.Checked
                });

                MessageBox.Show("Upload completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
            }
        };
        timer.Start();
    }
}

/// <summary>
/// Event arguments for upload request.
/// </summary>
public class UploadRequestedEventArgs : EventArgs
{
    public string SourceDevice { get; set; } = string.Empty;
    public string TargetDevice { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public bool OverwriteExisting { get; set; }
}
