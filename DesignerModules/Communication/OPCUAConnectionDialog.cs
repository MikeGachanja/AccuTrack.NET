using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Designer.Modules.Communication;

namespace Designer.Modules.Communication;

/// <summary>
/// Dialog for configuring OPC UA connection settings.
/// </summary>
public partial class OPCUAConnectionDialog : Form
{
    private TextBox _endpointUrlTextBox;
    private TextBox _securityPolicyTextBox;
    private TextBox _securityModeTextBox;
    private TextBox _usernameTextBox;
    private TextBox _passwordTextBox;
    private CheckBox _useAnonymousCheckBox;
    private NumericUpDown _sessionTimeoutNumeric;
    private CommunicationModule _module;

    public OPCUAConnectionDialog(CommunicationModule module)
    {
        _module = module;
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Text = "OPC UA Connection Settings";
        Size = new System.Drawing.Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(10)
        };

        int row = 0;

        mainLayout.Controls.Add(new Label { Text = "Endpoint URL:", AutoSize = true }, 0, row);
        _endpointUrlTextBox = new TextBox { Dock = DockStyle.Fill };
        _endpointUrlTextBox.Text = "opc.tcp://localhost:4840";
        mainLayout.Controls.Add(_endpointUrlTextBox, 1, row++);

        mainLayout.Controls.Add(new Label { Text = "Security Policy:", AutoSize = true }, 0, row);
        _securityPolicyTextBox = new TextBox { Dock = DockStyle.Fill };
        _securityPolicyTextBox.Text = "None";
        mainLayout.Controls.Add(_securityPolicyTextBox, 1, row++);

        mainLayout.Controls.Add(new Label { Text = "Security Mode:", AutoSize = true }, 0, row);
        _securityModeTextBox = new TextBox { Dock = DockStyle.Fill };
        _securityModeTextBox.Text = "None";
        mainLayout.Controls.Add(_securityModeTextBox, 1, row++);

        _useAnonymousCheckBox = new CheckBox { Text = "Use Anonymous Authentication", AutoSize = true };
        _useAnonymousCheckBox.Checked = true;
        _useAnonymousCheckBox.CheckedChanged += (s, e) =>
        {
            _usernameTextBox.Enabled = !_useAnonymousCheckBox.Checked;
            _passwordTextBox.Enabled = !_useAnonymousCheckBox.Checked;
        };
        mainLayout.Controls.Add(_useAnonymousCheckBox, 0, row);
        mainLayout.SetColumnSpan(_useAnonymousCheckBox, 2);
        row++;

        mainLayout.Controls.Add(new Label { Text = "Username:", AutoSize = true }, 0, row);
        _usernameTextBox = new TextBox { Dock = DockStyle.Fill, Enabled = false };
        mainLayout.Controls.Add(_usernameTextBox, 1, row++);

        mainLayout.Controls.Add(new Label { Text = "Password:", AutoSize = true }, 0, row);
        _passwordTextBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Enabled = false };
        mainLayout.Controls.Add(_passwordTextBox, 1, row++);

        mainLayout.Controls.Add(new Label { Text = "Session Timeout (ms):", AutoSize = true }, 0, row);
        _sessionTimeoutNumeric = new NumericUpDown { Minimum = 1000, Maximum = 3600000, Value = 60000, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_sessionTimeoutNumeric, 1, row++);

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
        if (_module.Settings.ContainsKey("endpointUrl"))
            _endpointUrlTextBox.Text = _module.Settings["endpointUrl"].ToString();
        if (_module.Settings.ContainsKey("securityPolicy"))
            _securityPolicyTextBox.Text = _module.Settings["securityPolicy"].ToString();
        if (_module.Settings.ContainsKey("securityMode"))
            _securityModeTextBox.Text = _module.Settings["securityMode"].ToString();
        if (_module.Settings.ContainsKey("useAnonymous"))
            _useAnonymousCheckBox.Checked = Convert.ToBoolean(_module.Settings["useAnonymous"]);
        if (_module.Settings.ContainsKey("username"))
            _usernameTextBox.Text = _module.Settings["username"].ToString();
        if (_module.Settings.ContainsKey("password"))
            _passwordTextBox.Text = _module.Settings["password"].ToString();
        if (_module.Settings.ContainsKey("sessionTimeout"))
            _sessionTimeoutNumeric.Value = Convert.ToInt32(_module.Settings["sessionTimeout"]);
    }

    public Dictionary<string, object> GetSettings()
    {
        return new Dictionary<string, object>
        {
            ["endpointUrl"] = _endpointUrlTextBox.Text,
            ["securityPolicy"] = _securityPolicyTextBox.Text,
            ["securityMode"] = _securityModeTextBox.Text,
            ["useAnonymous"] = _useAnonymousCheckBox.Checked,
            ["username"] = _usernameTextBox.Text,
            ["password"] = _passwordTextBox.Text,
            ["sessionTimeout"] = (int)_sessionTimeoutNumeric.Value
        };
    }
}
