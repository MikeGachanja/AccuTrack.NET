using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Project;

/// <summary>
/// Dialog for creating a new SCADA project (two-step wizard).
/// </summary>
public partial class ScadaProjectDialog : Form
{
    private Panel _step1Panel;
    private Panel _step2Panel;
    private TextBox _nameTextBox;
    private ComboBox _typeComboBox;
    private ComboBox _screenSizeComboBox;
    private NumericUpDown _widthNumeric;
    private NumericUpDown _heightNumeric;
    private Label _widthLabel;
    private Label _heightLabel;
    private Label _xLabel;
    private CheckBox _addAlarmsScreenCheckBox;
    private CheckBox _addLogsScreenCheckBox;
    private Button _nextButton;
    private Button _backButton;
    private Button _cancelButton;
    private Label _stepLabel;
    private int _currentStep = 1;
    private bool _customResolution = false;

    // Predefined screen sizes for HMI displays
    private readonly Dictionary<string, Size> _screenSizes = new Dictionary<string, Size>
    {
        { "8\"", new Size(800, 600) },
        { "10\"", new Size(1024, 600) },
        { "15\"", new Size(1366, 768) },
        { "21\"", new Size(1920, 1080) },
        { "Custom", Size.Empty } // Empty size indicates custom
    };

    public string ScadaName => _nameTextBox.Text;
    public ScadaType ScadaType => (ScadaType)_typeComboBox.SelectedIndex;
    public Size Resolution => new Size((int)_widthNumeric.Value, (int)_heightNumeric.Value);
    public bool AddAlarmsScreen => _addAlarmsScreenCheckBox.Checked;
    public bool AddLogsScreen => _addLogsScreenCheckBox.Checked;

