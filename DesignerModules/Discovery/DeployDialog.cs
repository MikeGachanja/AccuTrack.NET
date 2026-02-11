using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Designer.Modules.Project;
using Designer.Modules.Compiler;

namespace Designer.Modules.Discovery;

/// <summary>
/// Dialog for deploying SCADA projects to target Runtime devices.
/// </summary>
public partial class DeployDialog : Form
{
    private ComboBox _scadaProjectCombo;
    private ComboBox _targetDeviceCombo;
    private TextBox _targetIpTextBox;
    private NumericUpDown _targetPortNumeric;
    private CheckBox _buildBeforeDeployCheckBox;
    private CheckBox _backupExistingCheckBox;
    private ListBox _deploymentLogListBox;
    private ProgressBar _deployProgressBar;
    private Button _deployButton;
    private Button _cancelButton;
    private Button _refreshDevicesButton;
    private ProjectManager? _projectManager;
    private CompilerModule? _compilerModule;
    private DeviceDiscoveryClient? _discoveryClient;
    private Dictionary<string, DiscoveredDevice> _devices = new();

    public DeployDialog(ProjectManager? projectManager, CompilerModule? compilerModule)
    {
        _projectManager = projectManager;
        _compilerModule = compilerModule;
        InitializeComponent();
        LoadScadaProjects();
        InitializeDiscovery();
    }

    private void InitializeDiscovery()
    {
        _discoveryClient = new DeviceDiscoveryClient();
        _discoveryClient.DeviceFound += OnDeviceFound;
        _discoveryClient.DeviceUpdated += OnDeviceUpdated;
        _discoveryClient.ErrorOccurred += (_, err) => Log($"Discovery error: {err}");
        
        // Start scanning automatically when dialog opens
        ScanForDevices();
    }

