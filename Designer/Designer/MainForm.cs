using AccuTrack.Compiler;
using AccuTrack.Components;
using AccuTrack.Project;
using AccuTrack.ScreenEditor;
using AccuTrack.TagEngine;
using Designer.Editors;

namespace Designer;

public partial class MainForm : Form
{
    private readonly ProjectManager _projectManager = new();
    private readonly BuildService _buildService = new();
    private readonly TabControl _editorTabControl;
    private readonly ProjectView _projectView;
    private readonly PropertyGrid _propertyGrid;
    private readonly TextBox _consoleBox;

    // Proportional layout: fractions of available width/height (0.0–1.0)
    private const double ProjectTreeFraction = 0.20;   // left panel width
    private const double PropertyGridFraction = 0.22; // right panel width
    private const double ConsoleFraction = 0.22;     // bottom panel height
    private const int ProjectTreeMinWidth = 160;
    private const int PropertyGridMinWidth = 180;
    private const int ConsoleMinHeight = 80;

    private SplitContainer? _mainSplit;
    private SplitContainer? _centerRightSplit;
    private SplitContainer? _bottomSplit;
    private Panel? _contentPanel;

    public MainForm()
    {
        InitializeComponent();
        _projectView = new ProjectView { Dock = DockStyle.Fill };
        _propertyGrid = new PropertyGrid { Dock = DockStyle.Fill };
        _editorTabControl = new TabControl { Dock = DockStyle.Fill };
        _editorTabControl.ControlAdded += (_, _) => EnsureTabCloseMenu();
        _editorTabControl.SelectedIndexChanged += (_, _) =>
        {
            if (_editorTabControl.SelectedTab?.Tag is ProjectNode node)
            {
                _propertyGrid.SelectedObject = node;
                SetStatus($"Editing: {node.Name}");
            }
        };
        _consoleBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9f)
        };

        BuildLayout();
        WireProjectEvents();
        WireMenuAndToolbar();
        UpdateProjectDependentState();
        Load += MainForm_Load;
        Resize += MainForm_Resize;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        ApplyProportionalLayout();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized) return;
        ApplyProportionalLayout();
    }

    private void ApplyProportionalLayout()
    {
        if (_contentPanel == null || _mainSplit == null || _centerRightSplit == null || _bottomSplit == null) return;
        var w = _contentPanel.ClientSize.Width;
        var h = _contentPanel.ClientSize.Height;
        if (w <= 0 || h <= 0) return;

        // Set SplitterDistance first (Panel2MinSize is 0 in BuildLayout so validation passes).
        // Then restore Panel2MinSize so panels can't be collapsed.
        const int mainPanel2Min = 200;
        var projectWidth = Math.Max(ProjectTreeMinWidth, (int)(w * ProjectTreeFraction));
        projectWidth = Math.Min(projectWidth, w - _mainSplit.SplitterWidth - mainPanel2Min);
        if (projectWidth >= _mainSplit.Panel1MinSize && projectWidth < w - _mainSplit.SplitterWidth)
            _mainSplit.SplitterDistance = projectWidth;

        var editorWidth = w - projectWidth - _mainSplit.SplitterWidth - _centerRightSplit.SplitterWidth;
        var propWidth = Math.Max(PropertyGridMinWidth, (int)(w * PropertyGridFraction));
        propWidth = Math.Min(propWidth, editorWidth - PropertyGridMinWidth);
        var centerSplitDistance = editorWidth - propWidth;
        if (centerSplitDistance >= _centerRightSplit.Panel1MinSize && centerSplitDistance < editorWidth - _centerRightSplit.SplitterWidth)
            _centerRightSplit.SplitterDistance = centerSplitDistance;

        var consoleHeight = Math.Max(ConsoleMinHeight, (int)(h * ConsoleFraction));
        consoleHeight = Math.Min(consoleHeight, h - _bottomSplit.SplitterWidth - ConsoleMinHeight);
        var bottomSplitDistance = h - consoleHeight;
        if (bottomSplitDistance >= _bottomSplit.Panel1MinSize && bottomSplitDistance < h - _bottomSplit.SplitterWidth)
            _bottomSplit.SplitterDistance = bottomSplitDistance;

        // Now safe to restore real min sizes (distances already set within valid range)
        _mainSplit.Panel2MinSize = mainPanel2Min;
        _centerRightSplit.Panel2MinSize = PropertyGridMinWidth;
        _bottomSplit.Panel2MinSize = ConsoleMinHeight;
    }

    private void BuildLayout()
    {
        _contentPanel = new Panel { Dock = DockStyle.Fill };

        // Min sizes only; SplitterDistance set in ApplyProportionalLayout when we have valid size.
        // (Setting SplitterDistance/Panel2MinSize in ctor can throw when control has no size yet.)
        _mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = ProjectTreeMinWidth,
            Panel2MinSize = 0,
            FixedPanel = FixedPanel.None
        };
        _mainSplit.Panel1.Controls.Add(_projectView);

        _centerRightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 200,
            Panel2MinSize = 0,
            FixedPanel = FixedPanel.None
        };
        _centerRightSplit.Panel1.Controls.Add(_editorTabControl);
        _centerRightSplit.Panel2.Controls.Add(_propertyGrid);
        _mainSplit.Panel2.Controls.Add(_centerRightSplit);

        _bottomSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            Panel1MinSize = 100,
            Panel2MinSize = 0,
            FixedPanel = FixedPanel.None
        };
        _bottomSplit.Panel1.Controls.Add(_mainSplit);
        var consolePanel = new Panel { Dock = DockStyle.Fill, MinimumSize = new Size(0, ConsoleMinHeight) };
        consolePanel.Controls.Add(_consoleBox);
        _bottomSplit.Panel2.Controls.Add(consolePanel);

        _contentPanel.Controls.Add(_bottomSplit);
        // Add content last so Dock=Fill uses the space between toolbar and status bar
        Controls.Add(_contentPanel);
    }

    private void EnsureTabCloseMenu()
    {
        if (_editorTabControl.ContextMenuStrip != null) return;
        var menu = new ContextMenuStrip();
        var close = new ToolStripMenuItem("Close");
        close.Click += (_, _) =>
        {
            if (_editorTabControl.SelectedTab != null)
            {
                _editorTabControl.TabPages.Remove(_editorTabControl.SelectedTab);
            }
        };
        menu.Items.Add(close);
        _editorTabControl.ContextMenuStrip = menu;
    }

    private void WireProjectEvents()
    {
        _projectManager.ProjectOpened += (_, project) =>
        {
            _projectView.Bind(project);
            _editorTabControl.TabPages.Clear();
            UpdateProjectDependentState();
            SetStatus($"Project: {project?.Name ?? ""}");
        };
        _projectManager.ProjectClosed += (_, _) =>
        {
            _projectView.Bind(null);
            _editorTabControl.TabPages.Clear();
            UpdateProjectDependentState();
            SetStatus("Ready");
        };
        _projectView.NodeActivated += OnProjectNodeActivated;
        _projectView.AddScreenRequested += OnAddScreen;
        _projectView.AddTagTableRequested += OnAddTagTable;
        _projectView.AddScriptRequested += OnAddScript;
        _projectView.DeleteNodeRequested += OnDeleteNode;
    }

    private void OnAddScreen(object? sender, EventArgs e)
    {
        var project = _projectManager.CurrentProject;
        if (project == null) return;
        var name = "Screen" + (project.Screens.Count + 1);
        project.Screens.Add(new ScreenNode { Name = name });
        _projectView.Bind(project);
        _projectManager.Save();
        SetStatus($"Added {name}");
    }

    private void OnAddTagTable(object? sender, EventArgs e)
    {
        var project = _projectManager.CurrentProject;
        if (project == null) return;
        var name = "TagTable" + (project.TagTables.Count + 1);
        project.TagTables.Add(new TagTableNode { Name = name });
        _projectView.Bind(project);
        _projectManager.Save();
        SetStatus($"Added {name}");
    }

    private void OnAddScript(object? sender, EventArgs e)
    {
        var project = _projectManager.CurrentProject;
        if (project == null) return;
        var name = "Script" + (project.Scripts.Count + 1);
        project.Scripts.Add(new ScriptNode { Name = name });
        _projectView.Bind(project);
        _projectManager.Save();
        SetStatus($"Added {name}");
    }

    private void OnDeleteNode(object? sender, ProjectNodeEventArgs e)
    {
        var project = _projectManager.CurrentProject;
        if (project == null) return;
        var node = e.Node;
        if (node is ScreenNode sn)
        {
            project.Screens.Remove(sn);
            CloseTabForNode(node);
        }
        else if (node is TagTableNode tn)
        {
            project.TagTables.Remove(tn);
            CloseTabForNode(node);
        }
        else if (node is ScriptNode srn)
        {
            project.Scripts.Remove(srn);
            CloseTabForNode(node);
        }
        else if (node is CommunicationModuleNode cn)
        {
            project.CommunicationModules.Remove(cn);
            CloseTabForNode(node);
        }
        _projectView.Bind(project);
        _projectManager.Save();
        SetStatus($"Deleted {node.Name}");
    }

    private void CloseTabForNode(ProjectNode node)
    {
        foreach (TabPage page in _editorTabControl.TabPages)
        {
            if (page.Tag == node)
            {
                _editorTabControl.TabPages.Remove(page);
                break;
            }
        }
    }

    private void SetStatus(string text)
    {
        if (toolStripStatusLabel1 != null && !toolStripStatusLabel1.IsDisposed)
            toolStripStatusLabel1.Text = text;
    }

    private void UpdateProjectDependentState()
    {
        var hasProject = _projectManager.CurrentProject != null;
        saveToolStripMenuItem.Enabled = hasProject;
        saveAsToolStripMenuItem.Enabled = hasProject;
        closeToolStripMenuItem.Enabled = hasProject;
        buildToolStripMenuItemBuild.Enabled = hasProject;
        cleanToolStripMenuItem.Enabled = hasProject;
    }

    private void OnProjectNodeActivated(object? sender, ProjectNodeEventArgs e)
    {
        var node = e.Node;
        var existing = FindTab(node);
        if (existing != null)
        {
            _editorTabControl.SelectedTab = existing;
            _propertyGrid.SelectedObject = node;
            SetStatus($"Editing: {node.Name}");
            return;
        }
        var tab = new TabPage(node.Name);
        var editor = CreateEditorFor(node);
        editor.Dock = DockStyle.Fill;
        tab.Controls.Add(editor);
        tab.Tag = node;
        _editorTabControl.TabPages.Add(tab);
        _editorTabControl.SelectedTab = tab;
        _propertyGrid.SelectedObject = node;
        SetStatus($"Editing: {node.Name}");
    }

    private TabPage? FindTab(ProjectNode node)
    {
        foreach (TabPage page in _editorTabControl.TabPages)
            if (page.Tag == node) return page;
        return null;
    }

    private Control CreateEditorFor(ProjectNode node)
    {
        if (node is ScreenNode screenNode)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 120,
                Panel2MinSize = 0
            };
            var palette = new ComponentsView { Dock = DockStyle.Fill, Width = 160 };
            split.Panel1.Controls.Add(palette);
            var editor = new ScreenEditorControl();
            editor.SelectionChanged += (_, comp) => _propertyGrid.SelectedObject = comp != null ? comp : node;
            var template = new ScreenTemplate { Name = screenNode.Name };
            editor.Bind(template);
            split.Panel2.Controls.Add(editor);
            // Set SplitterDistance and Panel2MinSize only when split is wide enough (avoids InvalidOperationException).
            const int panel1Min = 120;
            const int panel2Min = 200;
            void ApplyScreenSplitLayout(object? s, EventArgs _)
            {
                int w = split.Width;
                if (w <= 0) return;
                int minWidthNeeded = panel1Min + split.SplitterWidth + panel2Min;
                if (w < minWidthNeeded)
                    return; // try again on next Resize when wider
                var maxDist = w - split.SplitterWidth - panel2Min;
                var dist = Math.Max(panel1Min, Math.Min(180, maxDist));
                if (dist <= maxDist)
                    split.SplitterDistance = dist;
                split.Panel2MinSize = panel2Min;
                split.Resize -= ApplyScreenSplitLayout;
            }
            split.Resize += ApplyScreenSplitLayout;
            return split;
        }
        if (node is TagTableNode tagNode)
        {
            var editor = new TagEditorControl();
            var model = new TagTableModel { Name = tagNode.Name };
            editor.Bind(model);
            return editor;
        }
        if (node is AlarmsNode)
            return new AlarmsEditorControl();
        if (node is SchedulesNode)
            return new SchedulerEditorControl();
        if (node is HistorianNode)
            return new HistorianEditorControl();
        if (node is ScriptNode scriptNode)
            return new ScriptEditorControl(scriptNode.Name);
        if (node is CommunicationModuleNode commNode)
            return new CommunicationEditorControl(commNode.Name);
        if (node is SecurityNode)
            return new SecurityConfiguratorControl();
        if (node is MachineLearningNode)
            return new MLConfiguratorControl();
        var placeholder = new Label
        {
            Text = $"Editor for {node.GetType().Name}: {node.Name}",
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        return placeholder;
    }

    private void WireMenuAndToolbar()
    {
        newToolStripMenuItem.Click += (_, _) => OnNewProject();
        openToolStripMenuItem.Click += (_, _) => OnOpenProject();
        saveToolStripMenuItem.Click += (_, _) => _projectManager.Save();
        saveAsToolStripMenuItem.Click += (_, _) => OnSaveAsProject();
        closeToolStripMenuItem.Click += (_, _) => _projectManager.Close();
        exitToolStripMenuItem.Click += (_, _) => Close();
        buildToolStripMenuItemBuild.Click += (_, _) => OnBuild();
        cleanToolStripMenuItem.Click += (_, _) => OnClean();
    }

    private void OnNewProject()
    {
        var defaultPath = AccuTrack.Project.ProjectFile.GetDefaultProjectsPath();
        if (!Directory.Exists(defaultPath))
            Directory.CreateDirectory(defaultPath);
        using var dlg = new SaveFileDialog
        {
            Title = "Create New Project",
            Filter = "AccuTrack project (*.isc)|*.isc|All files|*.*",
            DefaultExt = "isc",
            FileName = "NewProject.isc",
            InitialDirectory = defaultPath
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var name = Path.GetFileNameWithoutExtension(dlg.FileName);
        if (string.IsNullOrEmpty(name)) name = "NewProject";
        // Project folder = parent of chosen file / name (e.g. .../Accutrack/MyProject)
        var parentDir = Path.GetDirectoryName(dlg.FileName) ?? defaultPath;
        var projectDir = Path.Combine(parentDir, name);
        if (!_projectManager.New(projectDir, name))
        {
            WriteConsole("Failed to create project.");
            SetStatus("Failed to create project.");
            return;
        }
        WriteConsole($"Project created: {_projectManager.ProjectFilePath}");
        SetStatus($"Project: {name}");
    }

    private void OnOpenProject()
    {
        var defaultPath = AccuTrack.Project.ProjectFile.GetDefaultProjectsPath();
        using var dlg = new OpenFileDialog
        {
            Title = "Open Project",
            Filter = "AccuTrack project (*.isc)|*.isc|All files|*.*",
            InitialDirectory = Directory.Exists(defaultPath) ? defaultPath : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        if (!_projectManager.Open(dlg.FileName))
        {
            WriteConsole("Failed to open project.");
            SetStatus("Failed to open project.");
            return;
        }
        WriteConsole($"Project opened: {_projectManager.ProjectFilePath}");
    }

    private void OnSaveAsProject()
    {
        if (_projectManager.CurrentProject == null) return;
        var defaultPath = AccuTrack.Project.ProjectFile.GetDefaultProjectsPath();
        using var dlg = new SaveFileDialog
        {
            Title = "Save Project As",
            Filter = "AccuTrack project (*.isc)|*.isc|All files|*.*",
            DefaultExt = "isc",
            FileName = _projectManager.CurrentProject.Name + ".isc",
            InitialDirectory = defaultPath
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var name = Path.GetFileNameWithoutExtension(dlg.FileName);
        var parentDir = Path.GetDirectoryName(dlg.FileName) ?? defaultPath;
        var projectDir = Path.Combine(parentDir, name);
        Directory.CreateDirectory(projectDir);
        _projectManager.CurrentProject.ProjectPath = projectDir;
        _projectManager.CurrentProject.Name = name;
        if (!_projectManager.Save())
        {
            WriteConsole("Failed to save project.");
            return;
        }
        WriteConsole($"Project saved to: {_projectManager.ProjectFilePath}");
    }

    private void OnBuild()
    {
        if (_projectManager.CurrentProject == null)
        {
            WriteConsole("No project open.");
            SetStatus("No project open.");
            return;
        }
        SetStatus("Building...");
        WriteConsole("Build started...");
        var ok = _buildService.Build(_projectManager.CurrentProject, WriteConsole);
        WriteConsole(ok ? "Build finished." : "Build failed.");
        SetStatus(ok ? "Build succeeded." : "Build failed.");
    }

    private void OnClean()
    {
        WriteConsole("Clean...");
        SetStatus("Clean.");
    }

    public void WriteConsole(string text)
    {
        if (_consoleBox.IsDisposed) return;
        _consoleBox.AppendText(text + Environment.NewLine);
    }
}
