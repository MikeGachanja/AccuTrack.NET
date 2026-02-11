using System;
using System.Net;
using System.Windows.Forms;

namespace Designer.Modules.Project;

/// <summary>
/// Dialog for entering IP address.
/// </summary>
public partial class IPAddressDialog : Form
{
    private TextBox _ipAddressTextBox;
    private NumericUpDown _portNumeric;

    public string IPAddress { get; private set; } = string.Empty;
    public int Port { get; private set; } = 0;

    public IPAddressDialog(string? initialIP = null, int initialPort = 0)
    {
        InitializeComponent();
        if (!string.IsNullOrEmpty(initialIP))
        {
            _ipAddressTextBox.Text = initialIP;
        }
        if (initialPort > 0)
        {
            _portNumeric.Value = initialPort;
        }
    }

    private void InitializeComponent()
    {
        Text = "Enter IP Address";
        Size = new System.Drawing.Size(350, 150);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(10)
        };

        mainLayout.Controls.Add(new Label { Text = "IP Address:", AutoSize = true }, 0, 0);
        _ipAddressTextBox = new TextBox { Dock = DockStyle.Fill };
        _ipAddressTextBox.Text = "192.168.1.1";
        mainLayout.Controls.Add(_ipAddressTextBox, 1, 0);

        mainLayout.Controls.Add(new Label { Text = "Port:", AutoSize = true }, 0, 1);
        _portNumeric = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 502, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_portNumeric, 1, 1);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
        okButton.Click += (s, e) =>
        {
            if (ValidateIPAddress())
            {
                IPAddress = _ipAddressTextBox.Text;
                Port = (int)_portNumeric.Value;
                DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("Please enter a valid IP address.", "Invalid IP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, 2);
        mainLayout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(mainLayout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private bool ValidateIPAddress()
    {
        if (string.IsNullOrEmpty(_ipAddressTextBox.Text))
            return false;

        return System.Net.IPAddress.TryParse(_ipAddressTextBox.Text, out _);
    }
}
