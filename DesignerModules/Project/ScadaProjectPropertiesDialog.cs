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
    private TextBox _nameTextBox;
    private ComboBox _typeComboBox;
    private ComboBox _resolutionComboBox;
    private NumericUpDown _widthNumeric;
    private NumericUpDown _heightNumeric;
    private TextBox _versionTextBox;
    private Label _pathLabel;
    private bool _customResolution = false;

    public ScadaProjectPropertiesDialog(ScadaProject scadaProject)
    {
        _scadaProject = scadaProject ?? throw new ArgumentNullException(nameof(scadaProject));
        InitializeComponent();
        LoadProperties();
    }

    private void InitializeComponent()
    {
        Text = $"SCADA Project Properties - {_scadaProject.Name}";
        Size = new Size(500, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(10)
        };

        int row = 0;

        // Name
        mainLayout.Controls.Add(new Label { Text = "Name:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _nameTextBox = new TextBox { Text = _scadaProject.Name, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_nameTextBox, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Type
        mainLayout.Controls.Add(new Label { Text = "Type:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _typeComboBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _typeComboBox.Items.AddRange(new[] { "HMI", "PC Station", "BMS" });
        _typeComboBox.SelectedIndex = (int)_scadaProject.Type;
        mainLayout.Controls.Add(_typeComboBox, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Resolution
        mainLayout.Controls.Add(new Label { Text = "Resolution:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        var resolutionLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _resolutionComboBox = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        _resolutionComboBox.Items.AddRange(new[] { "1920x1080 (Full HD)", "1280x720 (HD)", "1024x768 (XGA)", "800x600 (SVGA)", "Custom..." });
        _resolutionComboBox.SelectedIndexChanged += OnResolutionComboBoxChanged;
        resolutionLayout.Controls.Add(_resolutionComboBox);
        _widthNumeric = new NumericUpDown { Width = 80, Minimum = 100, Maximum = 10000, Value = _scadaProject.Resolution.Width };
        _heightNumeric = new NumericUpDown { Width = 80, Minimum = 100, Maximum = 10000, Value = _scadaProject.Resolution.Height };
        var xLabel = new Label { Text = "x", AutoSize = true, Anchor = AnchorStyles.Left };
        resolutionLayout.Controls.Add(_widthNumeric);
        resolutionLayout.Controls.Add(xLabel);
        resolutionLayout.Controls.Add(_heightNumeric);
        UpdateResolutionComboBox();
        UpdateCustomResolutionVisibility();
        mainLayout.Controls.Add(resolutionLayout, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Version
        mainLayout.Controls.Add(new Label { Text = "Version:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _versionTextBox = new TextBox { Text = _scadaProject.Version, Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_versionTextBox, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Path (read-only)
        mainLayout.Controls.Add(new Label { Text = "Path:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _pathLabel = new Label { Text = _scadaProject.Path, AutoSize = false, Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(3) };
        mainLayout.Controls.Add(_pathLabel, 1, row);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Spacer
        mainLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, row);
        mainLayout.SetColumnSpan(mainLayout.Controls[mainLayout.Controls.Count - 1], 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        row++;

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };
        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 23) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 23) };
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, row);
        mainLayout.SetColumnSpan(buttonPanel, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        Controls.Add(mainLayout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void LoadProperties()
    {
        _nameTextBox.Text = _scadaProject.Name;
        _typeComboBox.SelectedIndex = (int)_scadaProject.Type;
        _versionTextBox.Text = _scadaProject.Version;
        _pathLabel.Text = _scadaProject.Path;
        UpdateResolutionComboBox();
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
        }
        base.OnFormClosing(e);
    }
}
