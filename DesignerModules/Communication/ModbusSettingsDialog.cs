using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Designer.Modules.Communication;

namespace Designer.Modules.Communication;

/// <summary>
/// Dialog for configuring Modbus settings.
/// </summary>
public partial class ModbusSettingsDialog : Form
{
    private TextBox _ipAddressTextBox;
    private NumericUpDown _portNumeric;
    private NumericUpDown _slaveIdNumeric;
    private NumericUpDown _timeoutNumeric;
    private ComboBox _baudRateCombo;
    private ComboBox _parityCombo;
    private ComboBox _stopBitsCombo;
    private ComboBox _dataBitsCombo;
    private ComboBox _portNameCombo;
    private CommunicationModule _module;

    public ModbusSettingsDialog(CommunicationModule module)
    {
        _module = module;
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Text = "Modbus Settings";
        Size = new System.Drawing.Size(400, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 10,
            Padding = new Padding(10)
        };

        int row = 0;

        if (_module.Type == "ModbusTCP")
        {
            mainLayout.Controls.Add(new Label { Text = "IP Address:", AutoSize = true }, 0, row);
            _ipAddressTextBox = new TextBox { Dock = DockStyle.Fill };
            _ipAddressTextBox.Text = "192.168.1.1";
            mainLayout.Controls.Add(_ipAddressTextBox, 1, row++);

            mainLayout.Controls.Add(new Label { Text = "Port:", AutoSize = true }, 0, row);
            _portNumeric = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 502, Dock = DockStyle.Fill };
            mainLayout.Controls.Add(_portNumeric, 1, row++);
        }
        else // ModbusRTU
        {
            mainLayout.Controls.Add(new Label { Text = "Port Name:", AutoSize = true }, 0, row);
            _portNameCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _portNameCombo.Items.AddRange(new[] { "COM1", "COM2", "COM3", "COM4", "COM5" });
            _portNameCombo.SelectedIndex = 0;
            mainLayout.Controls.Add(_portNameCombo, 1, row++);

            mainLayout.Controls.Add(new Label { Text = "Baud Rate:", AutoSize = true }, 0, row);
            _baudRateCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _baudRateCombo.Items.AddRange(new[] { "9600", "19200", "38400", "57600", "115200" });
            _baudRateCombo.SelectedIndex = 0;
            mainLayout.Controls.Add(_baudRateCombo, 1, row++);

            mainLayout.Controls.Add(new Label { Text = "Data Bits:", AutoSize = true }, 0, row);
            _dataBitsCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _dataBitsCombo.Items.AddRange(new[] { "7", "8" });
            _dataBitsCombo.SelectedIndex = 1;
            mainLayout.Controls.Add(_dataBitsCombo, 1, row++);

            mainLayout.Controls.Add(new Label { Text = "Parity:", AutoSize = true }, 0, row);
            _parityCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _parityCombo.Items.AddRange(new[] { "None", "Even", "Odd" });
            _parityCombo.SelectedIndex = 0;
            mainLayout.Controls.Add(_parityCombo, 1, row++);

            mainLayout.Controls.Add(new Label { Text = "Stop Bits:", AutoSize = true }, 0, row);
            _stopBitsCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _stopBitsCombo.Items.AddRange(new[] { "1", "2" });
            _stopBitsCombo.SelectedIndex = 0;
            mainLayout.Controls.Add(_stopBitsCombo, 1, row++);
        }

        mainLayout.Controls.Add(new Label { Text = "Slave ID:", AutoSize = true }, 0, row);
        _slaveIdNumeric = new NumericUpDown { Minimum = 1, Maximum = 247, Value = 1, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_slaveIdNumeric, 1, row++);

        mainLayout.Controls.Add(new Label { Text = "Timeout (ms):", AutoSize = true }, 0, row);
        _timeoutNumeric = new NumericUpDown { Minimum = 100, Maximum = 10000, Value = 1000, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_timeoutNumeric, 1, row++);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new System.Drawing.Size(75, 23)
        };
        okButton.Click += (s, e) => DialogResult = DialogResult.OK;

        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new System.Drawing.Size(75, 23)
        };

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, row);
        mainLayout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(mainLayout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void LoadSettings()
    {
        if (_module.Settings.ContainsKey("ipAddress") && _ipAddressTextBox != null)
            _ipAddressTextBox.Text = _module.Settings["ipAddress"].ToString();
        if (_module.Settings.ContainsKey("port") && _portNumeric != null)
            _portNumeric.Value = Convert.ToInt32(_module.Settings["port"]);
        if (_module.Settings.ContainsKey("slaveId") && _slaveIdNumeric != null)
            _slaveIdNumeric.Value = Convert.ToInt32(_module.Settings["slaveId"]);
        if (_module.Settings.ContainsKey("timeout") && _timeoutNumeric != null)
            _timeoutNumeric.Value = Convert.ToInt32(_module.Settings["timeout"]);
        if (_module.Settings.ContainsKey("portName") && _portNameCombo != null)
            _portNameCombo.Text = _module.Settings["portName"].ToString();
        if (_module.Settings.ContainsKey("baudRate") && _baudRateCombo != null)
            _baudRateCombo.Text = _module.Settings["baudRate"].ToString();
        if (_module.Settings.ContainsKey("parity") && _parityCombo != null)
            _parityCombo.Text = _module.Settings["parity"].ToString();
        if (_module.Settings.ContainsKey("stopBits") && _stopBitsCombo != null)
            _stopBitsCombo.Text = _module.Settings["stopBits"].ToString();
        if (_module.Settings.ContainsKey("dataBits") && _dataBitsCombo != null)
            _dataBitsCombo.Text = _module.Settings["dataBits"].ToString();
    }

    public Dictionary<string, object> GetSettings()
    {
        var settings = new Dictionary<string, object>();

        if (_module.Type == "ModbusTCP")
        {
            if (_ipAddressTextBox != null)
                settings["ipAddress"] = _ipAddressTextBox.Text;
            if (_portNumeric != null)
                settings["port"] = (int)_portNumeric.Value;
        }
        else // ModbusRTU
        {
            if (_portNameCombo != null)
                settings["portName"] = _portNameCombo.Text;
            if (_baudRateCombo != null)
                settings["baudRate"] = _baudRateCombo.Text;
            if (_dataBitsCombo != null)
                settings["dataBits"] = _dataBitsCombo.Text;
            if (_parityCombo != null)
                settings["parity"] = _parityCombo.Text;
            if (_stopBitsCombo != null)
                settings["stopBits"] = _stopBitsCombo.Text;
        }

        if (_slaveIdNumeric != null)
            settings["slaveId"] = (int)_slaveIdNumeric.Value;
        if (_timeoutNumeric != null)
            settings["timeout"] = (int)_timeoutNumeric.Value;

        return settings;
    }
}
