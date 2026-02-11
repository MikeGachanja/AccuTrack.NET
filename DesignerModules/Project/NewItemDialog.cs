using System;
using System.Windows.Forms;

namespace Designer.Modules.Project;

/// <summary>
/// Simple dialog for entering a new item name.
/// </summary>
public partial class NewItemDialog : Form
{
    private TextBox _nameTextBox;
    private TextBox? _descriptionTextBox;

    public string ItemName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public NewItemDialog(string itemType, bool includeDescription = false)
    {
        InitializeComponent(itemType, includeDescription);
    }

    private void InitializeComponent(string itemType, bool includeDescription)
    {
        Text = $"New {itemType}";
        Size = new System.Drawing.Size(400, includeDescription ? 180 : 120);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = includeDescription ? 3 : 2,
            Padding = new Padding(10)
        };

        mainLayout.Controls.Add(new Label { Text = $"{itemType} Name:", AutoSize = true }, 0, 0);
        _nameTextBox = new TextBox { Dock = DockStyle.Fill };
        _nameTextBox.Text = $"New{itemType}";
        _nameTextBox.SelectAll();
        mainLayout.Controls.Add(_nameTextBox, 1, 0);

        if (includeDescription)
        {
            mainLayout.Controls.Add(new Label { Text = "Description:", AutoSize = true }, 0, 1);
            _descriptionTextBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 50 };
            mainLayout.Controls.Add(_descriptionTextBox, 1, 1);
        }

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };
        okButton.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show($"Please enter a {itemType} name.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            ItemName = _nameTextBox.Text.Trim();
            Description = _descriptionTextBox?.Text ?? string.Empty;
            DialogResult = DialogResult.OK;
        };

        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, includeDescription ? 2 : 1);
        mainLayout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(mainLayout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
