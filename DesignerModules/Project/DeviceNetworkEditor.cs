using System;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Project;

/// <summary>
/// Editor for device network configuration.
/// </summary>
public partial class DeviceNetworkEditor : UserControl
{
    private DeviceNetwork? _deviceNetwork;
    private ProjectManager? _projectManager;
    private DataGridView _devicesGrid;
    private Button _addDeviceButton;
    private Button _removeDeviceButton;

    public DeviceNetworkEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };

        // Toolbar
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 35
        };

        _addDeviceButton = new Button
        {
            Text = "Add Device",
            Size = new Size(100, 25)
        };
        _addDeviceButton.Click += OnAddDevice;

        _removeDeviceButton = new Button
        {
            Text = "Remove Device",
            Size = new Size(100, 25),
            Enabled = false
        };
        _removeDeviceButton.Click += OnRemoveDevice;

        toolbar.Controls.Add(_addDeviceButton);
        toolbar.Controls.Add(_removeDeviceButton);

        // Devices grid
        _devicesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _devicesGrid.Columns.Add("Name", "Device Name");
        _devicesGrid.Columns.Add("IPAddress", "IP Address");
        _devicesGrid.Columns.Add("Port", "Port");
        _devicesGrid.Columns.Add("Type", "Type");

        _devicesGrid.SelectionChanged += (s, e) =>
        {
            _removeDeviceButton.Enabled = _devicesGrid.SelectedRows.Count > 0;
        };

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_devicesGrid, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Sets the device network to edit.
    /// </summary>
    public void SetDeviceNetwork(DeviceNetwork network)
    {
        _deviceNetwork = network;
        LoadDevices();
    }

    /// <summary>
    /// Sets the project manager.
    /// </summary>
    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;
    }

    /// <summary>
    /// Loads devices into the grid.
    /// </summary>
    private void LoadDevices()
    {
        _devicesGrid.Rows.Clear();
        // TODO: Load devices from DeviceNetwork when device model is implemented
    }

    /// <summary>
    /// Handles add device button click.
    /// </summary>
    private void OnAddDevice(object? sender, EventArgs e)
    {
        // TODO: Show add device dialog
        MessageBox.Show("Add device functionality not yet implemented.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Handles remove device button click.
    /// </summary>
    private void OnRemoveDevice(object? sender, EventArgs e)
    {
        if (_devicesGrid.SelectedRows.Count > 0)
        {
            // TODO: Remove selected device
            MessageBox.Show("Remove device functionality not yet implemented.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
