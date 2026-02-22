using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Designer.Modules.Project;
using Designer.Modules.TagEngine;
using Designer.Modules.ScriptEditor;
using Svg;
// Removed using Designer.Modules.ScreenEditor; to break circular dependency
// ScreenTemplate will be handled via object/dynamic

namespace Designer.Modules.Project;

/// <summary>
/// Project view control displaying project structure in a tree view.
/// </summary>
public partial class ProjectView : UserControl
{
    private TreeView _treeView;
    private ProjectManager? _projectManager;
    private ContextMenuStrip _contextMenu;
    private ImageList _imageList;
    private string _iconPath = "";

    // Events
    public event EventHandler<ScreenOpenEventArgs>? ScreenOpenRequested;
    public event EventHandler<TagTableOpenEventArgs>? TagTableOpenRequested;
    public event EventHandler<AllTagTablesOpenEventArgs>? AllTagTablesOpenRequested;
    public event EventHandler<ScriptOpenEventArgs>? ScriptOpenRequested;
    public event EventHandler<ScreenCreatedEventArgs>? ScreenCreated;
    public event EventHandler<TagTableCreatedEventArgs>? TagTableCreated;
    public event EventHandler<ScriptCreatedEventArgs>? ScriptCreated;
    public event EventHandler<CommunicationModuleOpenEventArgs>? CommunicationModuleOpenRequested;
    public event EventHandler<AlarmsOpenEventArgs>? AlarmsOpenRequested;
    public event EventHandler<SchedulesOpenEventArgs>? SchedulesOpenRequested;
    public event EventHandler<HistorianOpenEventArgs>? HistorianOpenRequested;
    public event EventHandler<SecurityOpenEventArgs>? SecurityOpenRequested;
    public event EventHandler<MachineLearningOpenEventArgs>? MachineLearningOpenRequested;
    public event EventHandler? DeviceNetworkOpenRequested;
    public event EventHandler? SettingsOpenRequested;

