using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Security;
using Designer.Modules.Project;

namespace Designer.Modules.Security;

/// <summary>
/// Editor for security configuration.
/// </summary>
public partial class SecurityEditor : UserControl
{
    private TabControl _mainTabs;
    private DataGridView _usersGrid;
    private DataGridView _rolesGrid;
    private Security? _security;
    private ScadaProject? _scadaProject;
    private bool _isModified = false;

    // Settings controls
    private CheckBox _enabledCheckBox;
    private NumericUpDown _sessionTimeoutNumeric;
    private CheckBox _requirePasswordChangeCheckBox;
    private NumericUpDown _passwordMinLengthNumeric;

    public bool IsModified => _isModified;

    public SecurityEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _mainTabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Users tab
        var usersTab = new TabPage("Users");
        var usersLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };

        var usersToolbar = new ToolStrip { Dock = DockStyle.Top };
        var addUserButton = new ToolStripButton("Add User");
        addUserButton.Click += (s, e) => AddUser();
        usersToolbar.Items.Add(addUserButton);
        var removeUserButton = new ToolStripButton("Remove User");
        removeUserButton.Click += (s, e) => RemoveSelectedUser();
        usersToolbar.Items.Add(removeUserButton);

        _usersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _usersGrid.Columns.Add("Username", "Username");
        _usersGrid.Columns.Add("FullName", "Full Name");
        _usersGrid.Columns.Add("Email", "Email");
        _usersGrid.Columns.Add("RoleName", "Role");
        _usersGrid.Columns.Add("Enabled", "Enabled");

        // Role column as combo box
        var roleColumn = new DataGridViewComboBoxColumn
        {
            Name = "RoleName",
            HeaderText = "Role",
            DataPropertyName = "RoleName"
        };
        _usersGrid.Columns.Remove("RoleName");
        _usersGrid.Columns.Insert(3, roleColumn);

        // Enabled checkbox
        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            DataPropertyName = "Enabled"
        };
        _usersGrid.Columns.Remove("Enabled");
        _usersGrid.Columns.Add(enabledColumn);

        _usersGrid.CellValueChanged += OnUserCellValueChanged;

        usersLayout.Controls.Add(usersToolbar, 0, 0);
        usersLayout.Controls.Add(_usersGrid, 0, 1);
        usersLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        usersLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        usersTab.Controls.Add(usersLayout);

        // Roles tab
        var rolesTab = new TabPage("Roles");
        var rolesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };

        var rolesToolbar = new ToolStrip { Dock = DockStyle.Top };
        var addRoleButton = new ToolStripButton("Add Role");
        addRoleButton.Click += (s, e) => AddRole();
        rolesToolbar.Items.Add(addRoleButton);
        var removeRoleButton = new ToolStripButton("Remove Role");
        removeRoleButton.Click += (s, e) => RemoveSelectedRole();
        rolesToolbar.Items.Add(removeRoleButton);

        _rolesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        _rolesGrid.Columns.Add("Name", "Name");
        _rolesGrid.Columns.Add("Description", "Description");
        _rolesGrid.Columns.Add("Permissions", "Permissions");

        _rolesGrid.CellValueChanged += OnRoleCellValueChanged;

        rolesLayout.Controls.Add(rolesToolbar, 0, 0);
        rolesLayout.Controls.Add(_rolesGrid, 0, 1);
        rolesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        rolesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rolesTab.Controls.Add(rolesLayout);

        // Settings tab
        var settingsTab = new TabPage("Settings");
        var settingsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(10)
        };

        _enabledCheckBox = new CheckBox { Text = "Enable Security", AutoSize = true };
        _enabledCheckBox.CheckedChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_enabledCheckBox, 0, 0);
        settingsLayout.SetColumnSpan(_enabledCheckBox, 2);

        settingsLayout.Controls.Add(new Label { Text = "Session Timeout (minutes):", AutoSize = true }, 0, 1);
        _sessionTimeoutNumeric = new NumericUpDown { Minimum = 1, Maximum = 1440, Value = 30, Dock = DockStyle.Fill };
        _sessionTimeoutNumeric.ValueChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_sessionTimeoutNumeric, 1, 1);

        _requirePasswordChangeCheckBox = new CheckBox { Text = "Require Password Change", AutoSize = true };
        _requirePasswordChangeCheckBox.CheckedChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_requirePasswordChangeCheckBox, 0, 2);
        settingsLayout.SetColumnSpan(_requirePasswordChangeCheckBox, 2);

        settingsLayout.Controls.Add(new Label { Text = "Minimum Password Length:", AutoSize = true }, 0, 3);
        _passwordMinLengthNumeric = new NumericUpDown { Minimum = 4, Maximum = 32, Value = 8, Dock = DockStyle.Fill };
        _passwordMinLengthNumeric.ValueChanged += (s, e) => _isModified = true;
        settingsLayout.Controls.Add(_passwordMinLengthNumeric, 1, 3);

        settingsTab.Controls.Add(settingsLayout);

        _mainTabs.TabPages.Add(usersTab);
        _mainTabs.TabPages.Add(rolesTab);
        _mainTabs.TabPages.Add(settingsTab);

        Controls.Add(_mainTabs);
    }

    public void SetSecurity(Security security)
    {
        _security = security;
        LoadSecurity();
        _isModified = false;
    }

    public void SetScadaProject(ScadaProject project)
    {
        _scadaProject = project;
    }

    private void LoadSecurity()
    {
        if (_security == null)
            _security = new Security();

        // Load settings
        _enabledCheckBox.Checked = _security.Enabled;
        _sessionTimeoutNumeric.Value = _security.SessionTimeoutMinutes;
        _requirePasswordChangeCheckBox.Checked = _security.RequirePasswordChange;
        _passwordMinLengthNumeric.Value = _security.PasswordMinLength;

        // Load users
        _usersGrid.Rows.Clear();
        foreach (var user in _security.Users)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_usersGrid);
            row.Cells[0].Value = user.Username;
            row.Cells[1].Value = user.FullName;
            row.Cells[2].Value = user.Email;
            row.Cells[3].Value = user.RoleName;
            row.Cells[4].Value = user.Enabled;
            row.Tag = user;
            _usersGrid.Rows.Add(row);
        }

        // Update role combo box
        if (_usersGrid.Columns["RoleName"] is DataGridViewComboBoxColumn roleColumn)
        {
            roleColumn.Items.Clear();
            roleColumn.Items.AddRange(_security.Roles.Select(r => r.Name).ToArray());
        }

        // Load roles
        _rolesGrid.Rows.Clear();
        foreach (var role in _security.Roles)
        {
            var row = new DataGridViewRow();
            row.CreateCells(_rolesGrid);
            row.Cells[0].Value = role.Name;
            row.Cells[1].Value = role.Description;
            row.Cells[2].Value = string.Join(", ", role.Permissions);
            row.Tag = role;
            _rolesGrid.Rows.Add(row);
        }
    }

    private void AddUser()
    {
        if (_security == null)
            _security = new Security();

        var newUser = new User
        {
            Username = $"user{_security.Users.Count + 1}",
            RoleName = _security.Roles.FirstOrDefault()?.Name ?? string.Empty,
            Enabled = true
        };

        _security.Users.Add(newUser);
        LoadSecurity();
        _isModified = true;
    }

    private void RemoveSelectedUser()
    {
        if (_usersGrid.SelectedRows.Count == 0 || _security == null)
            return;

        var selectedRow = _usersGrid.SelectedRows[0];
        if (selectedRow.Tag is User user)
        {
            _security.Users.Remove(user);
            LoadSecurity();
            _isModified = true;
        }
    }

    private void AddRole()
    {
        if (_security == null)
            _security = new Security();

        var newRole = new Role
        {
            Name = $"Role{_security.Roles.Count + 1}",
            Description = string.Empty
        };

        _security.Roles.Add(newRole);
        LoadSecurity();
        _isModified = true;
    }

    private void RemoveSelectedRole()
    {
        if (_rolesGrid.SelectedRows.Count == 0 || _security == null)
            return;

        var selectedRow = _rolesGrid.SelectedRows[0];
        if (selectedRow.Tag is Role role)
        {
            _security.Roles.Remove(role);
            LoadSecurity();
            _isModified = true;
        }
    }

    private void OnUserCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _usersGrid.Rows[e.RowIndex];
        if (row.Tag is not User user)
            return;

        var column = _usersGrid.Columns[e.ColumnIndex];
        var value = row.Cells[e.ColumnIndex].Value;

        switch (column.Name)
        {
            case "Username":
                user.Username = value?.ToString() ?? string.Empty;
                break;
            case "FullName":
                user.FullName = value?.ToString() ?? string.Empty;
                break;
            case "Email":
                user.Email = value?.ToString() ?? string.Empty;
                break;
            case "RoleName":
                user.RoleName = value?.ToString() ?? string.Empty;
                break;
            case "Enabled":
                user.Enabled = value is bool enabled ? enabled : true;
                break;
        }

        _isModified = true;
    }

    private void OnRoleCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var row = _rolesGrid.Rows[e.RowIndex];
        if (row.Tag is not Role role)
            return;

        var column = _rolesGrid.Columns[e.ColumnIndex];
        var value = row.Cells[e.ColumnIndex].Value;

        switch (column.Name)
        {
            case "Name":
                role.Name = value?.ToString() ?? string.Empty;
                break;
            case "Description":
                role.Description = value?.ToString() ?? string.Empty;
                break;
            case "Permissions":
                var permissionsStr = value?.ToString() ?? string.Empty;
                role.Permissions = permissionsStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()).ToList();
                break;
        }

        _isModified = true;
    }

    public Security? GetSecurity()
    {
        if (_security == null)
            return null;

        _security.Enabled = _enabledCheckBox.Checked;
        _security.SessionTimeoutMinutes = (int)_sessionTimeoutNumeric.Value;
        _security.RequirePasswordChange = _requirePasswordChangeCheckBox.Checked;
        _security.PasswordMinLength = (int)_passwordMinLengthNumeric.Value;

        return _security;
    }

    /// <summary>
    /// Resets the modified flag (called after successful save).
    /// </summary>
    public void ResetModified()
    {
        _isModified = false;
    }
}
