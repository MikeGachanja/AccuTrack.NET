using System;
using System.Drawing;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Project;

/// <summary>
/// Dialog for viewing and editing SCADA project properties.
/// </summary>
public partial class ScadaProjectPropertiesDialog : Form
{
    private ScadaProject _scadaProject;
    private ProjectManager? _projectManager;
    private TextBox _nameTextBox;
    private ComboBox _typeComboBox;
    private ComboBox _resolutionComboBox;
    private NumericUpDown _widthNumeric;
    private NumericUpDown _heightNumeric;
    private TextBox _versionTextBox;
    private Label _pathLabel;
    private ComboBox _startupScreenComboBox;
    private bool _customResolution = false;
    private TextBox _targetDeviceHostTextBox;
    private NumericUpDown _targetDevicePortNumeric;

    public ScadaProjectPropertiesDialog(ScadaProject scadaProject, ProjectManager? projectManager = null)
    {
        _scadaProject = scadaProject ?? throw new ArgumentNullException(nameof(scadaProject));
        _projectManager = projectManager;
        InitializeComponent();
        LoadProperties();
    }

    private void InitializeComponent()
    {
        Text = $"SCADA Project Properties - {_scadaProject.Name}";
        Size = new Size(520, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var tabControl = new TabControl { Dock = DockStyle.Fill, Padding = new Point(8, 6) };

        // --- Tab 1: General ---
        var generalTab = new TabPage("General");
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(10)
        };
        int row = 0;

        mainLayout.Controls.Add(new Label { Text = "Name:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _nameTextBox = new TextBox { Text = _scadaProject.Name, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_nameTextBox, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        mainLayout.Controls.Add(new Label { Text = "Type:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _typeComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _typeComboBox.Items.AddRange(new[] { "HMI", "PC Station", "BMS" });
        _typeComboBox.SelectedIndex = (int)_scadaProject.Type;
        mainLayout.Controls.Add(_typeComboBox, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        mainLayout.Controls.Add(new Label { Text = "Resolution:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        var resolutionLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _resolutionComboBox = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        _resolutionComboBox.Items.AddRange(new[] { "1920x1080 (Full HD)", "1280x720 (HD)", "1024x768 (XGA)", "800x600 (SVGA)", "Custom..." });
        _resolutionComboBox.SelectedIndexChanged += OnResolutionComboBoxChanged;
        resolutionLayout.Controls.Add(_resolutionComboBox);
        _widthNumeric = new NumericUpDown { Width = 80, Minimum = 100, Maximum = 10000, Value = _scadaProject.Resolution.Width };
        _heightNumeric = new NumericUpDown { Width = 80, Minimum = 100, Maximum = 10000, Value = _scadaProject.Resolution.Height };
        resolutionLayout.Controls.Add(_widthNumeric);
        resolutionLayout.Controls.Add(new Label { Text = " x ", AutoSize = true });
        resolutionLayout.Controls.Add(_heightNumeric);
        UpdateResolutionComboBox();
        UpdateCustomResolutionVisibility();
        mainLayout.Controls.Add(resolutionLayout, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        mainLayout.Controls.Add(new Label { Text = "Version:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _versionTextBox = new TextBox { Text = _scadaProject.Version, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_versionTextBox, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        mainLayout.Controls.Add(new Label { Text = "Path:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _pathLabel = new Label { Text = _scadaProject.Path, AutoSize = false, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(3) };
        mainLayout.Controls.Add(_pathLabel, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        mainLayout.Controls.Add(new Label { Text = "Startup Screen:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _startupScreenComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        LoadStartupScreenOptions();
        mainLayout.Controls.Add(_startupScreenComboBox, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        mainLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, row);
        mainLayout.SetColumnSpan(mainLayout.Controls[mainLayout.Controls.Count - 1], 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        generalTab.Controls.Add(mainLayout);
        tabControl.TabPages.Add(generalTab);

        // --- Tab 2: Target Device ---
        var deviceTab = new TabPage("Target Device");
        var deviceLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(10)
        };
        deviceLayout.Controls.Add(new Label { Text = "Deploy/Upload will probe this address first when scanning for devices.", AutoSize = true }, 0, 0);
        deviceLayout.SetColumnSpan(deviceLayout.Controls[deviceLayout.Controls.Count - 1], 2);
        deviceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        deviceLayout.Controls.Add(new Label { Text = "Host (IP or name):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        _targetDeviceHostTextBox = new TextBox { Text = _scadaProject.TargetDeviceHost ?? "", Dock = DockStyle.Fill, Width = 220 };
        deviceLayout.Controls.Add(_targetDeviceHostTextBox, 1, 1);
        deviceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        deviceLayout.Controls.Add(new Label { Text = "Transfer port (TCP):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        _targetDevicePortNumeric = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = _scadaProject.TargetDevicePort, Width = 80 };
        deviceLayout.Controls.Add(_targetDevicePortNumeric, 1, 2);
        deviceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        deviceLayout.Controls.Add(new Label { Text = "Leave host blank to use scan/discovery only.", AutoSize = true, ForeColor = System.Drawing.Color.Gray }, 0, 3);
        deviceLayout.SetColumnSpan(deviceLayout.Controls[deviceLayout.Controls.Count - 1], 2);
        deviceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        deviceTab.Controls.Add(deviceLayout);
        tabControl.TabPages.Add(deviceTab);

        // Main form: tab control + buttons
        var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(6) };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        mainPanel.Controls.Add(tabControl, 0, 0);
        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Height = 40 };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 23) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 23) };
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        mainPanel.Controls.Add(buttonPanel, 0, 1);
        Controls.Add(mainPanel);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void LoadProperties()
    {
        _nameTextBox.Text = _scadaProject.Name;
        _typeComboBox.SelectedIndex = (int)_scadaProject.Type;
        _versionTextBox.Text = _scadaProject.Version;
        _pathLabel.Text = _scadaProject.Path;
        _targetDeviceHostTextBox.Text = _scadaProject.TargetDeviceHost ?? "";
        _targetDevicePortNumeric.Value = Math.Clamp(_scadaProject.TargetDevicePort, 1, 65535);
        UpdateResolutionComboBox();
        UpdateStartupScreenSelection();
    }
    
    private void LoadStartupScreenOptions()
    {
        _startupScreenComboBox.Items.Clear();
        _startupScreenComboBox.Items.Add("(None - Use first screen)");
        
        if (_projectManager != null)
        {
            var screens = _projectManager.GetScreens(_scadaProject.Name);
            foreach (var screen in screens)
            {
                try
                {
                    dynamic screenObj = screen;
                    string screenName = screenObj.Name?.ToString() ?? "Unknown";
                    string screenId = screenObj.Id?.ToString() ?? screenName;
                    _startupScreenComboBox.Items.Add($"{screenName} ({screenId})");
                }
                catch { /* Skip invalid screens */ }
            }
        }
        
        if (_startupScreenComboBox.Items.Count > 0)
            _startupScreenComboBox.SelectedIndex = 0;
    }
    
    private void UpdateStartupScreenSelection()
    {
        if (string.IsNullOrEmpty(_scadaProject.StartupScreen))
        {
            _startupScreenComboBox.SelectedIndex = 0; // "(None - Use first screen)"
            return;
        }
        
        // Find the matching screen in the combo box
        for (int i = 1; i < _startupScreenComboBox.Items.Count; i++)
        {
            string itemText = _startupScreenComboBox.Items[i].ToString() ?? "";
            // Extract screen ID from item text (format: "Name (Id)")
            if (itemText.Contains("(") && itemText.Contains(")"))
            {
                int startIdx = itemText.LastIndexOf("(") + 1;
                int endIdx = itemText.LastIndexOf(")");
                if (startIdx > 0 && endIdx > startIdx)
                {
                    string screenId = itemText.Substring(startIdx, endIdx - startIdx);
                    if (screenId == _scadaProject.StartupScreen)
                    {
                        _startupScreenComboBox.SelectedIndex = i;
                        return;
                    }
                }
            }
        }
        
        // If not found, select "(None)"
        _startupScreenComboBox.SelectedIndex = 0;
    }

    private void UpdateResolutionComboBox()
    {
        var resolution = _scadaProject.Resolution;
        if (resolution.Width == 1920 && resolution.Height == 1080)
            _resolutionComboBox.SelectedIndex = 0;
        else if (resolution.Width == 1280 && resolution.Height == 720)
            _resolutionComboBox.SelectedIndex = 1;
        else if (resolution.Width == 1024 && resolution.Height == 768)
            _resolutionComboBox.SelectedIndex = 2;
        else if (resolution.Width == 800 && resolution.Height == 600)
            _resolutionComboBox.SelectedIndex = 3;
        else
        {
            _resolutionComboBox.SelectedIndex = 4; // Custom
            _customResolution = true;
        }
        UpdateCustomResolutionVisibility();
    }

    private void OnResolutionComboBoxChanged(object? sender, EventArgs e)
    {
        if (_resolutionComboBox.SelectedIndex == 4) // Custom
        {
            _customResolution = true;
        }
        else
        {
            _customResolution = false;
            // Set standard resolution
            switch (_resolutionComboBox.SelectedIndex)
            {
                case 0: // 1920x1080
                    _widthNumeric.Value = 1920;
                    _heightNumeric.Value = 1080;
                    break;
                case 1: // 1280x720
                    _widthNumeric.Value = 1280;
                    _heightNumeric.Value = 720;
                    break;
                case 2: // 1024x768
                    _widthNumeric.Value = 1024;
                    _heightNumeric.Value = 768;
                    break;
                case 3: // 800x600
                    _widthNumeric.Value = 800;
                    _heightNumeric.Value = 600;
                    break;
            }
        }
        UpdateCustomResolutionVisibility();
    }

    private void UpdateCustomResolutionVisibility()
    {
        _widthNumeric.Visible = _customResolution;
        _heightNumeric.Visible = _customResolution;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("Please enter a project name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            // Update SCADA project properties
            _scadaProject.Name = _nameTextBox.Text.Trim();
            _scadaProject.Type = (ScadaType)_typeComboBox.SelectedIndex;
            _scadaProject.Resolution = new Size((int)_widthNumeric.Value, (int)_heightNumeric.Value);
            _scadaProject.Version = _versionTextBox.Text.Trim();
            
            // Update startup screen
            if (_startupScreenComboBox.SelectedIndex == 0)
                _scadaProject.StartupScreen = null;
            else
            {
                string selectedItem = _startupScreenComboBox.Items[_startupScreenComboBox.SelectedIndex].ToString() ?? "";
                if (selectedItem.Contains("(") && selectedItem.Contains(")"))
                {
                    int startIdx = selectedItem.LastIndexOf("(") + 1;
                    int endIdx = selectedItem.LastIndexOf(")");
                    if (startIdx > 0 && endIdx > startIdx)
                        _scadaProject.StartupScreen = selectedItem.Substring(startIdx, endIdx - startIdx);
                }
            }

            // Target device (deploy/upload)
            var host = _targetDeviceHostTextBox.Text.Trim();
            _scadaProject.TargetDeviceHost = string.IsNullOrEmpty(host) ? null : host;
            _scadaProject.TargetDevicePort = (int)_targetDevicePortNumeric.Value;
        }
        base.OnFormClosing(e);
    }
}
