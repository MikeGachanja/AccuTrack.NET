using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Components;
using Svg;

namespace Designer.Modules.Components;

/// <summary>
/// Components view displaying available components in a categorized palette with icons.
/// </summary>
public partial class ComponentsView : UserControl
{
    private TabControl _componentTabs;
    private TextBox _searchBox;
    private ComponentsModule? _componentsModule;
    private string _iconPath = "";
    private string _svgPath = "";

    public ComponentsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(5)
        };

        // Search box
        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Height = 25
        };
        _searchBox.TextChanged += (s, e) => FilterComponents(_searchBox.Text);

        // Component tabs (categories)
        _componentTabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        mainLayout.Controls.Add(_searchBox, 0, 0);
        mainLayout.Controls.Add(_componentTabs, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Sets the components module.
    /// </summary>
    public void SetComponentsModule(ComponentsModule module)
    {
        _componentsModule = module;
        
        // Initialize paths - use executable directory (bin/Debug/Designer/)
        string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? Application.StartupPath;
        _iconPath = Path.Combine(exeDir, "icons");
        if (!Directory.Exists(_iconPath))
        {
            Directory.CreateDirectory(_iconPath);
        }
        
        _svgPath = Path.Combine(exeDir, "svg");
        if (!Directory.Exists(_svgPath))
        {
            Directory.CreateDirectory(_svgPath);
        }
        
        SetupComponentList();
    }

    /// <summary>
    /// Sets up the component list with categories.
    /// </summary>
    private void SetupComponentList()
    {
        _componentTabs.TabPages.Clear();

        if (_componentsModule == null)
            return;

        // Define component categories matching Qt implementation
        // Component keys match the registered names in ComponentsModule
        var categories = new[]
        {
            new ComponentCategory("Basic Shapes", new[]
            {
                ("Line", "Line"),
                ("Rectangle", "Rectangle"),
                ("Triangle", "Triangle")
            }),
            new ComponentCategory("Input Controls", new[]
            {
                ("Button", "Button"),
                ("Checkbox", "Checkbox"),
                ("Radio Button", "RadioButton"),
                ("Text Input", "TextInput"),
                ("Numeric Viewer", "Numeric"),
                ("Slider", "Slider"),
                ("Spinner", "Spinner"),
                ("Toggle Switch", "ToggleSwitch")
            }),
            new ComponentCategory("Display Components", new[]
            {
                ("Text Label", "TextLabel"),
                ("Date Time", "DateTime"),
                ("Image View", "ImageView"),
                ("Indicator", "Indicator"),
                ("Progress Bar", "ProgressBar"),
                ("Tab", "Tab"),
                ("Popup", "Popup")
            }),
            new ComponentCategory("Data Views", new[]
            {
                ("Table", "Table"),
                ("Trend View", "TrendView"),
                ("Alarm View", "AlarmView"),
                ("Gauge View", "GaugeView")
            }),
            new ComponentCategory("Industrial Components", new[]
            {
                ("Motor", "Motor"),
                ("Pump", "Pump"),
                ("Conveyor", "Conveyor"),
                ("Tank", "Tank")
            })
        };

        // Add category tabs
        foreach (var category in categories)
        {
            var tabPage = CreateCategoryTab(category);
            if (tabPage != null)
            {
                _componentTabs.TabPages.Add(tabPage);
            }
        }

        // Add SVG components tab if SVG files exist
        var svgTab = CreateSvgComponentsTab();
        if (svgTab != null)
        {
            _componentTabs.TabPages.Add(svgTab);
        }
    }

    /// <summary>
    /// Creates a tab page for a component category.
    /// </summary>
    private TabPage? CreateCategoryTab(ComponentCategory category)
    {
        var tabPage = new TabPage(category.Name);
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        var gridLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            Padding = new Padding(5)
        };

        int row = 0;
        foreach (var (displayName, componentKey) in category.Components)
        {
            // Check if component exists in module (componentKey matches registered name)
            var availableComponents = _componentsModule?.GetAvailableComponents();
            if (availableComponents != null)
            {
                string? componentName = availableComponents.OfType<string>()
                    .FirstOrDefault(c => c.Equals(componentKey, StringComparison.OrdinalIgnoreCase));
                
                if (componentName == null)
                    continue; // Skip if component not available
                
                var button = CreateComponentButton(componentName, displayName);
                if (button != null)
                {
                    gridLayout.Controls.Add(button, row % 3, row / 3);
                    gridLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
                    row++;
                }
            }
        }

        if (gridLayout.Controls.Count == 0)
            return null; // No components in this category

        scrollPanel.Controls.Add(gridLayout);
        tabPage.Controls.Add(scrollPanel);
        return tabPage;
    }

    /// <summary>
    /// Creates a component button with icon and text.
    /// </summary>
    private Button? CreateComponentButton(string componentName, string displayName)
    {
        var button = new Button
        {
            Size = new Size(80, 80),
            Text = displayName,
            TextAlign = ContentAlignment.BottomCenter,
            UseVisualStyleBackColor = true,
            FlatStyle = FlatStyle.Flat,
            Tag = componentName
        };

        // Try to load icon
        Image? icon = LoadComponentIcon(componentName);
        if (icon != null)
        {
            button.Image = icon;
            button.ImageAlign = ContentAlignment.TopCenter;
            button.TextImageRelation = TextImageRelation.ImageAboveText;
        }

        // Set up drag and drop
        button.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left && button.Tag is string name && _componentsModule != null)
            {
                var component = _componentsModule.CreateComponent(name);
                if (component != null)
                {
                    var data = new DataObject(ComponentDragDropFormat, component);
                    button.DoDragDrop(data, DragDropEffects.Copy);
                }
            }
        };

        return button;
    }

    /// <summary>
    /// Loads a component icon from file.
    /// </summary>
    private Image? LoadComponentIcon(string componentName)
    {
        // Map component name to icon file name (matching Qt naming convention)
        string iconBaseName = componentName.ToLower() + "Component";
        
        // Try SVG file first
        string svgFile = Path.Combine(_iconPath, $"{iconBaseName}.svg");
        if (File.Exists(svgFile))
        {
            return LoadSvgAsImage(svgFile, 48, 48);
        }

        // Try PNG file
        string pngFile = Path.Combine(_iconPath, $"{iconBaseName}.png");
        if (File.Exists(pngFile))
        {
            try
            {
                var img = Image.FromFile(pngFile);
                // Resize to 48x48 if needed
                if (img.Width != 48 || img.Height != 48)
                {
                    var resized = new Bitmap(48, 48);
                    using (var g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(img, 0, 0, 48, 48);
                    }
                    img.Dispose();
                    return resized;
                }
                return img;
            }
            catch
            {
                return null;
            }
        }

        // Try default icon
        string defaultIcon = Path.Combine(_iconPath, "svg_default.svg");
        if (File.Exists(defaultIcon))
        {
            return LoadSvgAsImage(defaultIcon, 48, 48);
        }

        return null;
    }

    /// <summary>
    /// Loads an SVG file as an Image using Svg.NET library.
    /// </summary>
    private Image? LoadSvgAsImage(string svgPath, int width, int height)
    {
        try
        {
            if (!File.Exists(svgPath))
                return null;

            // Load SVG document using Svg.NET
            var svgDoc = SvgDocument.Open(svgPath);
            if (svgDoc == null)
                return null;

            // Get original SVG bounds
            var originalBounds = svgDoc.Bounds;
            float originalWidth = originalBounds.Width > 0 ? originalBounds.Width : (svgDoc.Width.Value > 0 ? svgDoc.Width.Value : width);
            float originalHeight = originalBounds.Height > 0 ? originalBounds.Height : (svgDoc.Height.Value > 0 ? svgDoc.Height.Value : height);

            // If original dimensions are invalid, try to calculate from viewBox
            if (originalWidth <= 0 || originalHeight <= 0)
            {
                if (svgDoc.ViewBox.Width > 0 && svgDoc.ViewBox.Height > 0)
                {
                    originalWidth = svgDoc.ViewBox.Width;
                    originalHeight = svgDoc.ViewBox.Height;
                }
                else
                {
                    // Fallback: use target size
                    originalWidth = width;
                    originalHeight = height;
                }
            }

            // Set SVG document dimensions to fill the icon space
            svgDoc.Width = new SvgUnit(SvgUnitType.Pixel, width);
            svgDoc.Height = new SvgUnit(SvgUnitType.Pixel, height);

            // Set ViewBox to original SVG bounds to ensure proper scaling
            if (originalWidth > 0 && originalHeight > 0)
            {
                svgDoc.ViewBox = new SvgViewBox(0, 0, originalWidth, originalHeight);
            }

            // Set aspect ratio to none to allow stretching (fill mode)
            svgDoc.AspectRatio = new SvgAspectRatio(SvgPreserveAspectRatio.none);

            // Create bitmap to render into
            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                // Use high-quality rendering
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                // Clear background to white (or transparent)
                g.Clear(Color.Transparent);

                // Render SVG to fill the bitmap
                svgDoc.Draw(g);
            }

            return bitmap;
        }
        catch (Exception ex)
        {
            // Log error for debugging
            System.Diagnostics.Debug.WriteLine($"Failed to load SVG icon '{svgPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates the SVG components tab with hierarchical folder structure.
    /// </summary>
    private TabPage? CreateSvgComponentsTab()
    {
        if (!Directory.Exists(_svgPath))
            return null;

        var svgFiles = Directory.GetFiles(_svgPath, "*.svg", SearchOption.AllDirectories);
        if (svgFiles.Length == 0)
            return null;

        var tabPage = new TabPage("SVG Components");
        // Use larger image size for better SVG preview (24x24)
        var imageList = new ImageList { ImageSize = new Size(24, 24) };
        var treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            ImageList = imageList,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true
        };

        // Add folder icon to ImageList
        var folderBmp = new Bitmap(24, 24);
        using (var g = Graphics.FromImage(folderBmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Draw folder icon (simplified)
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 240, 200)), 3, 6, 18, 15);
            g.FillPolygon(new SolidBrush(Color.FromArgb(255, 220, 180)), new Point[]
            {
                new Point(3, 6),
                new Point(9, 6),
                new Point(10, 9),
                new Point(21, 9),
                new Point(21, 21),
                new Point(3, 21)
            });
            g.DrawRectangle(Pens.DarkGray, 3, 6, 18, 15);
            g.DrawLine(Pens.DarkGray, 3, 6, 10, 6);
        }
        imageList.Images.Add("folder", folderBmp);
        
        // Add default file icon (fallback for SVGs that fail to load)
        var fileBmp = new Bitmap(24, 24);
        using (var g = Graphics.FromImage(fileBmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Draw file icon (simplified document)
            g.FillRectangle(Brushes.White, 4, 3, 16, 18);
            g.DrawRectangle(Pens.Black, 4, 3, 16, 18);
            // Draw corner fold
            g.DrawLine(Pens.Black, 15, 3, 15, 8);
            g.DrawLine(Pens.Black, 15, 8, 20, 8);
        }
        imageList.Images.Add("file", fileBmp);

        // Build hierarchical tree structure
        var rootNode = new TreeNode("SVG Files")
        {
            ImageIndex = treeView.ImageList.Images.IndexOfKey("folder"),
            SelectedImageIndex = treeView.ImageList.Images.IndexOfKey("folder")
        };

        // Organize files by folder structure
        var folderMap = new Dictionary<string, TreeNode>();
        
        foreach (var svgFile in svgFiles)
        {
            string relativePath = Path.GetRelativePath(_svgPath, svgFile);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
            
            TreeNode currentNode = rootNode;
            string currentPath = "";
            
            // Process all parts except the last one (which is the filename)
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string part = parts[i];
                if (string.IsNullOrEmpty(part))
                    continue;
                    
                if (!string.IsNullOrEmpty(currentPath))
                    currentPath += Path.DirectorySeparatorChar;
                currentPath += part;
                
                // Find or create folder node
                TreeNode? folderNode = null;
                if (folderMap.TryGetValue(currentPath, out var existingNode))
                {
                    folderNode = existingNode;
                }
                else
                {
                    folderNode = currentNode.Nodes.Cast<TreeNode>()
                        .FirstOrDefault(n => n.Text == part && n.Tag == null); // Tag is null for folders
                    
                    if (folderNode == null)
                    {
                        folderNode = new TreeNode(part)
                        {
                            ImageIndex = treeView.ImageList.Images.IndexOfKey("folder"),
                            SelectedImageIndex = treeView.ImageList.Images.IndexOfKey("folder"),
                            Tag = null // null tag indicates folder
                        };
                        currentNode.Nodes.Add(folderNode);
                        folderMap[currentPath] = folderNode;
                    }
                }
                
                currentNode = folderNode;
            }
            
            // Add the file node with rendered SVG icon
            string fileName = parts[parts.Length - 1];
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            
            // Try to load and render the SVG as an icon
            Image? svgIcon = LoadSvgAsImage(svgFile, 24, 24);
            int iconIndex;
            
            if (svgIcon != null)
            {
                // Add rendered SVG icon to ImageList
                string iconKey = $"svg_{relativePath.Replace(Path.DirectorySeparatorChar, '_').Replace(" ", "_")}";
                if (!imageList.Images.ContainsKey(iconKey))
                {
                    imageList.Images.Add(iconKey, svgIcon);
                }
                iconIndex = imageList.Images.IndexOfKey(iconKey);
            }
            else
            {
                // Fallback to default file icon if SVG fails to load
                iconIndex = imageList.Images.IndexOfKey("file");
            }
            
            var fileNode = new TreeNode(fileNameWithoutExt)
            {
                ImageIndex = iconIndex,
                SelectedImageIndex = iconIndex,
                Tag = relativePath // Store relative path for drag-drop
            };
            currentNode.Nodes.Add(fileNode);
        }

        treeView.Nodes.Add(rootNode);
        rootNode.ExpandAll();

        // Enable drag for the tree view
        treeView.AllowDrop = false; // TreeView itself doesn't accept drops
        
        // Handle mouse down to start drag
        treeView.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                TreeNode? node = treeView.GetNodeAt(e.X, e.Y);
                if (node != null && node.Tag is string svgPath)
                {
                    // Only drag files (folders have null Tag)
                    var data = new DataObject("AccuTrack.SCADA.SVG", svgPath);
                    treeView.DoDragDrop(data, DragDropEffects.Copy);
                }
            }
        };

        // Handle double-click to drag
        treeView.NodeMouseDoubleClick += (s, e) =>
        {
            if (e.Node.Tag is string svgPath)
            {
                var data = new DataObject("AccuTrack.SCADA.SVG", svgPath);
                treeView.DoDragDrop(data, DragDropEffects.Copy);
            }
        };

        tabPage.Controls.Add(treeView);
        return tabPage;
    }

    /// <summary>
    /// Filters components by search text.
    /// </summary>
    private void FilterComponents(string searchText)
    {
        foreach (TabPage tab in _componentTabs.TabPages)
        {
            foreach (Control control in tab.Controls)
            {
                if (control is Panel panel)
                {
                    foreach (Control ctrl in panel.Controls)
                    {
                        if (ctrl is TableLayoutPanel grid)
                        {
                            foreach (Control btn in grid.Controls)
                            {
                                if (btn is Button button)
                                {
                                    bool visible = string.IsNullOrEmpty(searchText) ||
                                                  button.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                                  (button.Tag is string tag && tag.Contains(searchText, StringComparison.OrdinalIgnoreCase));
                                    button.Visible = visible;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Data format name for drag-drop.
    /// </summary>
    public const string ComponentDragDropFormat = "AccuTrack.SCADA.Component";

    /// <summary>
    /// Component category definition.
    /// </summary>
    private class ComponentCategory
    {
        public string Name { get; }
        public (string DisplayName, string ComponentKey)[] Components { get; }

        public ComponentCategory(string name, (string, string)[] components)
        {
            Name = name;
            Components = components;
        }
    }
}
