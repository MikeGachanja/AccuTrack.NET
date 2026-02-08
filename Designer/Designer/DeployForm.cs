using AccuTrack.Discovery;
using AccuTrack.Compiler;
using AccuTrack.Project;

namespace Designer;

/// <summary>
/// Deploy to Device dialog: scan for Runtime devices (UDP discovery), select one, deploy build output via HTTP.
/// </summary>
public sealed class DeployForm : Form
{
    private readonly ProjectManager _projectManager;
    private readonly BuildService _buildService;
    private readonly DeviceDiscovery _discovery = new();
    private readonly ProjectTransferClient _transfer = new();
    private DataGridView? _deviceGrid;
    private Button? _scanButton;
    private Button? _deployButton;
    private Label? _statusLabel;
    private ProgressBar? _progressBar;
    private readonly BindingSource _deviceBinding = new();
    private List<DeviceInfo> _devices = new();
    private bool _scanning;

    public DeployForm(ProjectManager projectManager, BuildService buildService)
    {
        _projectManager = projectManager;
        _buildService = buildService;
        Text = "Deploy to Device";
        Size = new Size(520, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        _deviceBinding.DataSource = _devices;
        BuildUi();
        WireEvents();
    }

    private void BuildUi()
    {
        var mainPanel = new Panel { Dock = DockStyle.Fill };

        var projectLabel = new Label { Text = "Project:", Location = new Point(12, 12), AutoSize = true };
        var projectNameLabel = new Label
        {
            Text = _projectManager.CurrentProject?.Name ?? "(No project open)",
            Location = new Point(80, 12),
            AutoSize = true
        };
        mainPanel.Controls.Add(projectLabel);
        mainPanel.Controls.Add(projectNameLabel);

        _scanButton = new Button
        {
            Text = "Scan for Devices",
            Location = new Point(12, 40),
            Size = new Size(120, 28)
        };
        mainPanel.Controls.Add(_scanButton);

        var deviceListLabel = new Label { Text = "Available devices:", Location = new Point(12, 78), AutoSize = true };
        mainPanel.Controls.Add(deviceListLabel);

        _deviceGrid = new DataGridView
        {
            Location = new Point(12, 98),
            Size = new Size(470, 160),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            DataSource = _deviceBinding
        };
        _deviceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeviceName", HeaderText = "Device", Width = 180 });
        _deviceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Ip", HeaderText = "IP", Width = 120 });
        _deviceGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Port", HeaderText = "Port", Width = 60 });
        _deviceGrid.SelectionChanged += (_, _) => UpdateDeployButton();
        mainPanel.Controls.Add(_deviceGrid);

        _progressBar = new ProgressBar
        {
            Location = new Point(12, 268),
            Size = new Size(470, 20),
            Anchor = AnchorStyles.Bottom,
            Visible = false,
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Maximum = 100,
            Value = 0
        };
        mainPanel.Controls.Add(_progressBar);

        _statusLabel = new Label
        {
            Text = "Select a device and click Deploy.",
            Location = new Point(12, 296),
            AutoSize = true,
            Anchor = AnchorStyles.Bottom
        };
        mainPanel.Controls.Add(_statusLabel);

        var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40, Padding = new Padding(8) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(80, 28) };
        _deployButton = new Button { Text = "Deploy", Size = new Size(80, 28), Enabled = false };
        _deployButton.Click += OnDeployClicked;
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(_deployButton);
        mainPanel.Controls.Add(buttonPanel);

        CancelButton = cancelButton;
        AcceptButton = _deployButton;
        Controls.Add(mainPanel);
    }

