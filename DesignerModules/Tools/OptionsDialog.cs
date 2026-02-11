using System;
using System.Windows.Forms;

namespace Designer.Modules.Tools;

/// <summary>
/// Options dialog for application settings.
/// </summary>
public partial class OptionsDialog : Form
{
    private TabControl _optionsTabs;
    private ComboBox _themeCombo;
    private CheckBox _autoSaveCheckBox;
    private NumericUpDown _autoSaveIntervalNumeric;
    private string _currentTheme = "Light";

    public event EventHandler<string>? ThemeChanged;

    public OptionsDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Options";
        Size = new System.Drawing.Size(600, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _optionsTabs = new TabControl { Dock = DockStyle.Fill };

        // General tab
        var generalTab = new TabPage("General");
        var generalLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10)
        };

        int row = 0;
        generalLayout.Controls.Add(new Label { Text = "Theme:", AutoSize = true }, 0, row);
        _themeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _themeCombo.Items.AddRange(new[] { "Light", "Dark", "System" });
        _themeCombo.SelectedIndex = 0;
        _themeCombo.SelectedIndexChanged += (s, e) =>
        {
            _currentTheme = _themeCombo.SelectedItem?.ToString() ?? "Light";
            ThemeChanged?.Invoke(this, _currentTheme);
        };
        generalLayout.Controls.Add(_themeCombo, 1, row++);

        _autoSaveCheckBox = new CheckBox { Text = "Enable Auto Save", AutoSize = true, Checked = true };
        generalLayout.Controls.Add(_autoSaveCheckBox, 0, row);
        generalLayout.SetColumnSpan(_autoSaveCheckBox, 2);
        row++;

        generalLayout.Controls.Add(new Label { Text = "Auto Save Interval (seconds):", AutoSize = true }, 0, row);
        _autoSaveIntervalNumeric = new NumericUpDown { Minimum = 10, Maximum = 3600, Value = 300, Dock = DockStyle.Fill };
        generalLayout.Controls.Add(_autoSaveIntervalNumeric, 1, row++);

        generalTab.Controls.Add(generalLayout);
        _optionsTabs.TabPages.Add(generalTab);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };
        var applyButton = new Button { Text = "Apply", Size = new System.Drawing.Size(75, 23) };
        applyButton.Click += (s, e) => ApplyTheme();

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(applyButton);

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        mainLayout.Controls.Add(_optionsTabs, 0, 0);
        mainLayout.Controls.Add(buttonPanel, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        Controls.Add(mainLayout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public string GetCurrentTheme() => _currentTheme;

    public void ApplyTheme()
    {
        ThemeChanged?.Invoke(this, _currentTheme);
    }
}
