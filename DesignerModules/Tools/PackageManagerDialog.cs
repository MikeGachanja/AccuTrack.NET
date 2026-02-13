using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace Designer.Modules.Tools;

/// <summary>
/// Dialog for managing packages/extensions.
/// </summary>
public partial class PackageManagerDialog : Form
{
    private TreeView _packagesTree;
    private Button _installButton;
    private Button _uninstallButton;
    private Button _refreshButton;
    private TextBox _searchEdit;
    private TextBox _detailsText;
    private Label _statusLabel;
    private List<PackageInfo> _packages = new List<PackageInfo>();
    private Dictionary<TreeNode, int> _nodeToPackageIndex = new Dictionary<TreeNode, int>();

    public PackageManagerDialog()
    {
        InitializeComponent();
        LoadPackages();
    }

    private void InitializeComponent()
    {
        Text = "Package Manager";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };

        // Search bar
        var searchLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 30
        };
        searchLayout.Controls.Add(new Label { Text = "Search:", AutoSize = true });
        _searchEdit = new TextBox { Width = 300 };
        _searchEdit.TextChanged += (s, e) => FilterPackages(_searchEdit.Text);
        searchLayout.Controls.Add(_searchEdit);
        _refreshButton = new Button { Text = "Refresh", Width = 80 };
        _refreshButton.Click += (s, e) => LoadPackages();
        searchLayout.Controls.Add(_refreshButton);
        searchLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }); // Spacer
        mainLayout.Controls.Add(searchLayout, 0, 0);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        // Splitter for packages and details
        var splitter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal
        };

        // Packages tree
        var packagesPanel = new Panel { Dock = DockStyle.Fill };
        var packagesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };

        _packagesTree = new TreeView
        {
            Dock = DockStyle.Fill,
            ShowLines = true,
            ShowPlusMinus = true,
            FullRowSelect = true
        };
        _packagesTree.AfterSelect += (s, e) => OnPackageSelected();
        packagesLayout.Controls.Add(_packagesTree, 0, 0);
        packagesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Action buttons
        var actionLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 35
        };
        _installButton = new Button { Text = "Install", Width = 100 };
        _installButton.Click += (s, e) => OnInstallPackage();
        _uninstallButton = new Button { Text = "Uninstall", Width = 100 };
        _uninstallButton.Click += (s, e) => OnUninstallPackage();
        actionLayout.Controls.Add(_installButton);
        actionLayout.Controls.Add(_uninstallButton);
        actionLayout.Controls.Add(new Panel { Dock = DockStyle.Fill }); // Spacer
        packagesLayout.Controls.Add(actionLayout, 0, 1);
        packagesLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));

        packagesPanel.Controls.Add(packagesLayout);
        splitter.Panel1.Controls.Add(packagesPanel);

        // Details panel
        var detailsPanel = new Panel { Dock = DockStyle.Fill };
        var detailsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(5)
        };
        detailsLayout.Controls.Add(new Label { Text = "Package Details:", AutoSize = true }, 0, 0);
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        _detailsText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical
        };
        detailsLayout.Controls.Add(_detailsText, 0, 1);
        detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detailsPanel.Controls.Add(detailsLayout);
        splitter.Panel2.Controls.Add(detailsPanel);
        splitter.SplitterDistance = 300;

        mainLayout.Controls.Add(splitter, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Status label
        _statusLabel = new Label
        {
            Text = "Ready",
            Dock = DockStyle.Fill,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft
        };
        mainLayout.Controls.Add(_statusLabel, 0, 2);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));

        // Dialog buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };
        var closeButton = new Button { Text = "Close", DialogResult = DialogResult.Cancel, Size = new Size(75, 23) };
        buttonPanel.Controls.Add(closeButton);
        mainLayout.Controls.Add(buttonPanel, 0, 3);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        Controls.Add(mainLayout);

        CancelButton = closeButton;
    }

    private void LoadPackages()
    {
        _packages.Clear();
        _packagesTree.Nodes.Clear();
        _nodeToPackageIndex.Clear();

        // Sample packages (in real implementation, load from package repository)
        _packages.Add(new PackageInfo
        {
            Name = "Modbus Driver",
            Version = "1.2.0",
            Description = "Enhanced Modbus communication driver with support for multiple protocols",
            Category = "Communication",
            Installed = true
        });

        _packages.Add(new PackageInfo
        {
            Name = "OPC UA Client",
            Version = "2.0.1",
            Description = "OPC UA client library with advanced security features",
            Category = "Communication",
            Installed = false
        });

        _packages.Add(new PackageInfo
        {
            Name = "Advanced Charts",
            Version = "1.5.3",
            Description = "Advanced charting components for data visualization",
            Category = "Components",
            Installed = false
        });

        _packages.Add(new PackageInfo
        {
            Name = "Database Connector",
            Version = "1.0.0",
            Description = "Connect to SQL Server, MySQL, and PostgreSQL databases",
            Category = "Data",
            Installed = true
        });

        UpdatePackageTree();
        _statusLabel.Text = $"Loaded {_packages.Count} packages";
    }

    private void UpdatePackageTree()
    {
        _packagesTree.Nodes.Clear();
        _nodeToPackageIndex.Clear();

        // Group by category
        var categories = _packages.GroupBy(p => p.Category).OrderBy(g => g.Key);

        foreach (var categoryGroup in categories)
        {
            var categoryNode = new TreeNode(categoryGroup.Key);
            
            foreach (var package in categoryGroup.OrderBy(p => p.Name))
            {
                var packageNode = new TreeNode($"{package.Name} ({package.Version})")
                {
                    Tag = package.Installed ? "Installed" : "Available"
                };
                
                // Add status as sub-node
                var statusNode = new TreeNode(package.Installed ? "Installed" : "Available");
                packageNode.Nodes.Add(statusNode);
                
                categoryNode.Nodes.Add(packageNode);
                _nodeToPackageIndex[packageNode] = _packages.IndexOf(package);
            }
            
            categoryNode.Expand();
            _packagesTree.Nodes.Add(categoryNode);
        }
    }

    private void FilterPackages(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            UpdatePackageTree();
            return;
        }

        _packagesTree.Nodes.Clear();
        _nodeToPackageIndex.Clear();

        filter = filter.ToLower();
        var filteredPackages = _packages.Where(p =>
            p.Name.ToLower().Contains(filter) ||
            p.Description.ToLower().Contains(filter) ||
            p.Category.ToLower().Contains(filter)).ToList();

        var categories = filteredPackages.GroupBy(p => p.Category).OrderBy(g => g.Key);

        foreach (var categoryGroup in categories)
        {
            var categoryNode = new TreeNode(categoryGroup.Key);
            
            foreach (var package in categoryGroup.OrderBy(p => p.Name))
            {
                var packageNode = new TreeNode($"{package.Name} ({package.Version})")
                {
                    Tag = package.Installed ? "Installed" : "Available"
                };
                var statusNode = new TreeNode(package.Installed ? "Installed" : "Available");
                packageNode.Nodes.Add(statusNode);
                categoryNode.Nodes.Add(packageNode);
                _nodeToPackageIndex[packageNode] = _packages.IndexOf(package);
            }
            
            categoryNode.Expand();
            _packagesTree.Nodes.Add(categoryNode);
        }
    }

    private void OnPackageSelected()
    {
        if (_packagesTree.SelectedNode == null || !_nodeToPackageIndex.ContainsKey(_packagesTree.SelectedNode))
        {
            _detailsText.Text = "";
            _installButton.Enabled = false;
            _uninstallButton.Enabled = false;
            return;
        }

        int index = _nodeToPackageIndex[_packagesTree.SelectedNode];
        var package = _packages[index];

        _detailsText.Text = $"Name: {package.Name}\n" +
                           $"Version: {package.Version}\n" +
                           $"Category: {package.Category}\n" +
                           $"Status: {(package.Installed ? "Installed" : "Available")}\n\n" +
                           $"Description:\n{package.Description}";

        _installButton.Enabled = !package.Installed;
        _uninstallButton.Enabled = package.Installed;
    }

    private void OnInstallPackage()
    {
        if (_packagesTree.SelectedNode == null || !_nodeToPackageIndex.ContainsKey(_packagesTree.SelectedNode))
            return;

        int index = _nodeToPackageIndex[_packagesTree.SelectedNode];
        var package = _packages[index];

        var result = MessageBox.Show(
            $"Install package '{package.Name}' version {package.Version}?",
            "Install Package",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            // TODO: Implement actual package installation
            package.Installed = true;
            UpdatePackageTree();
            _statusLabel.Text = $"Package '{package.Name}' installed successfully";
            MessageBox.Show($"Package '{package.Name}' has been installed.\n\nNote: Package installation will be fully implemented in a future update.",
                "Installation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OnUninstallPackage()
    {
        if (_packagesTree.SelectedNode == null || !_nodeToPackageIndex.ContainsKey(_packagesTree.SelectedNode))
            return;

        int index = _nodeToPackageIndex[_packagesTree.SelectedNode];
        var package = _packages[index];

        var result = MessageBox.Show(
            $"Uninstall package '{package.Name}' version {package.Version}?",
            "Uninstall Package",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            // TODO: Implement actual package uninstallation
            package.Installed = false;
            UpdatePackageTree();
            _statusLabel.Text = $"Package '{package.Name}' uninstalled successfully";
            MessageBox.Show($"Package '{package.Name}' has been uninstalled.\n\nNote: Package uninstallation will be fully implemented in a future update.",
                "Uninstallation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

/// <summary>
/// Represents package information.
/// </summary>
public class PackageInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Installed { get; set; } = false;
}