    private void WireEvents()
    {
        _scanButton!.Click += (_, _) =>
        {
            if (_scanning)
            {
                _discovery.StopScanning();
                _scanning = false;
                _scanButton.Text = "Scan for Devices";
                _statusLabel!.Text = "Scanning stopped.";
            }
            else
            {
                _devices.Clear();
                _deviceBinding.ResetBindings(false);
                _discovery.StartScanning();
                _scanning = true;
                _scanButton.Text = "Stop Scan";
                _statusLabel!.Text = "Scanning for devices...";
            }
        };
        _discovery.DeviceFound += OnDeviceFound;
        _discovery.DeviceLost += OnDeviceLost;
        _discovery.DeviceUpdated += OnDeviceUpdated;
        _transfer.TransferProgress += (_, pct) =>
        {
            if (IsDisposed || _progressBar == null) return;
            if (InvokeRequired) BeginInvoke(() => { _progressBar.Value = Math.Min(100, pct); _progressBar.Visible = true; _statusLabel!.Text = $"Deploying... {pct}%"; });
            else { _progressBar.Value = Math.Min(100, pct); _progressBar.Visible = true; _statusLabel!.Text = $"Deploying... {pct}%"; }
        };
        _transfer.TransferComplete += (_, t) =>
        {
            if (IsDisposed) return;
            void Update()
            {
                _progressBar!.Visible = false;
                _deployButton!.Enabled = !_transfer.IsTransferring;
                _statusLabel!.Text = t.success ? "Deployment completed successfully." : "Deployment failed.";
                if (t.success)
                    MessageBox.Show(this, t.message, "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(this, t.message, "Deploy Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (InvokeRequired) BeginInvoke(Update); else Update();
        };
        _transfer.ErrorOccurred += (_, msg) =>
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(() => { _statusLabel!.Text = msg; MessageBox.Show(this, msg, "Deploy Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); });
            else { _statusLabel!.Text = msg; MessageBox.Show(this, msg, "Deploy Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        FormClosing += (_, e) =>
        {
            if (_scanning) _discovery.StopScanning();
            _discovery.DeviceFound -= OnDeviceFound;
            _discovery.DeviceLost -= OnDeviceLost;
            _discovery.DeviceUpdated -= OnDeviceUpdated;
        };
    }

    private void OnDeviceFound(object? sender, DeviceInfo device)
    {
        if (InvokeRequired) { BeginInvoke(() => OnDeviceFound(sender, device)); return; }
        if (!_devices.Any(d => d.Ip == device.Ip))
        {
            _devices.Add(device);
            _deviceBinding.ResetBindings(false);
        }
        UpdateDeployButton();
    }

    private void OnDeviceLost(object? sender, string ip)
    {
        if (InvokeRequired) { BeginInvoke(() => OnDeviceLost(sender, ip)); return; }
        _devices.RemoveAll(d => d.Ip == ip);
        _deviceBinding.ResetBindings(false);
        UpdateDeployButton();
    }

    private void OnDeviceUpdated(object? sender, DeviceInfo device)
    {
        if (InvokeRequired) { BeginInvoke(() => OnDeviceUpdated(sender, device)); return; }
        var idx = _devices.FindIndex(d => d.Ip == device.Ip);
        if (idx >= 0) _devices[idx] = device;
        else _devices.Add(device);
        _deviceBinding.ResetBindings(false);
        UpdateDeployButton();
    }

    private void UpdateDeployButton()
    {
        var hasProject = _projectManager.CurrentProject != null;
        var buildDir = GetBuildDirectory();
        var hasBuildDir = !string.IsNullOrEmpty(buildDir) && Directory.Exists(buildDir);
        var hasSelection = _deviceGrid?.CurrentRow?.DataBoundItem is DeviceInfo;
        _deployButton!.Enabled = hasProject && hasBuildDir && hasSelection && !_transfer.IsTransferring;
    }

    private string? GetBuildDirectory()
    {
        var project = _projectManager.CurrentProject;
        if (project == null) return null;
        return Path.Combine(project.ProjectPath, _buildService.OutputPath);
    }

    private async void OnDeployClicked(object? sender, EventArgs e)
    {
        var buildDir = GetBuildDirectory();
        if (string.IsNullOrEmpty(buildDir) || !Directory.Exists(buildDir))
        {
            MessageBox.Show(this, "Build the project first (Build menu) so the output folder exists.", "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_deviceGrid?.CurrentRow?.DataBoundItem is not DeviceInfo device)
        {
            MessageBox.Show(this, "Select a device from the list.", "Deploy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var result = MessageBox.Show(this,
            $"Deploy project '{_projectManager.CurrentProject?.Name}' to {device.DeviceName} ({device.Ip})?\n\nThis will overwrite any existing project on the device.",
            "Deploy Project",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        _deployButton!.Enabled = false;
        _statusLabel!.Text = "Deploying...";
        _progressBar!.Visible = true;
        _progressBar.Value = 0;

        await _transfer.DeployAsync(buildDir, device.Ip, ProjectTransferClient.TransferPort).ConfigureAwait(false);

        if (!_transfer.IsTransferring)
            UpdateDeployButton();
    }
}
