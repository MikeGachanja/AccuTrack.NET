using System;
using System.Drawing;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Project;

/// <summary>
/// Editor for device network: displays configured SCADA projects as nodes with name, type, IP address, and port.
/// Devices are synced from the project's SCADA list; configure target device in each SCADA project's Properties → Target Device.
/// </summary>
public partial class DeviceNetworkEditor : UserControl
{
    private DeviceNetwork? _deviceNetwork;
    private ProjectManager? _projectManager;
    private Panel _cardsPanel;
    private FlowLayoutPanel _flowPanel;
    private Label _infoLabel;

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
            RowCount = 2,
            Padding = new Padding(10)
        };

        _infoLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = "Device nodes are created from your SCADA projects. Set each project's target device in SCADA Project Properties → Target Device tab (IP and port).",
            ForeColor = Color.Gray,
            MaximumSize = new Size(800, 40),
            AutoEllipsis = true
        };
        mainLayout.Controls.Add(_infoLabel, 0, 0);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _flowPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0)
        };
        scroll.Controls.Add(_flowPanel);
        _cardsPanel = scroll;
        mainLayout.Controls.Add(scroll, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }

    public void SetDeviceNetwork(DeviceNetwork network)
    {
        _deviceNetwork = network;
        LoadDevices();
    }

    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;
        if (_deviceNetwork != null && _projectManager != null)
        {
            _deviceNetwork.SyncFromScadaProjects(_projectManager.GetScadaProjects());
            LoadDevices();
        }
    }

    private void LoadDevices()
    {
        _flowPanel.Controls.Clear();
        if (_deviceNetwork == null)
            return;

        foreach (var node in _deviceNetwork.GetAllDevices())
        {
            var card = CreateDeviceCard(node);
            _flowPanel.Controls.Add(card);
        }

        if (_flowPanel.Controls.Count == 0)
        {
            var noDevices = new Label
            {
                Text = "No SCADA projects in this solution. Add a SCADA project; then set its target device in Properties → Target Device.",
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(8)
            };
            _flowPanel.Controls.Add(noDevices);
        }
    }

    private Panel CreateDeviceCard(DeviceNode node)
    {
        const int cardWidth = 220;
        const int cardHeight = 160;

        var card = new Panel
        {
            Size = new Size(cardWidth, cardHeight),
            Margin = new Padding(8),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var nameLabel = new Label
        {
            Text = node.Name,
            Font = new Font(card.Font.FontFamily, 11, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(10, 10),
            MaximumSize = new Size(cardWidth - 20, 0)
        };
        card.Controls.Add(nameLabel);

        var typeLabel = new Label
        {
            Text = node.TypeString,
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(10, nameLabel.Bottom + 4)
        };
        card.Controls.Add(typeLabel);

        var ipLabel = new Label
        {
            Text = "IP Address",
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(10, typeLabel.Bottom + 12)
        };
        card.Controls.Add(ipLabel);

        var ipValue = new Label
        {
            Text = string.IsNullOrEmpty(node.IpAddress) ? "Not configured" : node.IpAddress,
            Font = new Font(card.Font.FontFamily, 9, FontStyle.Bold),
            ForeColor = string.IsNullOrEmpty(node.IpAddress) ? Color.Gray : Color.DarkSlateGray,
            AutoSize = true,
            Location = new Point(10, ipLabel.Bottom + 2)
        };
        card.Controls.Add(ipValue);

        var portLabel = new Label
        {
            Text = "Port",
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(10, ipValue.Bottom + 8)
        };
        card.Controls.Add(portLabel);

        var portValue = new Label
        {
            Text = node.Port.ToString(),
            AutoSize = true,
            Location = new Point(10, portLabel.Bottom + 2)
        };
        card.Controls.Add(portValue);

        return card;
    }
}
