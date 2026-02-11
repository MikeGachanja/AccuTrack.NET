using System;
using System.Drawing;
using System.Windows.Forms;
using Designer.Modules.Project;

namespace Designer.Modules.Project;

/// <summary>
/// Dialog for creating a new SCADA project.
/// </summary>
public partial class ScadaProjectDialog : Form
{
    private TextBox _nameTextBox;
    private ComboBox _typeComboBox;
    private NumericUpDown _widthNumeric;
    private NumericUpDown _heightNumeric;
    private Button _okButton;
    private Button _cancelButton;

    public string ScadaName => _nameTextBox.Text;
    public ScadaType ScadaType => (ScadaType)_typeComboBox.SelectedIndex;
    public Size Resolution => new Size((int)_widthNumeric.Value, (int)_heightNumeric.Value);

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
        Size = new Size(400, 250);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(15)
        };

        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Name
        mainLayout.Controls.Add(new Label { Text = "Name:", Anchor = AnchorStyles.Left, AutoSize = false }, 0, 0);
        _nameTextBox = new TextBox { Dock = DockStyle.Fill };
        mainLayout.Controls.Add(_nameTextBox, 1, 0);

        // Type
        mainLayout.Controls.Add(new Label { Text = "Type:", Anchor = AnchorStyles.Left, AutoSize = false }, 0, 1);
        _typeComboBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _typeComboBox.Items.AddRange(new[] { "HMI", "PC Station", "BMS" });
        _typeComboBox.SelectedIndex = 0;
        mainLayout.Controls.Add(_typeComboBox, 1, 1);

        // Resolution Width
        mainLayout.Controls.Add(new Label { Text = "Width:", Anchor = AnchorStyles.Left, AutoSize = false }, 0, 2);
        _widthNumeric = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 320,
            Maximum = 7680,
            Value = 1920
        };
        mainLayout.Controls.Add(_widthNumeric, 1, 2);

        // Resolution Height
        mainLayout.Controls.Add(new Label { Text = "Height:", Anchor = AnchorStyles.Left, AutoSize = false }, 0, 3);
        _heightNumeric = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 240,
            Maximum = 4320,
            Value = 1080
        };
        mainLayout.Controls.Add(_heightNumeric, 1, 3);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        _okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(75, 25)
        };
        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(75, 25)
        };
        buttonPanel.Controls.Add(_okButton);
        buttonPanel.Controls.Add(_cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, 4);
        mainLayout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(mainLayout);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        _nameTextBox.TextChanged += (s, e) => ValidateInput();
        ValidateInput();
    }

    private void ValidateInput()
    {
        _okButton.Enabled = !string.IsNullOrWhiteSpace(_nameTextBox.Text);
    }
}
