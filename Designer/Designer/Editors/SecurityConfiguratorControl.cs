using System.Windows.Forms;

namespace Designer.Editors;

/// <summary>Security configurator: roles, users, permissions.</summary>
public class SecurityConfiguratorControl : UserControl
{
    private readonly TabControl _tabs;
    private readonly DataGridView _rolesGrid;
    private readonly DataGridView _usersGrid;
    private readonly BindingSource _rolesBinding = new();
    private readonly BindingSource _usersBinding = new();
    private readonly List<SecurityRoleEntry> _roles = new();
    private readonly List<SecurityUserEntry> _users = new();

    public SecurityConfiguratorControl()
    {
        _tabs = new TabControl { Dock = DockStyle.Fill };
        _rolesBinding.DataSource = _roles;
        _usersBinding.DataSource = _users;

        var rolesPage = new TabPage("Roles");
        _rolesGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            DataSource = _rolesBinding
        };
        _rolesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Role" });
        _rolesGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Permissions", HeaderText = "Permissions (comma-separated)" });
        var rolesToolbar = new ToolStrip { Dock = DockStyle.Top };
        var addRoleBtn = new ToolStripButton("Add role") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        addRoleBtn.Click += (_, _) =>
        {
            _roles.Add(new SecurityRoleEntry { Name = "Role" + (_roles.Count + 1), Permissions = "Read" });
            _rolesBinding.ResetBindings(false);
        };
        rolesToolbar.Items.Add(addRoleBtn);
        var delRoleBtn = new ToolStripButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        delRoleBtn.Click += (_, _) =>
        {
            if (_rolesGrid.CurrentRow?.DataBoundItem is SecurityRoleEntry e) { _roles.Remove(e); _rolesBinding.ResetBindings(false); }
        };
        rolesToolbar.Items.Add(delRoleBtn);
        rolesPage.Controls.Add(_rolesGrid);
        rolesPage.Controls.Add(rolesToolbar);
        _tabs.TabPages.Add(rolesPage);

        var usersPage = new TabPage("Users");
        _usersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            DataSource = _usersBinding
        };
        _usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Username", HeaderText = "Username" });
        _usersGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Role", HeaderText = "Role" });
        _usersGrid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Enabled", HeaderText = "Enabled" });
        var usersToolbar = new ToolStrip { Dock = DockStyle.Top };
        var addUserBtn = new ToolStripButton("Add user") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        addUserBtn.Click += (_, _) =>
        {
            _users.Add(new SecurityUserEntry { Username = "user" + (_users.Count + 1), Role = "Operator", Enabled = true });
            _usersBinding.ResetBindings(false);
        };
        usersToolbar.Items.Add(addUserBtn);
        var delUserBtn = new ToolStripButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        delUserBtn.Click += (_, _) =>
        {
            if (_usersGrid.CurrentRow?.DataBoundItem is SecurityUserEntry e) { _users.Remove(e); _usersBinding.ResetBindings(false); }
        };
        usersToolbar.Items.Add(delUserBtn);
        usersPage.Controls.Add(_usersGrid);
        usersPage.Controls.Add(usersToolbar);
        _tabs.TabPages.Add(usersPage);

        Controls.Add(_tabs);
    }

    private class SecurityRoleEntry
    {
        public string Name { get; set; } = "";
        public string Permissions { get; set; } = "Read";
    }

    private class SecurityUserEntry
    {
        public string Username { get; set; } = "";
        public string Role { get; set; } = "Operator";
        public bool Enabled { get; set; } = true;
    }
}