    private void OnDeviceFound(object? sender, DiscoveredDevice device)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnDeviceFound(sender, device));
            return;
        }

        string key = $"{device.IpAddress}:{device.Port}";
        _devices[key] = device;
        UpdateDeviceComboBox();
        Log($"Device found: {device.DeviceName} at {device.IpAddress}:{device.Port}");
    }

    private void OnDeviceUpdated(object? sender, DiscoveredDevice device)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => OnDeviceUpdated(sender, device));
            return;
        }

        string key = $"{device.IpAddress}:{device.Port}";
        if (_devices.ContainsKey(key))
        {
            _devices[key] = device;
            UpdateDeviceComboBox();
        }
    }

    private void UpdateDeviceComboBox()
    {
        _targetDeviceCombo.Items.Clear();
        
        // Add discovered devices
        foreach (var device in _devices.Values.OrderBy(d => d.DeviceName))
        {
            _targetDeviceCombo.Items.Add($"{device.DeviceName} ({device.IpAddress}:{device.Port})");
        }
        
        // Add standard options
        if (_targetDeviceCombo.Items.Count > 0)
        {
            _targetDeviceCombo.Items.Add("---");
        }
        _targetDeviceCombo.Items.Add("Local (127.0.0.1)");
        _targetDeviceCombo.Items.Add("Custom");
        
        if (_targetDeviceCombo.Items.Count > 0)
        {
            _targetDeviceCombo.SelectedIndex = 0;
        }
    }

    private void ScanForDevices()
    {
        if (_discoveryClient == null)
            return;

        Log("Scanning for devices on all network interfaces...");
        _discoveryClient.ClearDevices();
        _devices.Clear();
        UpdateDeviceComboBox();
        
        if (_discoveryClient.StartScanning(5))
        {
            // Wait a bit for responses, then update UI
            Task.Delay(6000).ContinueWith(_ =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(() =>
                    {
                        Log($"Scan complete. Found {_devices.Count} device(s).");
                        if (_devices.Count == 0)
                        {
                            Log("No devices found via broadcast. Try entering IP manually and clicking 'Probe'.");
                            Log("Make sure Runtime is running and listening on port 8889.");
                        }
                    });
                }
            });
        }
        else
        {
            Log("Failed to start device scan.");
        }
    }

    private async Task ProbeDeviceAsync()
    {
        if (_discoveryClient == null)
            return;

        string ip = _targetIpTextBox.Text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            Log("Please enter an IP address to probe.");
            return;
        }

        // Discovery always happens on UDP port 8889 (separate from the TCP transfer port)
        const int discoveryPort = 8889;
        Log($"Probing device discovery at {ip}:{discoveryPort}...");
        
        bool found = await _discoveryClient.ProbeDeviceAsync(ip, discoveryPort, 2000);
        
        if (found)
        {
            Log($"Discovery response received from {ip}:{discoveryPort}");
            // Update device combo box to show the discovered device
            UpdateDeviceComboBox();
        }
        else
        {
            Log($"No discovery response from {ip}:{discoveryPort}. Make sure Runtime is running and accessible, and that UDP 8889 is open.");
        }
    }

    private void InitializeComponent()
    {
        Text = "Deploy Project";
        Size = new System.Drawing.Size(650, 580);
        MinimumSize = new System.Drawing.Size(550, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(10),
            AutoSize = true
        };

        // Configure column styles
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row = 0;

        // Row 0: SCADA Project
        mainLayout.Controls.Add(new Label { Text = "SCADA Project:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top }, 0, row);
        _scadaProjectCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        mainLayout.Controls.Add(_scadaProjectCombo, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Row 1: Target Device
        mainLayout.Controls.Add(new Label { Text = "Target Device:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top }, 0, row);
        var deviceLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        _targetDeviceCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _targetDeviceCombo.SelectedIndexChanged += OnDeviceSelectionChanged;
        _refreshDevicesButton = new Button { Text = "Refresh", Size = new System.Drawing.Size(75, 25), Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _refreshDevicesButton.Click += (s, e) => ScanForDevices();
        deviceLayout.Controls.Add(_targetDeviceCombo);
        deviceLayout.Controls.Add(_refreshDevicesButton);
        mainLayout.Controls.Add(deviceLayout, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Row 2: Target IP/Port
        mainLayout.Controls.Add(new Label { Text = "Target IP:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top }, 0, row);
        var ipPortLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        _targetIpTextBox = new TextBox { Text = "127.0.0.1", Width = 140, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        _targetPortNumeric = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 8888, Width = 70, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        var probeButton = new Button { Text = "Probe", Size = new System.Drawing.Size(60, 25), Anchor = AnchorStyles.Left | AnchorStyles.Top };
        probeButton.Click += async (s, e) => await ProbeDeviceAsync();
        ipPortLayout.Controls.Add(_targetIpTextBox);
        ipPortLayout.Controls.Add(new Label { Text = " Port:", AutoSize = true });
        ipPortLayout.Controls.Add(_targetPortNumeric);
        ipPortLayout.Controls.Add(probeButton);
        mainLayout.Controls.Add(ipPortLayout, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Row 3: Build checkbox
        _buildBeforeDeployCheckBox = new CheckBox { Text = "Build project before deploying", AutoSize = true, Checked = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        mainLayout.Controls.Add(_buildBeforeDeployCheckBox, 0, row);
        mainLayout.SetColumnSpan(_buildBeforeDeployCheckBox, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Row 4: Backup checkbox
        _backupExistingCheckBox = new CheckBox { Text = "Runtime will replace existing project (no backup)", AutoSize = true, Checked = true, Anchor = AnchorStyles.Left | AnchorStyles.Top };
        mainLayout.Controls.Add(_backupExistingCheckBox, 0, row);
        mainLayout.SetColumnSpan(_backupExistingCheckBox, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Row 5: Log label
        mainLayout.Controls.Add(new Label { Text = "Deployment Log:", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top }, 0, row);
        mainLayout.SetColumnSpan(mainLayout.Controls[mainLayout.Controls.Count - 1], 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Row 6: Log listbox (expandable)
        _deploymentLogListBox = new ListBox { Dock = DockStyle.Fill, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        mainLayout.Controls.Add(_deploymentLogListBox, 0, row);
        mainLayout.SetColumnSpan(_deploymentLogListBox, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        row++;

        // Row 7: Progress bar
        _deployProgressBar = new ProgressBar { Dock = DockStyle.Fill, Height = 25, Style = ProgressBarStyle.Continuous, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
        mainLayout.Controls.Add(_deployProgressBar, 0, row);
        mainLayout.SetColumnSpan(_deployProgressBar, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Row 8: Buttons (fixed at bottom)
        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Height = 35 };
        _deployButton = new Button { Text = "Deploy", Size = new System.Drawing.Size(85, 28), Anchor = AnchorStyles.Right | AnchorStyles.Top };
        _deployButton.Click += OnDeployClick;
        _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(85, 28), Anchor = AnchorStyles.Right | AnchorStyles.Top };
        buttonPanel.Controls.Add(_deployButton);
        buttonPanel.Controls.Add(new Label { Width = 10 }); // Spacing
        buttonPanel.Controls.Add(_cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, row);
        mainLayout.SetColumnSpan(buttonPanel, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        Controls.Add(mainLayout);
        CancelButton = _cancelButton;
    }

    private void OnDeviceSelectionChanged(object? sender, EventArgs e)
    {
        if (_targetDeviceCombo.SelectedItem == null)
            return;

        string selected = _targetDeviceCombo.SelectedItem.ToString() ?? "";
        
        // Check if it's a discovered device
        if (selected.Contains("(") && selected.Contains(")"))
        {
            // Extract IP and port from format "DeviceName (IP:Port)"
            int start = selected.IndexOf('(') + 1;
            int end = selected.IndexOf(')');
            if (start > 0 && end > start)
            {
                string ipPort = selected.Substring(start, end - start);
                string[] parts = ipPort.Split(':');
                if (parts.Length == 2)
                {
                    _targetIpTextBox.Text = parts[0];
                    _targetIpTextBox.Enabled = false;
                    if (int.TryParse(parts[1], out int port))
                    {
                        _targetPortNumeric.Value = port;
                    }
                    return;
                }
            }
        }
        
        // Handle standard options
        if (selected == "Local (127.0.0.1)")
        {
            _targetIpTextBox.Text = "127.0.0.1";
            _targetIpTextBox.Enabled = false;
            _targetPortNumeric.Value = 8888;
        }
        else if (selected == "Custom")
        {
            _targetIpTextBox.Enabled = true;
        }
        else if (selected == "---")
        {
            // Separator, don't change anything
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _discoveryClient?.StopScanning();
        base.OnFormClosing(e);
    }

    private void LoadScadaProjects()
    {
        if (_projectManager != null)
        {
            var projects = _projectManager.GetScadaProjects();
            foreach (var project in projects)
                _scadaProjectCombo.Items.Add(project.Name);
            if (_scadaProjectCombo.Items.Count > 0)
                _scadaProjectCombo.SelectedIndex = 0;
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => { _deploymentLogListBox.Items.Add(message); _deploymentLogListBox.SelectedIndex = _deploymentLogListBox.Items.Count - 1; });
            return;
        }
        _deploymentLogListBox.Items.Add(message);
        _deploymentLogListBox.SelectedIndex = _deploymentLogListBox.Items.Count - 1;
    }

    private void SetProgress(int percent)
    {
        if (InvokeRequired) { BeginInvoke(() => _deployProgressBar.Value = Math.Clamp(percent, 0, 100)); return; }
        _deployProgressBar.Value = Math.Clamp(percent, 0, 100);
    }

    private async void OnDeployClick(object? sender, EventArgs e)
    {
        if (_scadaProjectCombo.SelectedItem == null)
        {
            MessageBox.Show("Please select a SCADA project.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string projectName = _scadaProjectCombo.SelectedItem.ToString() ?? "";
        var scada = _projectManager?.FindScadaProject(projectName);
        if (scada == null)
        {
            MessageBox.Show("SCADA project not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _deployButton.Enabled = false;
        _deployProgressBar.Value = 0;
        _deploymentLogListBox.Items.Clear();
        Log("Starting deployment...");

        try
        {
            if (_buildBeforeDeployCheckBox.Checked && _compilerModule != null)
            {
                Log("Building project...");
                _compilerModule.SetProject(scada);
                bool built = _compilerModule.CompileProject();
                if (!built)
                {
                    Log("Build failed.");
                    MessageBox.Show("Build failed. Check the Output/Debug console.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _deployButton.Enabled = true;
                    return;
                }
                Log("Build completed.");
            }

            string buildPath = scada.Paths.BuildPath;
            if (!System.IO.Directory.Exists(buildPath))
            {
                Log("Build directory not found. Build the project first.");
                MessageBox.Show("Build directory not found. Enable 'Build project before deploying' or build manually.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _deployButton.Enabled = true;
                return;
            }

            // Get IP and port from selected device or text boxes
            string host = _targetIpTextBox.Text.Trim();
            if (string.IsNullOrEmpty(host)) host = "127.0.0.1";
            int port = (int)_targetPortNumeric.Value;
            
            // If a discovered device is selected, use its IP and port
            if (_targetDeviceCombo.SelectedItem != null)
            {
                string selected = _targetDeviceCombo.SelectedItem.ToString() ?? "";
                if (selected.Contains("(") && selected.Contains(")"))
                {
                    int start = selected.IndexOf('(') + 1;
                    int end = selected.IndexOf(')');
                    if (start > 0 && end > start)
                    {
                        string ipPort = selected.Substring(start, end - start);
                        string[] parts = ipPort.Split(':');
                        if (parts.Length == 2)
                        {
                            host = parts[0];
                            if (int.TryParse(parts[1], out int devicePort))
                            {
                                port = devicePort;
                            }
                        }
                    }
                }
            }

            Log($"Connecting to {host}:{port}...");
            Log($"Build directory: {buildPath}");
            
            // Verify build directory contents
            if (System.IO.Directory.Exists(buildPath))
            {
                var files = System.IO.Directory.GetFiles(buildPath, "*", System.IO.SearchOption.AllDirectories);
                Log($"Found {files.Length} files in build directory");
            }
            
            var client = new ProjectTransferClient();
            client.TransferStarted += (_, projectName) => Log($"Transfer started for project: {projectName}");
            client.TransferProgress += (_, p) => 
            {
                SetProgress(p);
                Log($"Transfer progress: {p}%");
            };
            client.TransferComplete += (_, msg) => Log($"Transfer complete: {msg}");
            client.ErrorOccurred += (_, err) => 
            {
                Log($"Error: {err}");
                System.Diagnostics.Debug.WriteLine($"Transfer error: {err}");
            };

            bool success = await client.DeployProjectAsync(buildPath, host, port).ConfigureAwait(true);

            if (success)
            {
                Log("Deployment completed successfully.");
                MessageBox.Show("Project deployed successfully. The Runtime will reload the project shortly.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
            }
            else
            {
                Log("Deployment failed.");
                MessageBox.Show("Deployment failed. Check the log above.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (InvokeRequired)
                BeginInvoke(() => _deployButton.Enabled = true);
            else
                _deployButton.Enabled = true;
        }
    }
}