    public ProjectView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Initialize icon path and image list
        InitializeIcons();

        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            ShowLines = true,
            ShowRootLines = true,
            ShowPlusMinus = true,
            HideSelection = false,
            ShowNodeToolTips = true,
            ImageList = _imageList
        };

        _treeView.NodeMouseDoubleClick += OnNodeDoubleClick;
        _treeView.AfterSelect += OnNodeSelected;
        _treeView.MouseDown += OnTreeViewMouseDown;
        _treeView.AfterLabelEdit += OnNodeLabelEdit;
        _treeView.ItemDrag += OnTreeViewItemDrag;

        // Initialize context menu
        _contextMenu = new ContextMenuStrip();
        InitializeContextMenus();

        Controls.Add(_treeView);
    }

    /// <summary>
    /// Initializes the icon path and image list for tree nodes.
    /// Icons are loaded from the icons folder in the build directory (e.g. bin/Debug/Designer/icons).
    /// </summary>
    private void InitializeIcons()
    {
        // Icons folder in build output (e.g. bin/Debug/Designer/icons)
        string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Application.StartupPath;
        _iconPath = Path.Combine(exeDir, "icons");

        // Create image list
        _imageList = new ImageList
        {
            ImageSize = new Size(16, 16),
            ColorDepth = ColorDepth.Depth32Bit
        };

        // Load icons for different node types
        LoadNodeIcons();
    }

    /// <summary>
    /// Loads icons for tree node types from the build icons folder.
    /// Uses only the SVG icons provided in Assets/icons (copied to build/icons).
    /// </summary>
    private void LoadNodeIcons()
    {
        // Map node types to icon file names (icons in build folder: bin/Debug/Designer/icons)
        var iconMap = new Dictionary<string, string>
        {
            { "project", "project.svg" },
            { "scada_folder", "scada_project.svg" },
            { "scada_project", "scada_project.svg" },
            { "screens_folder", "screens_folder.svg" },
            { "screen", "screen.svg" },
            { "scripts_folder", "scripts_folder.svg" },
            { "script", "script.svg" },
            { "tags_folder", "tags_folder.svg" },
            { "tag_table", "tag_table.svg" },
            { "comm_modules", "add_new.svg" },
            { "alarms", "alarms.svg" },
            { "schedules", "schedules.svg" },
            { "historian", "historical_data.svg" },
            { "security", "security.svg" },
            { "machine_learning", "add_new.svg" },
            { "device_network", "pc_station.svg" },
            { "settings", "project_settings.svg" },
            { "default", "add_new.svg" }
        };

        // Load each icon
        foreach (var kvp in iconMap)
        {
            string iconFile = Path.Combine(_iconPath, kvp.Value);
            Image? icon = LoadIcon(iconFile);
            if (icon != null)
            {
                _imageList.Images.Add(kvp.Key, icon);
            }
        }

        // If no icons loaded, create a default folder icon
        if (_imageList.Images.Count == 0)
        {
            var defaultIcon = CreateDefaultFolderIcon();
            _imageList.Images.Add("default", defaultIcon);
        }
    }

    /// <summary>
    /// Loads an icon from file (SVG or PNG).
    /// </summary>
    private Image? LoadIcon(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
            return null;

        try
        {
            if (iconPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                return LoadSvgAsImage(iconPath, 16, 16);
            }
            else if (iconPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || iconPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                var img = Image.FromFile(iconPath);
                if (img.Width != 16 || img.Height != 16)
                {
                    var resized = new Bitmap(16, 16);
                    using (var g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(img, 0, 0, 16, 16);
                    }
                    img.Dispose();
                    return resized;
                }
                return img;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Loads an SVG file as an Image.
    /// </summary>
    private Image? LoadSvgAsImage(string svgPath, int width, int height)
    {
        try
        {
            if (!File.Exists(svgPath))
                return null;

            var svgDoc = SvgDocument.Open(svgPath);
            if (svgDoc == null)
                return null;

            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);
                svgDoc.Draw(g);
            }

            return bitmap;
        }
        catch (ArgumentOutOfRangeException)
        {
            // Svg.NET can throw when parsing some SVG content (e.g. startIndex -1 from IndexOf)
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a default folder icon if no icons are available.
    /// </summary>
    private Image CreateDefaultFolderIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 240, 200)), 2, 4, 12, 10);
            g.FillPolygon(new SolidBrush(Color.FromArgb(255, 220, 180)), new Point[]
            {
                new Point(2, 4),
                new Point(6, 4),
                new Point(7, 6),
                new Point(14, 6),
                new Point(14, 14),
                new Point(2, 14)
            });
            g.DrawRectangle(Pens.DarkGray, 2, 4, 12, 10);
            g.DrawLine(Pens.DarkGray, 2, 4, 7, 4);
        }
        return bmp;
    }

    /// <summary>
    /// Gets the icon index for a node type.
    /// </summary>
    private int GetIconIndex(NodeType nodeType)
    {
        string iconKey = nodeType switch
        {
            NodeType.Project => "project",
            NodeType.ScadaFolder => "scada_folder",
            NodeType.ScadaProject => "scada_project",
            NodeType.ScreensFolder => "screens_folder",
            NodeType.Screen => "screen",
            NodeType.ScriptsFolder => "scripts_folder",
            NodeType.Script => "script",
            NodeType.TagsFolder => "tags_folder",
            NodeType.TagTable => "tag_table",
            NodeType.CommunicationModules => "comm_modules",
            NodeType.Alarms => "alarms",
            NodeType.Schedules => "schedules",
            NodeType.Historian => "historian",
            NodeType.Security => "security",
            NodeType.MachineLearning => "machine_learning",
            NodeType.DeviceNetwork => "device_network",
            NodeType.Settings => "settings",
            _ => "default"
        };

        int index = _imageList.Images.IndexOfKey(iconKey);
        return index >= 0 ? index : (_imageList.Images.Count > 0 ? 0 : -1);
    }

    /// <summary>
    /// Sets the project manager for this view.
    /// </summary>
    public void SetProjectManager(ProjectManager manager)
    {
        _projectManager = manager;
        RefreshView();
    }

    /// <summary>
    /// Refreshes the project view.
    /// </summary>
    public void RefreshView()
    {
        // Save expansion state before clearing
        var expandedPaths = SaveExpansionState();

        _treeView.Nodes.Clear();

        if (_projectManager?.GetCurrentProject() == null)
            return;

        var project = _projectManager.GetCurrentProject();

        // Root node
        int projectIconIndex = GetIconIndex(NodeType.Project);
        var rootNode = new TreeNode(project.Name)
        {
            Tag = new ProjectNodeData { Type = NodeType.Project, Data = project },
            ImageIndex = projectIconIndex,
            SelectedImageIndex = projectIconIndex
        };

        // SCADA Projects folder
        int scadaFolderIconIndex = GetIconIndex(NodeType.ScadaFolder);
        var scadaFolderNode = new TreeNode("SCADA Projects")
        {
            Tag = new ProjectNodeData { Type = NodeType.ScadaFolder },
            ImageIndex = scadaFolderIconIndex,
            SelectedImageIndex = scadaFolderIconIndex
        };

        foreach (var scada in project.ScadaProjects)
        {
            var scadaNode = CreateScadaNode(scada);
            scadaFolderNode.Nodes.Add(scadaNode);
        }

        rootNode.Nodes.Add(scadaFolderNode);

        // Device Network
        int deviceNetworkIconIndex = GetIconIndex(NodeType.DeviceNetwork);
        var deviceNetworkNode = new TreeNode("Device Network")
        {
            Tag = new ProjectNodeData { Type = NodeType.DeviceNetwork },
            ImageIndex = deviceNetworkIconIndex,
            SelectedImageIndex = deviceNetworkIconIndex
        };
        rootNode.Nodes.Add(deviceNetworkNode);

        // Project Settings
        int settingsIconIndex = GetIconIndex(NodeType.Settings);
        var settingsNode = new TreeNode("Project Settings")
        {
            Tag = new ProjectNodeData { Type = NodeType.Settings },
            ImageIndex = settingsIconIndex,
            SelectedImageIndex = settingsIconIndex
        };
        rootNode.Nodes.Add(settingsNode);

        _treeView.Nodes.Add(rootNode);
        rootNode.Expand();
        scadaFolderNode.Expand();

        // Restore expansion state after rebuilding
        RestoreExpansionState(expandedPaths);
    }

    private TreeNode CreateScadaNode(ScadaProject scada)
    {
        string version = string.IsNullOrEmpty(scada.Version) ? "1.0.0" : scada.Version;
        string displayText = $"{scada.Name} (v{version})";
        int scadaIconIndex = GetIconIndex(NodeType.ScadaProject);
        var scadaNode = new TreeNode(displayText)
        {
            Tag = new ProjectNodeData { Type = NodeType.ScadaProject, Data = scada },
            ImageIndex = scadaIconIndex,
            SelectedImageIndex = scadaIconIndex
        };
        scadaNode.ToolTipText = $"SCADA Project: {scada.Name}\nVersion: {version}\nType: {scada.Type}";

        // Screens folder
        int screensFolderIconIndex = GetIconIndex(NodeType.ScreensFolder);
        var screensFolder = new TreeNode("Screens")
        {
            Tag = new ProjectNodeData { Type = NodeType.ScreensFolder, ScadaName = scada.Name },
            ImageIndex = screensFolderIconIndex,
            SelectedImageIndex = screensFolderIconIndex
        };
        // Load and add screen items
        if (_projectManager != null)
        {
            var screens = _projectManager.GetScreens(scada.Name);
            foreach (var screen in screens)
            {
                // Use dynamic to access Name property without direct ScreenTemplate reference
                try
                {
                    dynamic screenObj = screen;
                    string screenName = screenObj.Name?.ToString() ?? "Unknown";
                    int screenIconIndex = GetIconIndex(NodeType.Screen);
                    var screenNode = new TreeNode(screenName)
                    {
                        Tag = new ProjectNodeData { Type = NodeType.Screen, Data = screen, ScadaName = scada.Name },
                        ImageIndex = screenIconIndex,
                        SelectedImageIndex = screenIconIndex
                    };
                    screensFolder.Nodes.Add(screenNode);
                }
                catch { /* Skip invalid screens */ }
            }
        }
        scadaNode.Nodes.Add(screensFolder);

        // Scripts folder
        int scriptsFolderIconIndex = GetIconIndex(NodeType.ScriptsFolder);
        var scriptsFolder = new TreeNode("Scripts")
        {
            Tag = new ProjectNodeData { Type = NodeType.ScriptsFolder, ScadaName = scada.Name },
            ImageIndex = scriptsFolderIconIndex,
            SelectedImageIndex = scriptsFolderIconIndex
        };
        // Load and add script items
        if (_projectManager != null)
        {
            var scripts = _projectManager.GetScripts(scada.Name);
            int scriptIconIndex = GetIconIndex(NodeType.Script);
            foreach (var script in scripts.OfType<LuaScript>())
            {
                var scriptNode = new TreeNode(script.Name)
                {
                    Tag = new ProjectNodeData { Type = NodeType.Script, Data = script, ScadaName = scada.Name },
                    ImageIndex = scriptIconIndex,
                    SelectedImageIndex = scriptIconIndex
                };
                scriptsFolder.Nodes.Add(scriptNode);
            }
        }
        scadaNode.Nodes.Add(scriptsFolder);

        // Tags folder
        int tagsFolderIconIndex = GetIconIndex(NodeType.TagsFolder);
        var tagsFolder = new TreeNode("Tags")
        {
            Tag = new ProjectNodeData { Type = NodeType.TagsFolder, ScadaName = scada.Name },
            ImageIndex = tagsFolderIconIndex,
            SelectedImageIndex = tagsFolderIconIndex
        };
        // Load and add tag table items
        if (_projectManager != null)
        {
            var tagTables = _projectManager.GetTagTables(scada.Name);
            int tagTableIconIndex = GetIconIndex(NodeType.TagTable);
            foreach (var table in tagTables.OfType<TagTable>())
            {
                var tableNode = new TreeNode(table.Name)
                {
                    Tag = new ProjectNodeData { Type = NodeType.TagTable, Data = table, ScadaName = scada.Name },
                    ImageIndex = tagTableIconIndex,
                    SelectedImageIndex = tagTableIconIndex
                };
                tagsFolder.Nodes.Add(tableNode);
            }
        }
        scadaNode.Nodes.Add(tagsFolder);

        // Communication Modules
        int commIconIndex = GetIconIndex(NodeType.CommunicationModules);
        var commNode = new TreeNode("Communication Modules")
        {
            Tag = new ProjectNodeData { Type = NodeType.CommunicationModules, ScadaName = scada.Name },
            ImageIndex = commIconIndex,
            SelectedImageIndex = commIconIndex
        };
        scadaNode.Nodes.Add(commNode);

        // Alarms
        int alarmsIconIndex = GetIconIndex(NodeType.Alarms);
        var alarmsNode = new TreeNode("Alarms")
        {
            Tag = new ProjectNodeData { Type = NodeType.Alarms, ScadaName = scada.Name },
            ImageIndex = alarmsIconIndex,
            SelectedImageIndex = alarmsIconIndex
        };
        scadaNode.Nodes.Add(alarmsNode);

        // Schedules
        int schedulesIconIndex = GetIconIndex(NodeType.Schedules);
        var schedulesNode = new TreeNode("Schedules")
        {
            Tag = new ProjectNodeData { Type = NodeType.Schedules, ScadaName = scada.Name },
            ImageIndex = schedulesIconIndex,
            SelectedImageIndex = schedulesIconIndex
        };
        scadaNode.Nodes.Add(schedulesNode);

        // Historian
        int historianIconIndex = GetIconIndex(NodeType.Historian);
        var historianNode = new TreeNode("Historian")
        {
            Tag = new ProjectNodeData { Type = NodeType.Historian, ScadaName = scada.Name },
            ImageIndex = historianIconIndex,
            SelectedImageIndex = historianIconIndex
        };
        scadaNode.Nodes.Add(historianNode);

        // Security
        int securityIconIndex = GetIconIndex(NodeType.Security);
        var securityNode = new TreeNode("Security")
        {
            Tag = new ProjectNodeData { Type = NodeType.Security, ScadaName = scada.Name },
            ImageIndex = securityIconIndex,
            SelectedImageIndex = securityIconIndex
        };
        scadaNode.Nodes.Add(securityNode);

        // Machine Learning
        int mlIconIndex = GetIconIndex(NodeType.MachineLearning);
        var mlNode = new TreeNode("Machine Learning")
        {
            Tag = new ProjectNodeData { Type = NodeType.MachineLearning, ScadaName = scada.Name },
            ImageIndex = mlIconIndex,
            SelectedImageIndex = mlIconIndex
        };
        scadaNode.Nodes.Add(mlNode);

        return scadaNode;
    }

    private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is not ProjectNodeData nodeData)
            return;

        switch (nodeData.Type)
        {
            case NodeType.Screen:
                if (nodeData.Data != null && !string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    ScreenOpenRequested?.Invoke(this, new ScreenOpenEventArgs(nodeData.Data, nodeData.ScadaName));
                }
                break;
            case NodeType.TagTable:
                if (nodeData.Data is TagTable tagTable && !string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    TagTableOpenRequested?.Invoke(this, new TagTableOpenEventArgs(tagTable, nodeData.ScadaName));
                }
                break;
            case NodeType.TagsFolder:
                if (!string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    // Get tag tables from project manager
                    var tagTables = new List<object>();
                    if (_projectManager != null)
                    {
                        tagTables = _projectManager.GetTagTables(nodeData.ScadaName);
                    }
                    AllTagTablesOpenRequested?.Invoke(this, new AllTagTablesOpenEventArgs(nodeData.ScadaName, tagTables));
                }
                break;
            case NodeType.Script:
                if (nodeData.Data is LuaScript script && !string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    ScriptOpenRequested?.Invoke(this, new ScriptOpenEventArgs(script, nodeData.ScadaName));
                }
                break;
            case NodeType.CommunicationModules:
                if (!string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    CommunicationModuleOpenRequested?.Invoke(this, new CommunicationModuleOpenEventArgs(nodeData.ScadaName, null));
                }
                break;
            case NodeType.Alarms:
                if (!string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    var alarmsPayload = _projectManager?.GetAlarms(nodeData.ScadaName);
                    AlarmsOpenRequested?.Invoke(this, new AlarmsOpenEventArgs(nodeData.ScadaName, alarmsPayload));
                }
                break;
            case NodeType.Schedules:
                if (!string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    SchedulesOpenRequested?.Invoke(this, new SchedulesOpenEventArgs(nodeData.ScadaName, null));
                }
                break;
            case NodeType.Historian:
                if (!string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    HistorianOpenRequested?.Invoke(this, new HistorianOpenEventArgs(nodeData.ScadaName, null));
                }
                break;
            case NodeType.Security:
                if (!string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    SecurityOpenRequested?.Invoke(this, new SecurityOpenEventArgs(nodeData.ScadaName, null));
                }
                break;
            case NodeType.MachineLearning:
                if (!string.IsNullOrEmpty(nodeData.ScadaName))
                {
                    MachineLearningOpenRequested?.Invoke(this, new MachineLearningOpenEventArgs(nodeData.ScadaName, null));
                }
                break;
            case NodeType.DeviceNetwork:
                DeviceNetworkOpenRequested?.Invoke(this, EventArgs.Empty);
                break;
            case NodeType.Settings:
                SettingsOpenRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    private void OnNodeSelected(object? sender, TreeViewEventArgs e)
    {
        // Handle selection if needed
    }

    private void OnTreeViewItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is TreeNode node && node.Tag is ProjectNodeData nodeData)
        {
            // Only allow dragging screen nodes
            if (nodeData.Type == NodeType.Screen && nodeData.Data != null)
            {
                try
                {
                    // Get screen name and ID
                    dynamic screenObj = nodeData.Data;
                    string screenName = screenObj.Name?.ToString() ?? node.Text;
                    string screenId = screenObj.Id?.ToString() ?? "";
                    string scadaName = nodeData.ScadaName ?? "";
                    
                    // Create drag data with screen information
                    var dragData = new Dictionary<string, object>
                    {
                        ["screenName"] = screenName,
                        ["screenId"] = screenId,
                        ["scadaName"] = scadaName,
                        ["screenObject"] = nodeData.Data
                    };
                    
                    var dataObject = new DataObject(ScreenDragDropFormat, dragData);
                    _treeView.DoDragDrop(dataObject, DragDropEffects.Copy);
                }
                catch
                {
                    // Ignore drag errors
                }
            }
        }
    }

    public const string ScreenDragDropFormat = "AccuTrack.SCADA.Screen";

    private void OnTreeViewMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            TreeNode? node = _treeView.GetNodeAt(e.X, e.Y);
            if (node != null)
            {
                _treeView.SelectedNode = node;
                ShowContextMenu(node, e.Location);
            }
        }
    }

    private void InitializeContextMenus()
    {
        // Context menu will be built dynamically based on node type
    }

    private void ShowContextMenu(TreeNode node, Point location)
    {
        if (node.Tag is not ProjectNodeData nodeData)
            return;

        _contextMenu.Items.Clear();

        switch (nodeData.Type)
        {
            case NodeType.ScadaFolder:
                AddMenuItem("Add SCADA Project...", () => AddScadaProject());
                break;

            case NodeType.ScadaProject:
                AddMenuItem("Open", () => OnNodeDoubleClick(null, new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, 0, 0)));
                AddMenuItem("Rename", () => RenameNode(node));
                AddMenuItem("Delete", () => DeleteScadaProject(node, nodeData));
                _contextMenu.Items.Add(new ToolStripSeparator());
                AddMenuItem("Properties", () => ShowScadaProjectProperties(nodeData));
                break;

            case NodeType.ScreensFolder:
                AddMenuItem("Add Screen...", () => AddScreen(nodeData.ScadaName ?? string.Empty));
                AddMenuItem("Refresh", () => RefreshView());
                break;

            case NodeType.Screen:
                AddMenuItem("Open", () => OnNodeDoubleClick(null, new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, 0, 0)));
                AddMenuItem("Rename", () => RenameNode(node));
                AddMenuItem("Delete", () => DeleteScreen(node, nodeData));
                break;

            case NodeType.ScriptsFolder:
                AddMenuItem("Add Script...", () => AddScript(nodeData.ScadaName ?? string.Empty));
                AddMenuItem("Refresh", () => RefreshView());
                break;

            case NodeType.Script:
                AddMenuItem("Open", () => OnNodeDoubleClick(null, new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, 0, 0)));
                AddMenuItem("Rename", () => RenameNode(node));
                AddMenuItem("Delete", () => DeleteScript(node, nodeData));
                break;

            case NodeType.TagsFolder:
                AddMenuItem("Add Tag Table...", () => AddTagTable(nodeData.ScadaName ?? string.Empty));
                AddMenuItem("Refresh", () => RefreshView());
                break;

            case NodeType.TagTable:
                AddMenuItem("Open", () => OnNodeDoubleClick(null, new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, 0, 0)));
                AddMenuItem("Rename", () => RenameNode(node));
                AddMenuItem("Delete", () => DeleteTagTable(node, nodeData));
                break;

            case NodeType.CommunicationModules:
            case NodeType.Alarms:
            case NodeType.Schedules:
            case NodeType.Historian:
            case NodeType.Security:
            case NodeType.MachineLearning:
                AddMenuItem("Open", () => OnNodeDoubleClick(null, new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, 0, 0)));
                break;

            case NodeType.DeviceNetwork:
            case NodeType.Settings:
                AddMenuItem("Open", () => OnNodeDoubleClick(null, new TreeNodeMouseClickEventArgs(node, MouseButtons.Left, 1, 0, 0)));
                break;
        }

        if (_contextMenu.Items.Count > 0)
        {
            _contextMenu.Show(_treeView, location);
        }
    }

    private void AddMenuItem(string text, Action action)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (s, e) => action();
        _contextMenu.Items.Add(item);
    }

    private void AddScadaProject()
    {
        if (_projectManager == null)
            return;

        var currentProject = _projectManager.GetCurrentProject();
        if (currentProject == null)
        {
            MessageBox.Show("No project is currently open.", "Add SCADA Project",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new ScadaProjectDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        var scadaName = dialog.ScadaName.Trim();
        if (string.IsNullOrEmpty(scadaName))
        {
            MessageBox.Show("Please enter a SCADA name.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Prevent duplicate SCADA names
        if (_projectManager.FindScadaProject(scadaName) != null)
        {
            MessageBox.Show($"SCADA project '{scadaName}' already exists.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Derive SCADA root path from project file/directory
        var projectPath = currentProject.Path;
        var projectDir = System.IO.Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDir) || !System.IO.Directory.Exists(projectDir))
        {
            // Fallback: if Path is already a directory
            projectDir = System.IO.Directory.Exists(projectPath) ? projectPath : ProjectDirectory.GetProjectPath(currentProject.Name);
        }

        // Pass project directory; ProjectManager will create SCADA root under it
        var added = _projectManager.AddScadaProject(scadaName, projectDir, dialog.ScadaType, dialog.Resolution);
        if (!added)
        {
            MessageBox.Show("Failed to add SCADA project.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Create default screens if selected
        if (dialog.AddAlarmsScreen)
        {
            CreateDefaultScreen(scadaName, "Alarms", "Alarms monitoring screen");
        }
        if (dialog.AddLogsScreen)
        {
            CreateDefaultScreen(scadaName, "Logs", "System logs screen");
        }

        RefreshView();
    }

    private void CreateDefaultScreen(string scadaName, string screenName, string description)
    {
        if (_projectManager == null) return;
        
        var scada = _projectManager.FindScadaProject(scadaName);
        if (scada == null) return;
        
        // Create ScreenTemplate using reflection to avoid circular dependency
        var screenTemplateType = Type.GetType("Designer.Modules.ScreenEditor.ScreenTemplate, ScreenEditor");
        if (screenTemplateType != null)
        {
            var screen = Activator.CreateInstance(screenTemplateType);
            if (screen != null)
            {
                // Set properties via reflection
                screenTemplateType.GetProperty("Name")?.SetValue(screen, screenName);
                screenTemplateType.GetProperty("Description")?.SetValue(screen, description);
                screenTemplateType.GetProperty("Size")?.SetValue(screen, scada.Resolution);
                screenTemplateType.GetProperty("BackgroundColor")?.SetValue(screen, System.Drawing.Color.White);
                
                // Get Components list to add buttons
                var componentsProperty = screenTemplateType.GetProperty("Components");
                var components = componentsProperty?.GetValue(screen) as System.Collections.IList;
                
                if (components != null)
                {
                    // Get ButtonComponent type
                    var buttonComponentType = Type.GetType("Designer.Modules.Components.ButtonComponent, Components");
                    if (buttonComponentType != null)
                    {
                        var resolution = scada.Resolution;
                        int buttonWidth = 120;
                        int buttonHeight = 35;
                        int buttonSpacing = 10;
                        int topMargin = 20;
                        int leftMargin = 20;
                        
                        // Calculate button positions (top-right area)
                        int startX = resolution.Width - buttonWidth - leftMargin;
                        int startY = topMargin;
                        
                        // Back button (always present)
                        var backButton = CreateButton(
                            buttonComponentType,
                            "Back",
                            new System.Drawing.Point(startX, startY),
                            new System.Drawing.Size(buttonWidth, buttonHeight),
                            "PreviousScreen",
                            System.Drawing.Color.FromArgb(70, 130, 180), // Steel blue
                            System.Drawing.Color.White
                        );
                        components.Add(backButton);
                        startY += buttonHeight + buttonSpacing;
                        
                        // Clear button
                        var clearButton = CreateButton(
                            buttonComponentType,
                            "Clear",
                            new System.Drawing.Point(startX, startY),
                            new System.Drawing.Size(buttonWidth, buttonHeight),
                            screenName == "Alarms" ? "ClearData" : "ClearLogs",
                            System.Drawing.Color.FromArgb(220, 53, 69), // Red
                            System.Drawing.Color.White
                        );
                        components.Add(clearButton);
                        startY += buttonHeight + buttonSpacing;
                        
                        // Print button
                        var printButton = CreateButton(
                            buttonComponentType,
                            "Print",
                            new System.Drawing.Point(startX, startY),
                            new System.Drawing.Size(buttonWidth, buttonHeight),
                            "PrintScreen",
                            System.Drawing.Color.FromArgb(40, 167, 69), // Green
                            System.Drawing.Color.White
                        );
                        components.Add(printButton);
                        
                        // Acknowledge button (only for Alarms screen)
                        if (screenName == "Alarms")
                        {
                            startY += buttonHeight + buttonSpacing;
                            var acknowledgeButton = CreateButton(
                                buttonComponentType,
                                "Acknowledge",
                                new System.Drawing.Point(startX, startY),
                                new System.Drawing.Size(buttonWidth, buttonHeight),
                                "AcknowledgeAllAlarms", // Action to acknowledge all alarms (will be handled by runtime)
                                System.Drawing.Color.FromArgb(255, 193, 7), // Amber/Yellow
                                System.Drawing.Color.Black
                            );
                            components.Add(acknowledgeButton);
                        }

                        // Console component (only for Logs screen) - fills main area for log output
                        if (screenName == "Logs")
                        {
                            var consoleComponentType = Type.GetType("Designer.Modules.Components.ConsoleComponent, Components");
                            if (consoleComponentType != null)
                            {
                                int buttonColumnWidth = 150; // space reserved for buttons on the right
                                int consoleX = leftMargin;
                                int consoleY = topMargin;
                                int consoleWidth = resolution.Width - leftMargin - buttonColumnWidth - leftMargin;
                                int consoleHeight = resolution.Height - (2 * topMargin);
                                if (consoleWidth > 0 && consoleHeight > 0)
                                {
                                    var consoleComp = Activator.CreateInstance(consoleComponentType);
                                    if (consoleComp != null)
                                    {
                                        consoleComponentType.GetProperty("Id")?.SetValue(consoleComp, Guid.NewGuid());
                                        consoleComponentType.GetProperty("Name")?.SetValue(consoleComp, "logConsole");
                                        consoleComponentType.GetProperty("Location")?.SetValue(consoleComp, new System.Drawing.Point(consoleX, consoleY));
                                        consoleComponentType.GetProperty("Size")?.SetValue(consoleComp, new System.Drawing.Size(consoleWidth, consoleHeight));
                                        consoleComponentType.GetProperty("Visible")?.SetValue(consoleComp, true);
                                        consoleComponentType.GetProperty("Enabled")?.SetValue(consoleComp, true);
                                        consoleComponentType.GetProperty("ZOrder")?.SetValue(consoleComp, 0);
                                        components.Add(consoleComp);
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Save screen to file
                string screensPath = scada.Paths.ScreensPath;
                if (!System.IO.Directory.Exists(screensPath))
                {
                    System.IO.Directory.CreateDirectory(screensPath);
                }

                string screenFile = System.IO.Path.Combine(screensPath, $"{screenName}.json");
                screenTemplateType.GetProperty("FilePath")?.SetValue(screen, screenFile);

                // Serialize using ToJson method
                var toJsonMethod = screenTemplateType.GetMethod("ToJson");
                if (toJsonMethod != null)
                {
                    var json = toJsonMethod.Invoke(screen, null);
                    if (json != null)
                    {
                        var jsonObj = json as Newtonsoft.Json.Linq.JObject;
                        jsonObj["filePath"] = screenFile;
                        System.IO.File.WriteAllText(screenFile, jsonObj.ToString());
                    }
                }

                // Trigger event to notify screen was created
                ScreenCreated?.Invoke(this, new ScreenCreatedEventArgs(screen, scadaName));
            }
        }
    }

    private object CreateButton(Type buttonComponentType, string text, System.Drawing.Point location, System.Drawing.Size size, string action, System.Drawing.Color backColor, System.Drawing.Color foreColor)
    {
        var button = Activator.CreateInstance(buttonComponentType);
        if (button != null)
        {
            // Set button properties via reflection
            buttonComponentType.GetProperty("Id")?.SetValue(button, Guid.NewGuid());
            buttonComponentType.GetProperty("Name")?.SetValue(button, $"btn{text}");
            buttonComponentType.GetProperty("Location")?.SetValue(button, location);
            buttonComponentType.GetProperty("Size")?.SetValue(button, size);
            buttonComponentType.GetProperty("Text")?.SetValue(button, text);
            buttonComponentType.GetProperty("Action")?.SetValue(button, action);
            buttonComponentType.GetProperty("BackColor")?.SetValue(button, backColor);
            buttonComponentType.GetProperty("ForeColor")?.SetValue(button, foreColor);
            buttonComponentType.GetProperty("BorderColor")?.SetValue(button, System.Drawing.Color.FromArgb(backColor.R / 2, backColor.G / 2, backColor.B / 2));
            buttonComponentType.GetProperty("BorderWidth")?.SetValue(button, 2);
            buttonComponentType.GetProperty("Font")?.SetValue(button, new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold));
            buttonComponentType.GetProperty("Visible")?.SetValue(button, true);
            buttonComponentType.GetProperty("Enabled")?.SetValue(button, true);
        }
        return button ?? throw new InvalidOperationException($"Failed to create button component: {text}");
    }

    private void RenameNode(TreeNode node)
    {
        node.BeginEdit();
    }

    private void DeleteScadaProject(TreeNode node, ProjectNodeData nodeData)
    {
        if (nodeData.Data is ScadaProject scada && _projectManager != null)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete SCADA project '{scada.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var project = _projectManager.GetCurrentProject();
                if (project != null)
                {
                    project.ScadaProjects.Remove(scada);
                    _projectManager.SaveProject();
                    RefreshView();
                }
            }
        }
    }

    private void DeleteScreen(TreeNode node, ProjectNodeData nodeData)
    {
        var result = MessageBox.Show(
            $"Are you sure you want to delete screen '{node.Text}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            // TODO: Implement screen deletion
            RefreshView();
        }
    }

    private void DeleteScript(TreeNode node, ProjectNodeData nodeData)
    {
        var result = MessageBox.Show(
            $"Are you sure you want to delete script '{node.Text}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            // TODO: Implement script deletion
            RefreshView();
        }
    }

    private void DeleteTagTable(TreeNode node, ProjectNodeData nodeData)
    {
        var result = MessageBox.Show(
            $"Are you sure you want to delete tag table '{node.Text}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            // TODO: Implement tag table deletion
            RefreshView();
        }
    }

    private void AddScreen(string scadaName)
    {
        if (_projectManager == null) return;
        using (var dialog = new NewItemDialog("Screen", includeDescription: true))
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var screenName = dialog.ItemName;
                var scada = _projectManager.FindScadaProject(scadaName);
                if (scada == null) return;
                
                // Create ScreenTemplate using reflection to avoid circular dependency
                var screenTemplateType = Type.GetType("Designer.Modules.ScreenEditor.ScreenTemplate, ScreenEditor");
                if (screenTemplateType != null)
                {
                    var screen = Activator.CreateInstance(screenTemplateType);
                    if (screen != null)
                    {
                        // Set properties via reflection
                        screenTemplateType.GetProperty("Name")?.SetValue(screen, screenName);
                        screenTemplateType.GetProperty("Description")?.SetValue(screen, dialog.Description);
                        screenTemplateType.GetProperty("Size")?.SetValue(screen, scada.Resolution);
                        screenTemplateType.GetProperty("BackgroundColor")?.SetValue(screen, System.Drawing.Color.White);
                        
                        // Save screen to file
                        string screensPath = scada.Paths.ScreensPath;
                        if (!System.IO.Directory.Exists(screensPath))
                        {
                            System.IO.Directory.CreateDirectory(screensPath);
                        }

                        string screenFile = System.IO.Path.Combine(screensPath, $"{screenName}.json");
                        screenTemplateType.GetProperty("FilePath")?.SetValue(screen, screenFile);

                        // Serialize using ToJson method
                        var toJsonMethod = screenTemplateType.GetMethod("ToJson");
                        if (toJsonMethod != null)
                        {
                            var json = toJsonMethod.Invoke(screen, null);
                            if (json != null)
                            {
                                var jsonObj = json as Newtonsoft.Json.Linq.JObject;
                                jsonObj["filePath"] = screenFile;
                                System.IO.File.WriteAllText(screenFile, jsonObj.ToString());
                            }
                        }

                        // Trigger event to open the screen
                        ScreenCreated?.Invoke(this, new ScreenCreatedEventArgs(screen, scadaName));
                        RefreshView();
                    }
                }
            }
        }
    }

    private void AddScript(string scadaName)
    {
        if (_projectManager == null) return;

        var scada = _projectManager.FindScadaProject(scadaName);
        if (scada == null)
        {
            MessageBox.Show($"SCADA project '{scadaName}' not found.", "Add Script",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (var dialog = new NewItemDialog("Script", includeDescription: true))
        {
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
                return;

            var name = dialog.ItemName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a script name.", "Add Script",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = _projectManager.GetScripts(scadaName).OfType<LuaScript>()
                .Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing)
            {
                MessageBox.Show($"A script named '{name}' already exists in this SCADA project.",
                    "Add Script", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var script = _projectManager.AddScript(scadaName, name, dialog.Description ?? "");
            if (script == null)
            {
                MessageBox.Show("Failed to create the script.", "Add Script",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ScriptCreated?.Invoke(this, new ScriptCreatedEventArgs(script, scadaName));
            RefreshView();
        }
    }

    private void AddTagTable(string scadaName)
    {
        if (_projectManager == null || string.IsNullOrWhiteSpace(scadaName))
        {
            MessageBox.Show("No SCADA project selected.", "Add Tag Table",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var scada = _projectManager.FindScadaProject(scadaName);
        if (scada == null)
        {
            MessageBox.Show($"SCADA project '{scadaName}' not found.", "Add Tag Table",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var dialog = new NewItemDialog("Tag Table");
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        var tableName = dialog.ItemName.Trim();
        if (string.IsNullOrEmpty(tableName))
            return;

        var tagsDir = scada.Paths.TagsPath;
        try
        {
            if (!System.IO.Directory.Exists(tagsDir))
                System.IO.Directory.CreateDirectory(tagsDir);

            var filePath = System.IO.Path.Combine(tagsDir, tableName + ".json");
            if (System.IO.File.Exists(filePath))
            {
                MessageBox.Show($"Tag table '{tableName}' already exists.", "Add Tag Table",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var table = new TagTable(tableName);
            if (!table.SaveToFile(filePath))
            {
                MessageBox.Show("Failed to create tag table file.", "Add Tag Table",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Notify listeners (MainForm opens the editor)
            TagTableCreated?.Invoke(this, new TagTableCreatedEventArgs(table, scadaName));
            RefreshView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add tag table: {ex.Message}", "Add Tag Table",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowScadaProjectProperties(ProjectNodeData nodeData)
    {
        if (nodeData.Data is ScadaProject scadaProject)
        {
            using (var dialog = new ScadaProjectPropertiesDialog(scadaProject, _projectManager))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Properties are updated in the dialog's OnFormClosing
                    // Save the project
                    if (_projectManager != null)
                    {
                        _projectManager.SaveProject();
                        RefreshView(); // Refresh to show updated name if changed
                    }
                }
            }
        }
        else
        {
            MessageBox.Show("No SCADA project selected.", "Properties", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void OnNodeLabelEdit(object? sender, NodeLabelEditEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Label))
        {
            e.CancelEdit = true;
            return;
        }

        if (e.Node?.Tag is ProjectNodeData nodeData)
        {
            // Handle renaming based on node type
            switch (nodeData.Type)
            {
                case NodeType.ScadaProject:
                    if (nodeData.Data is ScadaProject scada)
                    {
                        // Strip optional " (vX.Y.Z)" suffix so we only store the project name
                        scada.Name = StripVersionSuffixFromDisplayName(e.Label ?? scada.Name);
                        if (_projectManager != null)
                        {
                            _projectManager.SaveProject();
                        }
                    }
                    break;
                // Add other rename handlers as needed
            }
        }
    }

    /// <summary>
    /// Removes " (vX.Y.Z)" suffix from display name so only the project name is stored.
    /// </summary>
    private static string StripVersionSuffixFromDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return displayName;
        var match = Regex.Match(displayName, @"\s*\(v\d+\.\d+\.\d+\)\s*$");
        return match.Success ? displayName[..match.Index].TrimEnd() : displayName.Trim();
    }

    /// <summary>
    /// Saves the expansion state of all nodes in the tree view.
    /// </summary>
    private HashSet<string> SaveExpansionState()
    {
        var expandedPaths = new HashSet<string>();
        foreach (TreeNode node in _treeView.Nodes)
        {
            SaveNodeExpansionState(node, "", expandedPaths);
        }
        return expandedPaths;
    }

    /// <summary>
    /// Recursively saves the expansion state of a node and its children.
    /// Uses node Tag data when available for more reliable identification.
    /// </summary>
    private void SaveNodeExpansionState(TreeNode node, string parentPath, HashSet<string> expandedPaths)
    {
        // Build path using Tag data when available for better identification
        string nodeIdentifier = GetNodeIdentifier(node);
        string currentPath = string.IsNullOrEmpty(parentPath) ? nodeIdentifier : $"{parentPath}/{nodeIdentifier}";
        
        if (node.IsExpanded)
        {
            expandedPaths.Add(currentPath);
        }

        foreach (TreeNode child in node.Nodes)
        {
            SaveNodeExpansionState(child, currentPath, expandedPaths);
        }
    }

    /// <summary>
    /// Gets a unique identifier for a node, using Tag data when available.
    /// </summary>
    private string GetNodeIdentifier(TreeNode node)
    {
        if (node.Tag is ProjectNodeData nodeData)
        {
            // Use type and name/data for unique identification
            if (nodeData.Data != null)
            {
                // Try to get a name from the data object
                try
                {
                    if (nodeData.Data is ScadaProject scada)
                        return $"{nodeData.Type}:{scada.Name}";
                    if (nodeData.Data is TagTable tagTable)
                        return $"{nodeData.Type}:{tagTable.Name}";
                    if (nodeData.Data is LuaScript script)
                        return $"{nodeData.Type}:{script.Name}";
                    
                    // Try dynamic access for ScreenTemplate
                    dynamic? screenObj = nodeData.Data;
                    if (screenObj != null)
                    {
                        string? screenName = screenObj.Name?.ToString();
                        if (!string.IsNullOrEmpty(screenName))
                            return $"{nodeData.Type}:{screenName}";
                    }
                }
                catch { }
            }
            
            // Fall back to type and scada name if available
            if (!string.IsNullOrEmpty(nodeData.ScadaName))
                return $"{nodeData.Type}:{nodeData.ScadaName}:{node.Text}";
            
            return $"{nodeData.Type}:{node.Text}";
        }
        
        // Fall back to node text
        return node.Text;
    }

    /// <summary>
    /// Restores the expansion state of nodes in the tree view.
    /// </summary>
    private void RestoreExpansionState(HashSet<string> expandedPaths)
    {
        foreach (TreeNode node in _treeView.Nodes)
        {
            RestoreNodeExpansionState(node, "", expandedPaths);
        }
    }

    /// <summary>
    /// Recursively restores the expansion state of a node and its children.
    /// </summary>
    private void RestoreNodeExpansionState(TreeNode node, string parentPath, HashSet<string> expandedPaths)
    {
        string nodeIdentifier = GetNodeIdentifier(node);
        string currentPath = string.IsNullOrEmpty(parentPath) ? nodeIdentifier : $"{parentPath}/{nodeIdentifier}";
        
        if (expandedPaths.Contains(currentPath))
        {
            node.Expand();
        }

        foreach (TreeNode child in node.Nodes)
        {
            RestoreNodeExpansionState(child, currentPath, expandedPaths);
        }
    }
}

// Node data structure
internal class ProjectNodeData
{
    public NodeType Type { get; set; }
    public object? Data { get; set; }
    public string? ScadaName { get; set; }
}

internal enum NodeType
{
    Project,
    ScadaFolder,
    ScadaProject,
    ScreensFolder,
    Screen,
    ScriptsFolder,
    Script,
    TagsFolder,
    TagTable,
    CommunicationModules,
    Alarms,
    Schedules,
    Historian,
    Security,
    MachineLearning,
    DeviceNetwork,
    Settings
}

// Event argument classes
public class ScreenOpenEventArgs : EventArgs
{
    public object Screen { get; }
    public string ScadaName { get; }

    public ScreenOpenEventArgs(object screen, string scadaName)
    {
        Screen = screen;
        ScadaName = scadaName;
    }
}

public class TagTableOpenEventArgs : EventArgs
{
    public object TagTable { get; }
    public string ScadaName { get; }

    public TagTableOpenEventArgs(object tagTable, string scadaName)
    {
        TagTable = tagTable;
        ScadaName = scadaName;
    }
}

public class AllTagTablesOpenEventArgs : EventArgs
{
    public string ScadaName { get; }
    public List<object> TagTables { get; }

    public AllTagTablesOpenEventArgs(string scadaName, List<object> tagTables)
    {
        ScadaName = scadaName;
        TagTables = tagTables;
    }
}

public class ScriptOpenEventArgs : EventArgs
{
    public object Script { get; }
    public string ScadaName { get; }

    public ScriptOpenEventArgs(object script, string scadaName)
    {
        Script = script;
        ScadaName = scadaName;
    }
}

public class CommunicationModuleOpenEventArgs : EventArgs
{
    public string ScadaName { get; }
    public object? CommunicationModule { get; }

    public CommunicationModuleOpenEventArgs(string scadaName, object? communicationModule)
    {
        ScadaName = scadaName;
        CommunicationModule = communicationModule;
    }
}

public class AlarmsOpenEventArgs : EventArgs
{
    public string ScadaName { get; }
    public object? Alarms { get; }

    public AlarmsOpenEventArgs(string scadaName, object? alarms)
    {
        ScadaName = scadaName;
        Alarms = alarms;
    }
}

public class SchedulesOpenEventArgs : EventArgs
{
    public string ScadaName { get; }
    public object? Schedules { get; }

    public SchedulesOpenEventArgs(string scadaName, object? schedules)
    {
        ScadaName = scadaName;
        Schedules = schedules;
    }
}

public class HistorianOpenEventArgs : EventArgs
{
    public string ScadaName { get; }
    public object? Historian { get; }

    public HistorianOpenEventArgs(string scadaName, object? historian)
    {
        ScadaName = scadaName;
        Historian = historian;
    }
}

public class SecurityOpenEventArgs : EventArgs
{
    public string ScadaName { get; }
    public object? Security { get; }

    public SecurityOpenEventArgs(string scadaName, object? security)
    {
        ScadaName = scadaName;
        Security = security;
    }
}

public class MachineLearningOpenEventArgs : EventArgs
{
    public string ScadaName { get; }
    public object? MachineLearning { get; }

    public MachineLearningOpenEventArgs(string scadaName, object? machineLearning)
    {
        ScadaName = scadaName;
        MachineLearning = machineLearning;
    }
}

public class ScreenCreatedEventArgs : EventArgs
{
    public object Screen { get; } // Changed to object to break circular dependency
    public string ScadaName { get; }

    public ScreenCreatedEventArgs(object screen, string scadaName)
    {
        Screen = screen;
        ScadaName = scadaName;
    }
}

public class TagTableCreatedEventArgs : EventArgs
{
    public TagTable TagTable { get; }
    public string ScadaName { get; }

    public TagTableCreatedEventArgs(TagTable tagTable, string scadaName)
    {
        TagTable = tagTable;
        ScadaName = scadaName;
    }
}

public class ScriptCreatedEventArgs : EventArgs
{
    public LuaScript Script { get; }
    public string ScadaName { get; }

    public ScriptCreatedEventArgs(LuaScript script, string scadaName)
    {
        Script = script;
        ScadaName = scadaName;
    }
}
