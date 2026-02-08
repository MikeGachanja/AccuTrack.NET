using System.Windows.Forms;

namespace AccuTrack.Project;

/// <summary>
/// Tree view of project: screens, tag tables, scripts, communication, alarms, schedules, historian, etc.
/// Double-click raises NodeActivated so MainForm can open the correct editor tab.
/// Context menu: Add Screen / Tag Table / Script, Delete.
/// </summary>
public class ProjectView : TreeView
{
    public event EventHandler<ProjectNodeEventArgs>? NodeActivated;
    public event EventHandler? AddScreenRequested;
    public event EventHandler? AddTagTableRequested;
    public event EventHandler? AddScriptRequested;
    public event EventHandler<ProjectNodeEventArgs>? DeleteNodeRequested;

    private TreeNode? _screensFolder;
    private TreeNode? _tagTablesFolder;
    private TreeNode? _scriptsFolder;

    public ProjectView()
    {
        FullRowSelect = true;
        HideSelection = false;
        AfterSelect += OnAfterSelect;
        NodeMouseDoubleClick += OnNodeMouseDoubleClick;
        ContextMenuStrip = BuildContextMenu();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        var addScreen = new ToolStripMenuItem("Add Screen");
        addScreen.Click += (_, _) => AddScreenRequested?.Invoke(this, EventArgs.Empty);
        var addTagTable = new ToolStripMenuItem("Add Tag Table");
        addTagTable.Click += (_, _) => AddTagTableRequested?.Invoke(this, EventArgs.Empty);
        var addScript = new ToolStripMenuItem("Add Script");
        addScript.Click += (_, _) => AddScriptRequested?.Invoke(this, EventArgs.Empty);
        var sep = new ToolStripSeparator();
        var delete = new ToolStripMenuItem("Delete");
        delete.Click += (_, _) =>
        {
            if (SelectedNode?.Tag is ProjectNode node)
                DeleteNodeRequested?.Invoke(this, new ProjectNodeEventArgs(node));
        };
        menu.Opening += (_, e) =>
        {
            var node = SelectedNode;
            var tag = node?.Tag;
            addScreen.Visible = tag is IList<ScreenNode>;
            addTagTable.Visible = tag is IList<TagTableNode>;
            addScript.Visible = tag is IList<ScriptNode>;
            delete.Visible = tag is ProjectNode pn && pn is not AlarmsNode and not SchedulesNode and not HistorianNode and not SecurityNode and not MachineLearningNode and not DeviceNetworkNode;
        };
        menu.Items.AddRange(new ToolStripItem[] { addScreen, addTagTable, addScript, sep, delete });
        return menu;
    }

    public void Bind(ScadaProject? project)
    {
        Nodes.Clear();
        if (project == null) return;

        var root = new TreeNode(project.Name) { Tag = project };
        Nodes.Add(root);

        _screensFolder = new TreeNode("Screens") { Tag = project.Screens };
        foreach (var s in project.Screens)
            _screensFolder.Nodes.Add(new TreeNode(s.Name) { Tag = s });
        root.Nodes.Add(_screensFolder);

        _tagTablesFolder = new TreeNode("Tag Tables") { Tag = project.TagTables };
        foreach (var t in project.TagTables)
            _tagTablesFolder.Nodes.Add(new TreeNode(t.Name) { Tag = t });
        root.Nodes.Add(_tagTablesFolder);

        _scriptsFolder = new TreeNode("Scripts") { Tag = project.Scripts };
        foreach (var s in project.Scripts)
            _scriptsFolder.Nodes.Add(new TreeNode(s.Name) { Tag = s });
        root.Nodes.Add(_scriptsFolder);

        var comm = new TreeNode("Communication") { Tag = project.CommunicationModules };
        foreach (var c in project.CommunicationModules)
            comm.Nodes.Add(new TreeNode(c.Name) { Tag = c });
        root.Nodes.Add(comm);

        if (project.Alarms != null)
            root.Nodes.Add(new TreeNode("Alarms") { Tag = project.Alarms });
        if (project.Schedules != null)
            root.Nodes.Add(new TreeNode("Schedules") { Tag = project.Schedules });
        if (project.Historian != null)
            root.Nodes.Add(new TreeNode("Historian") { Tag = project.Historian });
        if (project.Security != null)
            root.Nodes.Add(new TreeNode("Security") { Tag = project.Security });
        if (project.MachineLearning != null)
            root.Nodes.Add(new TreeNode("Machine Learning") { Tag = project.MachineLearning });
        if (project.DeviceNetwork != null)
            root.Nodes.Add(new TreeNode("Device Network") { Tag = project.DeviceNetwork });

        root.Expand();
    }

    /// <summary>Refresh tree after adding/removing nodes (call Bind(project) from MainForm instead, or this).</summary>
    public void RefreshNode(TreeNode? folder)
    {
        if (folder?.Tag == null) return;
        folder.Nodes.Clear();
        if (folder.Tag is IList<ScreenNode> screens)
            foreach (var s in screens)
                folder.Nodes.Add(new TreeNode(s.Name) { Tag = s });
        else if (folder.Tag is IList<TagTableNode> tagTables)
            foreach (var t in tagTables)
                folder.Nodes.Add(new TreeNode(t.Name) { Tag = t });
        else if (folder.Tag is IList<ScriptNode> scripts)
            foreach (var s in scripts)
                folder.Nodes.Add(new TreeNode(s.Name) { Tag = s });
    }

    private void OnAfterSelect(object? sender, TreeViewEventArgs e)
    {
        // Selection for property editor can be wired here
    }

    private void OnNodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        var tag = e.Node?.Tag;
        if (tag is ProjectNode node)
            NodeActivated?.Invoke(this, new ProjectNodeEventArgs(node));
    }
}

public class ProjectNodeEventArgs : EventArgs
{
    public ProjectNode Node { get; }

    public ProjectNodeEventArgs(ProjectNode node)
    {
        Node = node;
    }
}