    public ScadaProjectDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "New SCADA Project";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(450, 320);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(15)
        };

        // Step indicator
        _stepLabel = new Label
        {
            Text = "Step 1 of 2: Project Details",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 10)
        };
        mainLayout.Controls.Add(_stepLabel, 0, 0);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        // Content panel (will hold step panels)
        var contentPanel = new Panel { Dock = DockStyle.Fill };
        mainLayout.Controls.Add(contentPanel, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Step 1: Project Details
        _step1Panel = CreateStep1Panel();
        _step1Panel.Dock = DockStyle.Fill;
        contentPanel.Controls.Add(_step1Panel);

        // Step 2: Default Screens
        _step2Panel = CreateStep2Panel();
        _step2Panel.Dock = DockStyle.Fill;
        _step2Panel.Visible = false;
        contentPanel.Controls.Add(_step2Panel);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 0, 0)
        };
        _nextButton = new Button
        {
            Text = "Next",
            Size = new Size(75, 25)
        };
        _nextButton.Click += OnNextClicked;
        _backButton = new Button
        {
            Text = "Back",
            Size = new Size(75, 25),
            Enabled = false
        };
        _backButton.Click += OnBackClicked;
        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(75, 25)
        };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_backButton);
        buttonPanel.Controls.Add(_nextButton);
        mainLayout.Controls.Add(buttonPanel, 0, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

        Controls.Add(mainLayout);

        CancelButton = _cancelButton;

        _nameTextBox.TextChanged += (s, e) => ValidateInput();
        ValidateInput();
    }

    private Panel CreateStep1Panel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int row = 0;

        // Name
        layout.Controls.Add(new Label { Text = "Name:", Anchor = AnchorStyles.Left, AutoSize = false }, 0, row);
        _nameTextBox = new TextBox { Dock = DockStyle.Fill };
        layout.Controls.Add(_nameTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Type
        layout.Controls.Add(new Label { Text = "Type:", Anchor = AnchorStyles.Left, AutoSize = false }, 0, row);
        _typeComboBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _typeComboBox.Items.AddRange(new[] { "HMI", "PC Station", "BMS" });
        _typeComboBox.SelectedIndex = 0;
        layout.Controls.Add(_typeComboBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Screen Size
        layout.Controls.Add(new Label { Text = "Screen Size:", Anchor = AnchorStyles.Left, AutoSize = false }, 0, row);
        var resolutionLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        _screenSizeComboBox = new ComboBox
        {
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _screenSizeComboBox.Items.AddRange(new[] { "8\"", "10\"", "15\"", "21\"", "Custom" });
        _screenSizeComboBox.SelectedIndex = 3; // Default to 21"
        _screenSizeComboBox.SelectedIndexChanged += OnScreenSizeChanged;
        resolutionLayout.Controls.Add(_screenSizeComboBox);
        
        // Custom resolution fields (initially hidden)
        _widthLabel = new Label { Text = "Width:", AutoSize = true, Visible = false };
        _widthNumeric = new NumericUpDown
        {
            Width = 80,
            Minimum = 320,
            Maximum = 7680,
            Value = 1920,
            Visible = false
        };
        _xLabel = new Label { Text = "x", AutoSize = true, Visible = false };
        _heightLabel = new Label { Text = "Height:", AutoSize = true, Visible = false };
        _heightNumeric = new NumericUpDown
        {
            Width = 80,
            Minimum = 240,
            Maximum = 4320,
            Value = 1080,
            Visible = false
        };
        
        resolutionLayout.Controls.Add(_widthLabel);
        resolutionLayout.Controls.Add(_widthNumeric);
        resolutionLayout.Controls.Add(_xLabel);
        resolutionLayout.Controls.Add(_heightLabel);
        resolutionLayout.Controls.Add(_heightNumeric);
        
        layout.Controls.Add(resolutionLayout, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Empty row for spacing
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(layout);

        // Set initial resolution based on default selection (21")
        OnScreenSizeChanged(null, EventArgs.Empty);

        return panel;
    }

    private Panel CreateStep2Panel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0)
        };

        // Instructions
        var instructionsLabel = new Label
        {
            Text = "Would you like to add default screens to your project?",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 15)
        };
        layout.Controls.Add(instructionsLabel, 0, 0);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Checkboxes
        var checkboxesPanel = new Panel { Dock = DockStyle.Fill };
        _addAlarmsScreenCheckBox = new CheckBox
        {
            Text = "Alarms Screen",
            AutoSize = true,
            Location = new Point(0, 0),
            Checked = true
        };
        _addLogsScreenCheckBox = new CheckBox
        {
            Text = "Logs Screen",
            AutoSize = true,
            Location = new Point(0, 30),
            Checked = true
        };
        checkboxesPanel.Controls.Add(_addAlarmsScreenCheckBox);
        checkboxesPanel.Controls.Add(_addLogsScreenCheckBox);
        layout.Controls.Add(checkboxesPanel, 0, 1);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Empty row for spacing
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(layout);
        return panel;
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        if (_currentStep == 1)
        {
            // Validate step 1
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("Please enter a project name.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Move to step 2
            _currentStep = 2;
            _step1Panel.Visible = false;
            _step2Panel.Visible = true;
            _stepLabel.Text = "Step 2 of 2: Default Screens";
            _backButton.Enabled = true;
            _nextButton.Text = "Finish";
        }
        else if (_currentStep == 2)
        {
            // Finish - close dialog with OK
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        if (_currentStep == 2)
        {
            // Move back to step 1
            _currentStep = 1;
            _step2Panel.Visible = false;
            _step1Panel.Visible = true;
            _stepLabel.Text = "Step 1 of 2: Project Details";
            _backButton.Enabled = false;
            _nextButton.Text = "Next";
        }
    }

    private void OnScreenSizeChanged(object? sender, EventArgs e)
    {
        string selectedSize = _screenSizeComboBox.SelectedItem?.ToString() ?? "21\"";
        
        if (selectedSize == "Custom")
        {
            _customResolution = true;
            _widthLabel.Visible = true;
            _widthNumeric.Visible = true;
            _xLabel.Visible = true;
            _heightLabel.Visible = true;
            _heightNumeric.Visible = true;
        }
        else
        {
            _customResolution = false;
            _widthLabel.Visible = false;
            _widthNumeric.Visible = false;
            _xLabel.Visible = false;
            _heightLabel.Visible = false;
            _heightNumeric.Visible = false;
            
            // Set resolution based on selected screen size
            if (_screenSizes.TryGetValue(selectedSize, out Size size) && size != Size.Empty)
            {
                _widthNumeric.Value = size.Width;
                _heightNumeric.Value = size.Height;
            }
        }
    }

    private void ValidateInput()
    {
        _nextButton.Enabled = !string.IsNullOrWhiteSpace(_nameTextBox.Text);
    }
}
