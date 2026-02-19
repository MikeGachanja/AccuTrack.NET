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
    private TreeView _componentTree;
    private TextBox _searchBox;
    private ComponentsModule? _componentsModule;
    private string _iconPath = "";
    private string _svgPath = "";
    private ImageList _imageList;

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

        // Component tree view
        _imageList = new ImageList
        {
            ImageSize = new Size(16, 16),
            ColorDepth = ColorDepth.Depth32Bit
        };
        
        _componentTree = new TreeView
        {
            Dock = DockStyle.Fill,
            ImageList = _imageList,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            HideSelection = false
        };

        mainLayout.Controls.Add(_searchBox, 0, 0);
        mainLayout.Controls.Add(_componentTree, 0, 1);
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
    /// Sets up the component list with categories in tree structure.
    /// </summary>
    private void SetupComponentList()
    {
        _componentTree.Nodes.Clear();
        _imageList.Images.Clear();

        if (_componentsModule == null)
            return;

        // Create folder icon for categories
        var folderIcon = CreateFolderIcon();
        _imageList.Images.Add("folder", folderIcon);
        int folderIconIndex = _imageList.Images.IndexOfKey("folder");

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
                ("Console", "Console"),
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

        // Add category nodes with component children
        foreach (var category in categories)
        {
            var categoryNode = CreateCategoryNode(category, folderIconIndex);
            if (categoryNode != null && categoryNode.Nodes.Count > 0)
            {
                _componentTree.Nodes.Add(categoryNode);
            }
        }

        // Add Miscellaneous (SVG components) node if SVG files exist
        var miscellaneousNode = CreateMiscellaneousNode(folderIconIndex);
        if (miscellaneousNode != null && miscellaneousNode.Nodes.Count > 0)
        {
            _componentTree.Nodes.Add(miscellaneousNode);
        }

        // Expand all categories by default
        _componentTree.ExpandAll();
        
        // Set up drag-and-drop handlers
        SetupTreeDragDrop();
    }

    /// <summary>
    /// Creates a folder icon for category nodes.
    /// </summary>
    private Image CreateFolderIcon()
    {
        var bmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Draw folder icon
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
    /// Creates a tree node for a component category with component children.
    /// </summary>
    private TreeNode? CreateCategoryNode(ComponentCategory category, int folderIconIndex)
    {
        var categoryNode = new TreeNode(category.Name)
        {
            ImageIndex = folderIconIndex,
            SelectedImageIndex = folderIconIndex,
            Tag = null // null tag indicates category folder
        };

        var availableComponents = _componentsModule?.GetAvailableComponents();
        if (availableComponents == null)
            return null;

        foreach (var (displayName, componentKey) in category.Components)
        {
            // Check if component exists in module (componentKey matches registered name)
            string? componentName = availableComponents.OfType<string>()
                .FirstOrDefault(c => c.Equals(componentKey, StringComparison.OrdinalIgnoreCase));
            
            if (componentName == null)
                continue; // Skip if component not available
            
            // Load component icon
            Image? icon = LoadComponentIcon(componentName);
            int iconIndex = folderIconIndex; // Default to folder icon
            
            if (icon != null)
            {
                string iconKey = $"component_{componentName}";
                if (!_imageList.Images.ContainsKey(iconKey))
                {
                    // Resize icon to 16x16 for tree view
                    var resizedIcon = new Bitmap(16, 16);
                    using (var g = Graphics.FromImage(resizedIcon))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(icon, 0, 0, 16, 16);
                    }
                    _imageList.Images.Add(iconKey, resizedIcon);
                    icon.Dispose();
                }
                iconIndex = _imageList.Images.IndexOfKey(iconKey);
            }
            
            var componentNode = new TreeNode(displayName)
            {
                ImageIndex = iconIndex,
                SelectedImageIndex = iconIndex,
                Tag = componentName // Store component name for drag-drop
            };
            
            categoryNode.Nodes.Add(componentNode);
        }

        return categoryNode.Nodes.Count > 0 ? categoryNode : null;
    }

    /// <summary>
    /// Sets up drag-and-drop handlers for the component tree.
    /// </summary>
    private void SetupTreeDragDrop()
    {
        _componentTree.MouseDown += (s, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                TreeNode? node = _componentTree.GetNodeAt(e.X, e.Y);
                if (node != null && node.Tag is string tagValue)
                {
                    // Check if it's an SVG component (relative path) or regular component (component name)
                    if (tagValue.Contains(Path.DirectorySeparatorChar) || tagValue.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    {
                        // SVG component - use SVG drag format
                        string fullPath = Path.Combine(_svgPath, tagValue);
                        var data = new DataObject("AccuTrack.SCADA.SVG", fullPath);
                        _componentTree.DoDragDrop(data, DragDropEffects.Copy);
                    }
                    else if (_componentsModule != null)
                    {
                        // Regular component - use component drag format
                        var component = _componentsModule.CreateComponent(tagValue);
                        if (component != null)
                        {
                            var data = new DataObject(ComponentDragDropFormat, component);
                            _componentTree.DoDragDrop(data, DragDropEffects.Copy);
                        }
                    }
                }
            }
        };

        _componentTree.NodeMouseDoubleClick += (s, e) =>
        {
            if (e.Node.Tag is string tagValue)
            {
                // Check if it's an SVG component or regular component
                if (tagValue.Contains(Path.DirectorySeparatorChar) || tagValue.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    // SVG component
                    string fullPath = Path.Combine(_svgPath, tagValue);
                    var data = new DataObject("AccuTrack.SCADA.SVG", fullPath);
                    _componentTree.DoDragDrop(data, DragDropEffects.Copy);
                }
                else if (_componentsModule != null)
                {
                    // Regular component
                    var component = _componentsModule.CreateComponent(tagValue);
                    if (component != null)
                    {
                        var data = new DataObject(ComponentDragDropFormat, component);
                        _componentTree.DoDragDrop(data, DragDropEffects.Copy);
                    }
                }
            }
        };
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
        catch (ArgumentOutOfRangeException)
        {
            // Svg.NET can throw when parsing some SVG content (e.g. startIndex -1 from IndexOf)
            return null;
        }
        catch (ArgumentException)
        {
            // Svg.NET parsing can throw for malformed or unsupported SVG values
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load SVG icon '{svgPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates the Miscellaneous (SVG components) tree node with hierarchical folder structure.
    /// </summary>
    private TreeNode? CreateMiscellaneousNode(int folderIconIndex)
    {
        if (!Directory.Exists(_svgPath))
            return null;

        var svgFiles = Directory.GetFiles(_svgPath, "*.svg", SearchOption.AllDirectories);
        if (svgFiles.Length == 0)
            return null;

        // Add default file icon (fallback for SVGs that fail to load)
        if (!_imageList.Images.ContainsKey("file"))
        {
            var fileBmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(fileBmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Draw file icon (simplified document)
                g.FillRectangle(Brushes.White, 2, 2, 12, 12);
                g.DrawRectangle(Pens.Black, 2, 2, 12, 12);
                // Draw corner fold
                g.DrawLine(Pens.Black, 11, 2, 11, 6);
                g.DrawLine(Pens.Black, 11, 6, 14, 6);
            }
            _imageList.Images.Add("file", fileBmp);
        }
        int fileIconIndex = _imageList.Images.IndexOfKey("file");

        // Build hierarchical tree structure
        var rootNode = new TreeNode("Miscellaneous")
        {
            ImageIndex = folderIconIndex,
            SelectedImageIndex = folderIconIndex
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
                            ImageIndex = folderIconIndex,
                            SelectedImageIndex = folderIconIndex,
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
            Image? svgIcon = LoadSvgAsImage(svgFile, 16, 16);
            int iconIndex = fileIconIndex;
            
            if (svgIcon != null)
            {
                // Add rendered SVG icon to ImageList
                string iconKey = $"svg_{relativePath.Replace(Path.DirectorySeparatorChar, '_').Replace(" ", "_")}";
                if (!_imageList.Images.ContainsKey(iconKey))
                {
                    _imageList.Images.Add(iconKey, svgIcon);
                }
                iconIndex = _imageList.Images.IndexOfKey(iconKey);
            }
            
            var fileNode = new TreeNode(fileNameWithoutExt)
            {
                ImageIndex = iconIndex,
                SelectedImageIndex = iconIndex,
                Tag = relativePath // Store relative path for drag-drop
            };
            currentNode.Nodes.Add(fileNode);
        }

        return rootNode.Nodes.Count > 0 ? rootNode : null;
    }

    /// <summary>
    /// Filters components by search text in the tree view.
    /// Expands categories that contain matches and collapses those that don't.
    /// (WinForms TreeNode does not support hiding individual nodes.)
    /// </summary>
    private void FilterComponents(string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            _componentTree.ExpandAll();
            return;
        }

        foreach (TreeNode categoryNode in _componentTree.Nodes)
        {
            bool hasMatch = categoryNode.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase);

            if (!hasMatch)
            {
                foreach (TreeNode componentNode in categoryNode.Nodes)
                {
                    if (componentNode.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        (componentNode.Tag is string tag && tag.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                    {
                        hasMatch = true;
                        break;
                    }
                }
            }

            if (hasMatch)
                categoryNode.Expand();
            else
                categoryNode.Collapse();
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
