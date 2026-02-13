using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace Designer.Modules.Tools;

/// <summary>
/// Dialog for customizing toolbars, menus, and keyboard shortcuts.
/// </summary>
public partial class CustomizeDialog : Form
{
    private TabControl _tabs;
    private TreeView _shortcutsTree;

    public CustomizeDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Customize";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };

        // Tab control
        _tabs = new TabControl { Dock = DockStyle.Fill };
        
        // Toolbars tab
        var toolbarsTab = new TabPage("Toolbars");
        var toolbarsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var toolbarsLabel = new Label
        {
            Text = "Toolbar customization will be available in a future update.",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };
        toolbarsLayout.Controls.Add(toolbarsLabel, 0, 0);
        toolbarsTab.Controls.Add(toolbarsLayout);
        _tabs.TabPages.Add(toolbarsTab);

        // Keyboard shortcuts tab
        var shortcutsTab = new TabPage("Keyboard Shortcuts");
        var shortcutsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        
        _shortcutsTree = new TreeView
        {
            Dock = DockStyle.Fill,
            ShowLines = true,
            ShowPlusMinus = true
        };
        
        // File group
        var fileGroup = new TreeNode("File");
        fileGroup.Nodes.Add(new TreeNode("New Project") { Tag = "Ctrl+N" });
        fileGroup.Nodes.Add(new TreeNode("Open Project") { Tag = "Ctrl+O" });
        fileGroup.Nodes.Add(new TreeNode("Save Project") { Tag = "Ctrl+S" });
        fileGroup.Nodes.Add(new TreeNode("Save Project As") { Tag = "Ctrl+Shift+S" });
        fileGroup.Nodes.Add(new TreeNode("Close Project") { Tag = "Ctrl+W" });
        fileGroup.Expand();
        
        // Build group
        var buildGroup = new TreeNode("Build");
        buildGroup.Nodes.Add(new TreeNode("Build Project") { Tag = "F7" });
        buildGroup.Nodes.Add(new TreeNode("Clean Project") { Tag = "Ctrl+Shift+F7" });
        buildGroup.Nodes.Add(new TreeNode("Rebuild Project") { Tag = "Ctrl+F7" });
        buildGroup.Expand();
        
        // Edit group
        var editGroup = new TreeNode("Edit");
        editGroup.Nodes.Add(new TreeNode("Undo") { Tag = "Ctrl+Z" });
        editGroup.Nodes.Add(new TreeNode("Redo") { Tag = "Ctrl+Y" });
        editGroup.Nodes.Add(new TreeNode("Cut") { Tag = "Ctrl+X" });
        editGroup.Nodes.Add(new TreeNode("Copy") { Tag = "Ctrl+C" });
        editGroup.Nodes.Add(new TreeNode("Paste") { Tag = "Ctrl+V" });
        editGroup.Expand();
        
        // View group
        var viewGroup = new TreeNode("View");
        viewGroup.Nodes.Add(new TreeNode("Zoom In") { Tag = "Ctrl++" });
        viewGroup.Nodes.Add(new TreeNode("Zoom Out") { Tag = "Ctrl+-" });
        viewGroup.Nodes.Add(new TreeNode("Reset Zoom") { Tag = "Ctrl+0" });
        viewGroup.Expand();
        
        _shortcutsTree.Nodes.Add(fileGroup);
        _shortcutsTree.Nodes.Add(buildGroup);
        _shortcutsTree.Nodes.Add(editGroup);
        _shortcutsTree.Nodes.Add(viewGroup);
        
        // Add columns for shortcuts (using custom drawing or ListView would be better, but TreeView is simpler)
        shortcutsLayout.Controls.Add(_shortcutsTree, 0, 0);
        
        var infoLabel = new Label
        {
            Text = "Double-click a shortcut to edit it. Changes will be saved when you click OK.",
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft
        };
        shortcutsLayout.Controls.Add(infoLabel, 0, 1);
        shortcutsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shortcutsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        
        shortcutsTab.Controls.Add(shortcutsLayout);
        _tabs.TabPages.Add(shortcutsTab);

        // Editor settings tab
        var editorTab = new TabPage("Editor");
        var editorLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        var editorLabel = new Label
        {
            Text = "Editor customization options will be available in a future update.",
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter
        };
        editorLayout.Controls.Add(editorLabel, 0, 0);
        editorTab.Controls.Add(editorLayout);
        _tabs.TabPages.Add(editorTab);

        mainLayout.Controls.Add(_tabs, 0, 0);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 40
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 23) };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 23) };
        
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        mainLayout.Controls.Add(buttonPanel, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        Controls.Add(mainLayout);

        AcceptButton = okButton;
        CancelButton = cancelButton;
        
        // Handle double-click to edit shortcuts
        _shortcutsTree.DoubleClick += (s, e) =>
        {
            if (_shortcutsTree.SelectedNode != null && _shortcutsTree.SelectedNode.Tag != null)
            {
                // TODO: Show dialog to edit shortcut
                MessageBox.Show($"Edit shortcut for '{_shortcutsTree.SelectedNode.Text}'\nCurrent: {_shortcutsTree.SelectedNode.Tag}\n\nShortcut editing will be fully implemented in a future update.",
                    "Edit Shortcut", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        };
    }
}
