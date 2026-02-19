using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Designer.Modules.Components;
using Designer.Modules.Events;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.ScreenEditor;

/// <summary>
/// Editor for screen templates with visual design capabilities.
/// </summary>
public partial class ScreenEditor : UserControl
{
    private Panel _canvas;
    private Panel _canvasContainer; // Container with margins for centering
    private object? _template; // Changed to object to avoid circular dependency
    private string? _scadaProjectName; // Store name instead of object to avoid circular dependency
    private object? _scadaProject; // Store ScadaProject object for event creation
    private Designer.Modules.Events.EventsModule? _eventsModule; // Events module for creating events
    private Size _scadaResolution = new Size(1920, 1080); // Default SCADA resolution
    private bool _modified;
    private float _zoomFactor = 1.0f;
    
    // Component management fields
    private List<BaseComponent> _components = new List<BaseComponent>();
    private BaseComponent? _selectedComponent;
    private List<BaseComponent> _selectedComponents = new List<BaseComponent>(); // Multi-select support
    private Point _dragStartPoint;
    private Point _dragStartComponentLocation; // Original component location when drag starts
    private Size _dragStartComponentSize; // Original component size when resize starts
    private bool _isDragging;
    private bool _isResizing;
    private Rectangle? _selectionRect;
    
    // Clipboard for copy/paste
    private List<BaseComponent>? _clipboardComponents;
    
    private const int ResizeHandleSize = 8;
    
    // Resize handle positions
    private enum ResizeHandle
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }
    
    private ResizeHandle _activeResizeHandle = ResizeHandle.None;
    private const int MarginSize = 20; // Margin around the canvas

    public event EventHandler<bool>? ModifiedChanged;
    public event EventHandler<BaseComponent?>? SelectionChanged;
    public event EventHandler<BaseComponent?>? ComponentDoubleClicked;

    public ScreenEditor()
    {
        InitializeComponent();
    }

    public bool IsModified => _modified;

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0)
        };

        // Toolbar
        var toolbar = new ToolStrip
        {
            Dock = DockStyle.Top
        };

        var zoomInButton = new ToolStripButton("Zoom In");
        zoomInButton.Click += (s, e) => ZoomIn();
        toolbar.Items.Add(zoomInButton);

        var zoomOutButton = new ToolStripButton("Zoom Out");
        zoomOutButton.Click += (s, e) => ZoomOut();
        toolbar.Items.Add(zoomOutButton);

        var resetZoomButton = new ToolStripButton("Reset Zoom");
        resetZoomButton.Click += (s, e) => ResetZoom();
        toolbar.Items.Add(resetZoomButton);

        toolbar.Items.Add(new ToolStripSeparator());

        var showGridButton = new ToolStripButton("Show Grid") { CheckOnClick = true, Checked = true };
        showGridButton.CheckedChanged += (s, e) =>
        {
            ShowGridEnabled = showGridButton.Checked;
        };
        toolbar.Items.Add(showGridButton);

        var snapToGridButton = new ToolStripButton("Snap to Grid") { CheckOnClick = true, Checked = true };
        snapToGridButton.CheckedChanged += (s, e) =>
        {
            SnapToGridEnabled = snapToGridButton.Checked;
        };
        toolbar.Items.Add(snapToGridButton);

        toolbar.Items.Add(new ToolStripSeparator());

        // Alignment tools
        var alignLeftButton = new ToolStripButton("Align Left");
        alignLeftButton.Click += (s, e) => AlignSelectedComponents(Alignment.Left);
        toolbar.Items.Add(alignLeftButton);

        var alignRightButton = new ToolStripButton("Align Right");
        alignRightButton.Click += (s, e) => AlignSelectedComponents(Alignment.Right);
        toolbar.Items.Add(alignRightButton);

        var alignTopButton = new ToolStripButton("Align Top");
        alignTopButton.Click += (s, e) => AlignSelectedComponents(Alignment.Top);
        toolbar.Items.Add(alignTopButton);

        var alignBottomButton = new ToolStripButton("Align Bottom");
        alignBottomButton.Click += (s, e) => AlignSelectedComponents(Alignment.Bottom);
        toolbar.Items.Add(alignBottomButton);

        var alignCenterHButton = new ToolStripButton("Center H");
        alignCenterHButton.Click += (s, e) => AlignSelectedComponents(Alignment.CenterHorizontal);
        toolbar.Items.Add(alignCenterHButton);

        var alignCenterVButton = new ToolStripButton("Center V");
        alignCenterVButton.Click += (s, e) => AlignSelectedComponents(Alignment.CenterVertical);
        toolbar.Items.Add(alignCenterVButton);

        // Container panel with margins for centering the canvas
        _canvasContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.LightGray, // Background color outside the screen bounds
            AutoScroll = true,
            Padding = new Padding(MarginSize)
        };

        // Canvas for screen design - use double-buffered panel to reduce flicker
        _canvas = new DoubleBufferedPanel
        {
            BackColor = Color.White,
            Size = _scadaResolution,
            Location = new Point(MarginSize, MarginSize)
        };

        _canvas.Paint += OnCanvasPaint;
        _canvas.MouseClick += OnCanvasMouseClick;
        _canvas.MouseDoubleClick += OnCanvasMouseDoubleClick;
        _canvas.MouseMove += OnCanvasMouseMove;
        _canvas.MouseDown += OnCanvasMouseDown;
        _canvas.MouseUp += OnCanvasMouseUp;
        _canvas.AllowDrop = true;
        _canvas.DragEnter += OnCanvasDragEnter;
        _canvas.DragDrop += OnCanvasDragDrop;
        _canvas.ContextMenuStrip = CreateCanvasContextMenu();

        _canvasContainer.Paint += OnContainerPaint;
        _canvasContainer.Controls.Add(_canvas);
        _canvasContainer.Scroll += (s, e) => _canvas.Invalidate(); // Invalidate canvas when scrolling
        _canvasContainer.Resize += (s, e) =>
        {
            // Update canvas position when container is resized (zoom stays at 100%)
            UpdateCanvasPosition();
            UpdateCanvasSize();
        };
        
        // Initialize canvas size when container handle is created (zoom stays at 100%)
        _canvasContainer.HandleCreated += (s, e) =>
        {
            if (_scadaResolution.Width > 0 && _scadaResolution.Height > 0)
            {
                BeginInvoke(new Action(() =>
                {
                    UpdateCanvasSize();
                }));
            }
        };

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_canvasContainer, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Sets the SCADA project name for this editor.
    /// </summary>
    public void SetScadaProjectName(string scadaName)
    {
        _scadaProjectName = scadaName;
    }

    /// <summary>
    /// Sets the SCADA project object for event creation.
    /// </summary>
    public void SetScadaProject(object? scadaProject)
    {
        _scadaProject = scadaProject;
        
        // Initialize events module if not already initialized
        if (_eventsModule == null && scadaProject != null)
        {
            _eventsModule = new Designer.Modules.Events.EventsModule();
            _eventsModule.SetScadaProject(scadaProject);
            
            // Ensure events.json path is set correctly
            var eventsPath = _eventsModule.GetEventsJsonPath();
            if (string.IsNullOrEmpty(eventsPath))
            {
                try
                {
                    dynamic projectObj = scadaProject;
                    var paths = projectObj.Paths;
                    if (paths != null)
                    {
                        var rootPath = paths.RootPath?.ToString();
                        if (!string.IsNullOrEmpty(rootPath))
                        {
                            _eventsModule.SetEventsJsonPath(System.IO.Path.Combine(rootPath, "json", "events.json"));
                        }
                    }
                }
                catch
                {
                    // Fallback handled by EventsModule
                }
            }
        }
    }

    /// <summary>
    /// Sets the SCADA resolution (screen size) for this editor.
    /// </summary>
    public void SetScadaResolution(Size resolution)
    {
        _scadaResolution = resolution;
        // Reset zoom to 100% when resolution changes
        _zoomFactor = 1.0f;
        
        UpdateCanvasSize();
    }

    /// <summary>
    /// Sets the screen template to edit.
    /// </summary>
    public void SetTemplate(object template)
    {
        if (template == null)
            return;
            
        _template = template;
        
        // Use dynamic/reflection to access properties
        try
        {
            dynamic templateObj = template;
            var size = templateObj.Size;
            var backgroundColor = templateObj.BackgroundColor;
            var components = templateObj.Components;
            
            if (size != null)
            {
                // Use SCADA resolution if available, otherwise use template size
                if (_scadaResolution.Width > 0 && _scadaResolution.Height > 0)
                {
                    _canvas.Size = _scadaResolution;
                }
                else
                {
                    _canvas.Size = new Size(size.Width, size.Height);
                }
                UpdateCanvasSize(); // This will update position and scroll area
            }
            if (backgroundColor != null)
            {
                _canvas.BackColor = backgroundColor;
            }
            
            // Load components from template
            _components.Clear();
            if (components != null)
            {
                foreach (var comp in components)
                {
                    if (comp is BaseComponent baseComp)
                    {
                        _components.Add(baseComp);
                    }
                }
            }
            
            _modified = false;
            _canvas.Invalidate();
        }
        catch
        {
            // Fallback if dynamic access fails
            _canvas.Invalidate();
        }
    }

    /// <summary>
    /// Gets the graphics scene (canvas) for property editor integration.
    /// </summary>
    public Panel GetScene() => _canvas;

    /// <summary>
    /// Gets the currently selected component, if any.
    /// </summary>
    public BaseComponent? GetSelectedComponent() => _selectedComponent;

    /// <summary>
    /// Saves the screen.
    /// </summary>
    public bool SaveScreen()
    {
        if (_template == null)
            return false;

        try
        {
            // Save components to template using dynamic/reflection
            dynamic templateObj = _template;
            var components = templateObj.Components;
            if (components != null)
            {
                components.Clear();
                foreach (var comp in _components)
                {
                    components.Add(comp);
                }
            }
            
            templateObj.ModifiedDate = DateTime.Now;
            
            // Get file path and save to disk
            string? filePath = templateObj.FilePath?.ToString();
            if (string.IsNullOrEmpty(filePath))
            {
                // If no file path, try to construct it from SCADA project name and screen name
                if (!string.IsNullOrEmpty(_scadaProjectName))
                {
                    // This is a fallback - ideally FilePath should always be set
                    // We'd need access to ProjectManager to get the screens path
                    // For now, just return false if no path
                    return false;
                }
            }
            
            // Serialize template to JSON and write to file
            var toJsonMethod = templateObj.GetType().GetMethod("ToJson");
            if (toJsonMethod != null)
            {
                var json = toJsonMethod.Invoke(templateObj, null) as JObject;
                if (json != null && !string.IsNullOrEmpty(filePath))
                {
                    // Ensure directory exists
                    string? directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Write JSON to file
                    File.WriteAllText(filePath, json.ToString(Newtonsoft.Json.Formatting.Indented));
                    
                    // Save events.json if components have events
                    SaveEventsIfNeeded(filePath);
                }
            }
            
            _modified = false;
            ModifiedChanged?.Invoke(this, false);
            return true;
        }
        catch (Exception ex)
        {
            // Log error for debugging
            System.Diagnostics.Debug.WriteLine($"Error saving screen: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Saves events.json if any components have events associated.
    /// Note: Events are saved immediately when created/updated in PropertyEditor,
    /// but this ensures events.json exists in the project structure.
    /// </summary>
    private void SaveEventsIfNeeded(string screenFilePath)
    {
        try
        {
            // Determine events.json path (should be in json/ directory relative to screens)
            string? screenDir = Path.GetDirectoryName(screenFilePath);
            if (string.IsNullOrEmpty(screenDir))
                return;
                
            // Go up from screens/ to project root, then to json/
            string? projectRoot = Path.GetDirectoryName(screenDir); // screens -> project root
            if (string.IsNullOrEmpty(projectRoot))
                return;
                
            string jsonDir = Path.Combine(projectRoot, "json");
            if (!Directory.Exists(jsonDir))
            {
                Directory.CreateDirectory(jsonDir);
            }
            
            string eventsPath = Path.Combine(jsonDir, "events.json");
            
            // Ensure events.json exists (even if empty)
            // Events are saved immediately when created/updated via PropertyEditor
            if (!File.Exists(eventsPath))
            {
                // Create empty events.json file
                var emptyEventsJson = new JObject { ["events"] = new JArray() };
                File.WriteAllText(eventsPath, emptyEventsJson.ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error ensuring events.json exists: {ex.Message}");
        }
    }

    private void OnCanvasPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Apply zoom transform to scale everything
        g.ScaleTransform(_zoomFactor, _zoomFactor);

        // Draw grid (will be scaled by zoom transform)
        DrawGrid(g);

        // Draw components in ZOrder (will be scaled by zoom transform)
        foreach (var component in _components.OrderBy(c => c.ZOrder))
        {
            bool isSelected = component == _selectedComponent;
            component.Draw(g, isSelected);
            
            // Draw resize handles for selected component (scaled by zoom)
            if (isSelected)
            {
                DrawResizeHandles(g, component.Bounds);
            }
        }
        
        // Draw selection rectangle if dragging (scaled by zoom)
        if (_selectionRect.HasValue)
        {
            using (var pen = new Pen(Color.Blue, 2 / _zoomFactor) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                g.DrawRectangle(pen, _selectionRect.Value);
            }
        }
    }

    private void DrawGrid(Graphics g)
    {
        if (!_showGrid)
            return;
            
        int gridSize = 20;
        // Pen width should account for zoom - thinner lines when zoomed out
        var pen = new Pen(Color.LightGray, 1 / _zoomFactor);

        // Draw grid using actual canvas size (not zoomed)
        int actualCanvasWidth = (int)(_canvas.Width / _zoomFactor);
        int actualCanvasHeight = (int)(_canvas.Height / _zoomFactor);
        
        for (int x = 0; x <= actualCanvasWidth; x += gridSize)
        {
            g.DrawLine(pen, x, 0, x, actualCanvasHeight);
        }

        for (int y = 0; y <= actualCanvasHeight; y += gridSize)
        {
            g.DrawLine(pen, 0, y, actualCanvasWidth, y);
        }
    }

    private void OnContainerPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        
        // Draw visual boundary around the screen canvas
        Rectangle canvasBounds = new Rectangle(
            _canvas.Location.X - 1,
            _canvas.Location.Y - 1,
            _canvas.Width + 2,
            _canvas.Height + 2
        );
        
        // Draw border around screen bounds
        using (var pen = new Pen(Color.DarkGray, 2))
        {
            g.DrawRectangle(pen, canvasBounds);
        }
        
        // Draw corner indicators
        int cornerSize = 10;
        using (var brush = new SolidBrush(Color.DarkGray))
        {
            // Top-left corner
            g.FillRectangle(brush, canvasBounds.Left, canvasBounds.Top, cornerSize, 3);
            g.FillRectangle(brush, canvasBounds.Left, canvasBounds.Top, 3, cornerSize);
            
            // Top-right corner
            g.FillRectangle(brush, canvasBounds.Right - cornerSize, canvasBounds.Top, cornerSize, 3);
            g.FillRectangle(brush, canvasBounds.Right - 3, canvasBounds.Top, 3, cornerSize);
            
            // Bottom-left corner
            g.FillRectangle(brush, canvasBounds.Left, canvasBounds.Bottom - 3, cornerSize, 3);
            g.FillRectangle(brush, canvasBounds.Left, canvasBounds.Bottom - cornerSize, 3, cornerSize);
            
            // Bottom-right corner
            g.FillRectangle(brush, canvasBounds.Right - cornerSize, canvasBounds.Bottom - 3, cornerSize, 3);
            g.FillRectangle(brush, canvasBounds.Right - 3, canvasBounds.Bottom - cornerSize, 3, cornerSize);
        }
    }

    /// <summary>
    /// Updates the canvas size to match SCADA resolution and centers it in the container.
    /// </summary>
    private void UpdateCanvasSize()
    {
        if (_scadaResolution.Width > 0 && _scadaResolution.Height > 0)
        {
            // Set canvas size with zoom applied (default zoom is 100%)
            _canvas.Size = new Size(
                (int)(_scadaResolution.Width * _zoomFactor),
                (int)(_scadaResolution.Height * _zoomFactor)
            );
            UpdateCanvasPosition();
            
            // Update container's auto-scroll minimum size to include margins
            _canvasContainer.AutoScrollMinSize = new Size(
                _canvas.Width + (MarginSize * 2),
                _canvas.Height + (MarginSize * 2)
            );
            
            _canvas.Invalidate();
            _canvasContainer.Invalidate();
        }
    }
    
    /// <summary>
    /// Calculates initial zoom factor to fit the screen in the viewport.
    /// </summary>
    private void CalculateInitialZoom()
    {
        if (_canvasContainer == null || _scadaResolution.Width == 0 || _scadaResolution.Height == 0)
            return;
        
        // Get available space in container (accounting for margins)
        int availableWidth = _canvasContainer.ClientSize.Width - (MarginSize * 2);
        int availableHeight = _canvasContainer.ClientSize.Height - (MarginSize * 2);
        
        if (availableWidth <= 0 || availableHeight <= 0)
            return;
        
        // Calculate zoom to fit both width and height
        float zoomX = (float)availableWidth / _scadaResolution.Width;
        float zoomY = (float)availableHeight / _scadaResolution.Height;
        
        // Use the smaller zoom to ensure entire screen fits
        _zoomFactor = Math.Min(zoomX, zoomY);
        
        // Clamp zoom factor to reasonable bounds
        _zoomFactor = Math.Max(0.1f, Math.Min(_zoomFactor, 5.0f));
    }

    /// <summary>
    /// Centers the canvas in the container with margins.
    /// </summary>
    private void UpdateCanvasPosition()
    {
        // Center the canvas in the container, but ensure minimum margin
        int containerWidth = _canvasContainer.ClientSize.Width;
        int containerHeight = _canvasContainer.ClientSize.Height;
        
        int x = Math.Max(MarginSize, (containerWidth - _canvas.Width) / 2);
        int y = Math.Max(MarginSize, (containerHeight - _canvas.Height) / 2);
        
        _canvas.Location = new Point(x, y);
    }

    private void OnCanvasDragEnter(object? sender, DragEventArgs e)
    {
        // Check for component drag-drop
        if (e.Data?.GetDataPresent(ComponentsView.ComponentDragDropFormat) == true &&
            e.Data?.GetData(ComponentsView.ComponentDragDropFormat) is BaseComponent)
        {
            e.Effect = DragDropEffects.Copy;
        }
        // Check for screen drag-drop (using string literal to avoid circular dependency)
        else if (e.Data?.GetDataPresent("AccuTrack.SCADA.Screen") == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
        // Check for SVG drag-drop
        else if (e.Data?.GetDataPresent("AccuTrack.SCADA.SVG") == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private void OnCanvasDragDrop(object? sender, DragEventArgs e)
    {
        Point dropPoint = _canvas.PointToClient(new Point(e.X, e.Y));
        Point canvasDropPoint = ScreenToCanvas(dropPoint); // dropPoint is already relative to canvas
        
        // Handle component drag-drop
        if (e.Data?.GetData(ComponentsView.ComponentDragDropFormat) is BaseComponent component)
        {
            // Clone the component and place it at drop location
            var newComponent = component.Clone();
            Point location = SnapToGrid(canvasDropPoint);
            // Constrain to screen bounds
            location = ConstrainToScreenBounds(location, newComponent.Size);
            newComponent.Location = location;
            newComponent.Name = $"{component.ComponentType}_{_components.Count + 1}";
            
            // Only add if it fits within screen bounds
            if (IsWithinScreenBounds(location, newComponent.Size))
            {
                _components.Add(newComponent);
                SetSelectedComponent(newComponent);
                SetModified(true);
                _canvas.Invalidate();
            }
        }
        // Handle screen drag-drop - create navigation button (using string literal to avoid circular dependency)
        else if (e.Data?.GetData("AccuTrack.SCADA.Screen") is Dictionary<string, object> screenData)
        {
            try
            {
                string screenName = screenData.TryGetValue("screenName", out var name) ? name?.ToString() ?? "" : "";
                string screenId = screenData.TryGetValue("screenId", out var id) ? id?.ToString() ?? "" : "";
                
                if (!string.IsNullOrEmpty(screenName))
                {
                    // Create a navigation button component
                    Point location = SnapToGrid(canvasDropPoint);
                    Size buttonSize = new Size(120, 35);
                    // Constrain to screen bounds
                    location = ConstrainToScreenBounds(location, buttonSize);
                    
                    var button = new ButtonComponent
                    {
                        Id = Guid.NewGuid(),
                        Name = $"NavButton_{screenName}_{_components.Count + 1}",
                        Location = location,
                        Size = buttonSize,
                        Text = screenName,
                        Action = $"NavigateScreen:{screenName}", // Format: NavigateScreen:ScreenName
                        BackColor = Color.FromArgb(70, 130, 180), // Steel blue
                        ForeColor = Color.White,
                        BorderColor = Color.FromArgb(50, 100, 150),
                        BorderWidth = 2,
                        Font = new Font("Arial", 9, FontStyle.Bold),
                        Visible = true,
                        Enabled = true
                    };
                    
                    // Only add if it fits within screen bounds
                    if (IsWithinScreenBounds(location, buttonSize))
                    {
                        _components.Add(button);
                        
                        // Create navigation event automatically
                        CreateNavigationEvent(button, screenName, screenId);
                        
                        SetSelectedComponent(button);
                        SetModified(true);
                        _canvas.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating navigation button: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        // Handle SVG drag-drop - create SVG view component
        else if (e.Data?.GetData("AccuTrack.SCADA.SVG") is string svgPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(svgPath))
                {
                    Point location = SnapToGrid(canvasDropPoint);
                    Size svgSize = new Size(100, 100);
                    // Constrain to screen bounds
                    location = ConstrainToScreenBounds(location, svgSize);
                    
                    // Create an SVG view component
                    var svgComponent = new SvgViewComponent
                    {
                        Id = Guid.NewGuid(),
                        Name = $"SVGView_{Path.GetFileNameWithoutExtension(svgPath)}_{_components.Count + 1}",
                        Location = location,
                        Size = svgSize,
                        SvgPath = svgPath, // Store relative path from svg/ directory
                        BorderColor = Color.Gray,
                        BorderWidth = 0,
                        Visible = true,
                        Enabled = true
                    };
                    
                    // Only add if it fits within screen bounds
                    if (IsWithinScreenBounds(location, svgSize))
                    {
                        _components.Add(svgComponent);
                        SetSelectedComponent(svgComponent);
                        SetModified(true);
                        _canvas.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating SVG component: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Converts screen coordinates to canvas coordinates accounting for zoom.
    /// Note: For mouse events on _canvas, e.Location is already relative to the canvas.
    /// </summary>
    private Point ScreenToCanvas(Point screenPoint)
    {
        // e.Location from canvas mouse events is already relative to the canvas
        // Just need to convert from zoomed coordinates to actual canvas coordinates
        if (_zoomFactor <= 0)
            return screenPoint; // Safety check - avoid division by zero
        
        return new Point(
            (int)(screenPoint.X / _zoomFactor),
            (int)(screenPoint.Y / _zoomFactor)
        );
    }
    
    /// <summary>
    /// Converts container coordinates to canvas coordinates accounting for zoom and canvas position.
    /// Use this for drag-drop events that come from the container.
    /// </summary>
    private Point ContainerToCanvas(Point containerPoint)
    {
        // Account for canvas position in container
        Point relativeToCanvas = new Point(
            containerPoint.X - _canvas.Location.X,
            containerPoint.Y - _canvas.Location.Y
        );
        
        // Convert from zoomed coordinates to actual canvas coordinates
        return new Point(
            (int)(relativeToCanvas.X / _zoomFactor),
            (int)(relativeToCanvas.Y / _zoomFactor)
        );
    }

    private void OnCanvasMouseClick(object? sender, MouseEventArgs e)
    {
        Point clickPoint = ScreenToCanvas(e.Location);
        
        // Check if clicking on a component
        BaseComponent? clickedComponent = null;
        for (int i = _components.Count - 1; i >= 0; i--)
        {
            if (_components[i].Contains(clickPoint))
            {
                clickedComponent = _components[i];
                break;
            }
        }
        
        SetSelectedComponent(clickedComponent);
        
        // Note: Resize handle detection and resizing is handled in OnCanvasMouseDown
        // This method is mainly for component selection
    }

    private void OnCanvasMouseDoubleClick(object? sender, MouseEventArgs e)
    {
        Point clickPoint = ScreenToCanvas(e.Location);
        
        // Check if double-clicking on a component
        BaseComponent? clickedComponent = null;
        for (int i = _components.Count - 1; i >= 0; i--)
        {
            if (_components[i].Contains(clickPoint))
            {
                clickedComponent = _components[i];
                break;
            }
        }
        
        if (clickedComponent != null)
        {
            // Select the component first
            SetSelectedComponent(clickedComponent);
            
            // Trigger double-click event to open Properties tab
            ComponentDoubleClicked?.Invoke(this, clickedComponent);
        }
    }

    private void OnCanvasMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && _selectedComponent != null)
        {
            // Convert current mouse position to canvas coordinates
            Point currentCanvasPoint = ScreenToCanvas(e.Location);
            Point startCanvasPoint = ScreenToCanvas(_dragStartPoint);
            
            // Calculate the mouse movement delta in canvas coordinates
            int deltaX = currentCanvasPoint.X - startCanvasPoint.X;
            int deltaY = currentCanvasPoint.Y - startCanvasPoint.Y;
            
            // Calculate new location based on original component location + mouse delta
            Point newLocation = new Point(
                _dragStartComponentLocation.X + deltaX,
                _dragStartComponentLocation.Y + deltaY
            );
            
            // Snap to grid
            newLocation = SnapToGrid(newLocation);
            
            // Only constrain if the component would go outside bounds
            // This allows smooth dragging within bounds
            if (!IsWithinScreenBounds(newLocation, _selectedComponent.Size))
            {
                newLocation = ConstrainToScreenBounds(newLocation, _selectedComponent.Size);
            }
            
            _selectedComponent.Move(newLocation);
            InvalidateComponentRegion(_selectedComponent, _dragStartComponentLocation, newLocation);
        }
        else if (_isResizing && _selectedComponent != null && _activeResizeHandle != ResizeHandle.None)
        {
            // Convert current mouse position to canvas coordinates
            Point currentCanvasPoint = ScreenToCanvas(e.Location);
            Point startCanvasPoint = ScreenToCanvas(_dragStartPoint);
            
            // Calculate delta in canvas coordinates
            Point delta = new Point(
                currentCanvasPoint.X - startCanvasPoint.X,
                currentCanvasPoint.Y - startCanvasPoint.Y
            );
            Point newLocation = _dragStartComponentLocation;
            Size newSize = _dragStartComponentSize;
            
            // Calculate new size and location based on which handle is being dragged
            switch (_activeResizeHandle)
            {
                case ResizeHandle.TopLeft:
                    newLocation = new Point(
                        _dragStartComponentLocation.X + delta.X,
                        _dragStartComponentLocation.Y + delta.Y
                    );
                    newSize = new Size(
                        Math.Max(20, _dragStartComponentSize.Width - delta.X),
                        Math.Max(20, _dragStartComponentSize.Height - delta.Y)
                    );
                    // Adjust location if size changed
                    newLocation = new Point(
                        _dragStartComponentLocation.X + (_dragStartComponentSize.Width - newSize.Width),
                        _dragStartComponentLocation.Y + (_dragStartComponentSize.Height - newSize.Height)
                    );
                    break;
                    
                case ResizeHandle.Top:
                    newLocation = new Point(
                        _dragStartComponentLocation.X,
                        _dragStartComponentLocation.Y + delta.Y
                    );
                    newSize = new Size(
                        _dragStartComponentSize.Width,
                        Math.Max(20, _dragStartComponentSize.Height - delta.Y)
                    );
                    newLocation = new Point(
                        _dragStartComponentLocation.X,
                        _dragStartComponentLocation.Y + (_dragStartComponentSize.Height - newSize.Height)
                    );
                    break;
                    
                case ResizeHandle.TopRight:
                    newLocation = new Point(
                        _dragStartComponentLocation.X,
                        _dragStartComponentLocation.Y + delta.Y
                    );
                    newSize = new Size(
                        Math.Max(20, _dragStartComponentSize.Width + delta.X),
                        Math.Max(20, _dragStartComponentSize.Height - delta.Y)
                    );
                    newLocation = new Point(
                        _dragStartComponentLocation.X,
                        _dragStartComponentLocation.Y + (_dragStartComponentSize.Height - newSize.Height)
                    );
                    break;
                    
                case ResizeHandle.Right:
                    newSize = new Size(
                        Math.Max(20, _dragStartComponentSize.Width + delta.X),
                        _dragStartComponentSize.Height
                    );
                    break;
                    
                case ResizeHandle.BottomRight:
                    newSize = new Size(
                        Math.Max(20, _dragStartComponentSize.Width + delta.X),
                        Math.Max(20, _dragStartComponentSize.Height + delta.Y)
                    );
                    break;
                    
                case ResizeHandle.Bottom:
                    newSize = new Size(
                        _dragStartComponentSize.Width,
                        Math.Max(20, _dragStartComponentSize.Height + delta.Y)
                    );
                    break;
                    
                case ResizeHandle.BottomLeft:
                    newLocation = new Point(
                        _dragStartComponentLocation.X + delta.X,
                        _dragStartComponentLocation.Y
                    );
                    newSize = new Size(
                        Math.Max(20, _dragStartComponentSize.Width - delta.X),
                        Math.Max(20, _dragStartComponentSize.Height + delta.Y)
                    );
                    newLocation = new Point(
                        _dragStartComponentLocation.X + (_dragStartComponentSize.Width - newSize.Width),
                        _dragStartComponentLocation.Y
                    );
                    break;
                    
                case ResizeHandle.Left:
                    newLocation = new Point(
                        _dragStartComponentLocation.X + delta.X,
                        _dragStartComponentLocation.Y
                    );
                    newSize = new Size(
                        Math.Max(20, _dragStartComponentSize.Width - delta.X),
                        _dragStartComponentSize.Height
                    );
                    newLocation = new Point(
                        _dragStartComponentLocation.X + (_dragStartComponentSize.Width - newSize.Width),
                        _dragStartComponentLocation.Y
                    );
                    break;
            }
            
            // Snap to grid
            newLocation = SnapToGrid(newLocation);
            newSize = SnapSizeToGrid(newSize);
            
            // Constrain to screen bounds
            if (!IsWithinScreenBounds(newLocation, newSize))
            {
                // First constrain the size
                newSize = ConstrainSizeToScreenBounds(newLocation, newSize);
                // Then adjust location if needed (using actual canvas size)
                int actualCanvasWidth = (int)(_canvas.Width / _zoomFactor);
                int actualCanvasHeight = (int)(_canvas.Height / _zoomFactor);
                if (newLocation.X + newSize.Width > actualCanvasWidth)
                    newLocation = new Point(actualCanvasWidth - newSize.Width, newLocation.Y);
                if (newLocation.Y + newSize.Height > actualCanvasHeight)
                    newLocation = new Point(newLocation.X, actualCanvasHeight - newSize.Height);
                if (newLocation.X < 0)
                    newLocation = new Point(0, newLocation.Y);
                if (newLocation.Y < 0)
                    newLocation = new Point(newLocation.X, 0);
            }
            
            _selectedComponent.Move(newLocation);
            _selectedComponent.Resize(newSize);
            InvalidateComponentRegion(_selectedComponent, _dragStartComponentLocation, newLocation, _dragStartComponentSize, newSize);
        }
        else
        {
            // Update cursor based on resize handle
            if (_selectedComponent != null)
            {
                Point canvasPoint = ScreenToCanvas(e.Location);
                ResizeHandle handle = GetResizeHandleAtPoint(canvasPoint, _selectedComponent.Bounds);
                _canvas.Cursor = GetCursorForResizeHandle(handle);
            }
            else
            {
                _canvas.Cursor = Cursors.Default;
            }
        }
    }

    private void OnCanvasMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Point clickPoint = ScreenToCanvas(e.Location);
            
            // Check if clicking on selected component
            if (_selectedComponent != null && _selectedComponent.Contains(clickPoint))
            {
                _dragStartPoint = e.Location; // Store screen coordinates for delta calculation
                _dragStartComponentLocation = _selectedComponent.Location; // Store original location
                _isDragging = true;
            }
            else if (_selectedComponent != null)
            {
                _activeResizeHandle = GetResizeHandleAtPoint(clickPoint, _selectedComponent.Bounds);
                if (_activeResizeHandle != ResizeHandle.None)
                {
                    _dragStartPoint = e.Location; // Store screen coordinates for delta calculation
                    _dragStartComponentSize = _selectedComponent.Size;
                    _isResizing = true;
                }
            }
            else
            {
                // Start selection rectangle (in canvas coordinates)
                _selectionRect = new Rectangle(clickPoint, Size.Empty);
            }
        }
        else if (e.Button == MouseButtons.Right)
        {
            // Select component under cursor so context menu applies to it
            Point clickPoint = ScreenToCanvas(e.Location);
            BaseComponent? clickedComponent = null;
            for (int i = _components.Count - 1; i >= 0; i--)
            {
                if (_components[i].Contains(clickPoint))
                {
                    clickedComponent = _components[i];
                    break;
                }
            }
            SetSelectedComponent(clickedComponent);
        }
    }

    private void OnCanvasMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (_isDragging || _isResizing)
            {
                SetModified(true);
            }
            else if (_selectionRect.HasValue)
            {
                // Convert selection rectangle to canvas coordinates if needed
                // Selection rectangle is already in canvas coordinates from MouseDown
                SetModified(true);
            }
            _isDragging = false;
            _isResizing = false;
            _activeResizeHandle = ResizeHandle.None;
            _selectionRect = null;
            _canvas.Cursor = Cursors.Default;
            _canvas.Invalidate();
        }
    }

    private void SetSelectedComponent(BaseComponent? component)
    {
        if (_selectedComponent != component)
        {
            _selectedComponent = component;
            SelectionChanged?.Invoke(this, component);
            _canvas.Invalidate();
        }
    }

    private void SetModified(bool modified)
    {
        if (_modified != modified)
        {
            _modified = modified;
            ModifiedChanged?.Invoke(this, modified);
        }
    }

    private bool _snapToGrid = true;
    private bool _showGrid = true;
    
    /// <summary>
    /// Gets or sets whether to snap components to grid.
    /// </summary>
    public bool SnapToGridEnabled
    {
        get => _snapToGrid;
        set => _snapToGrid = value;
    }
    
    /// <summary>
    /// Gets or sets whether to show the grid.
    /// </summary>
    public bool ShowGridEnabled
    {
        get => _showGrid;
        set
        {
            _showGrid = value;
            _canvas.Invalidate();
        }
    }

    private Point SnapToGrid(Point point)
    {
        if (!_snapToGrid)
            return point;
            
        int gridSize = 20;
        return new Point(
            (point.X / gridSize) * gridSize,
            (point.Y / gridSize) * gridSize
        );
    }
    
    private Size SnapSizeToGrid(Size size)
    {
        if (!_snapToGrid)
            return size;
            
        int gridSize = 20;
        return new Size(
            ((size.Width + gridSize / 2) / gridSize) * gridSize,
            ((size.Height + gridSize / 2) / gridSize) * gridSize
        );
    }
    
    /// <summary>
    /// Gets the appropriate cursor for a resize handle.
    /// </summary>
    private Cursor GetCursorForResizeHandle(ResizeHandle handle)
    {
        return handle switch
        {
            ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
            ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
            ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
            ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
            _ => Cursors.Default
        };
    }

    /// <summary>
    /// Gets which resize handle (if any) the point is on.
    /// </summary>
    private ResizeHandle GetResizeHandleAtPoint(Point point, Rectangle bounds)
    {
        int halfSize = ResizeHandleSize / 2;
        int tolerance = ResizeHandleSize;
        
        // Check corners first (they take priority)
        if (Math.Abs(point.X - bounds.Left) <= tolerance && Math.Abs(point.Y - bounds.Top) <= tolerance)
            return ResizeHandle.TopLeft;
        if (Math.Abs(point.X - bounds.Right) <= tolerance && Math.Abs(point.Y - bounds.Top) <= tolerance)
            return ResizeHandle.TopRight;
        if (Math.Abs(point.X - bounds.Right) <= tolerance && Math.Abs(point.Y - bounds.Bottom) <= tolerance)
            return ResizeHandle.BottomRight;
        if (Math.Abs(point.X - bounds.Left) <= tolerance && Math.Abs(point.Y - bounds.Bottom) <= tolerance)
            return ResizeHandle.BottomLeft;
        
        // Check edges
        if (Math.Abs(point.X - bounds.Left) <= tolerance && point.Y >= bounds.Top && point.Y <= bounds.Bottom)
            return ResizeHandle.Left;
        if (Math.Abs(point.X - bounds.Right) <= tolerance && point.Y >= bounds.Top && point.Y <= bounds.Bottom)
            return ResizeHandle.Right;
        if (Math.Abs(point.Y - bounds.Top) <= tolerance && point.X >= bounds.Left && point.X <= bounds.Right)
            return ResizeHandle.Top;
        if (Math.Abs(point.Y - bounds.Bottom) <= tolerance && point.X >= bounds.Left && point.X <= bounds.Right)
            return ResizeHandle.Bottom;
        
        return ResizeHandle.None;
    }
    
    /// <summary>
    /// Legacy method for compatibility - checks if point is on any resize handle.
    /// </summary>
    private bool IsPointOnResizeHandle(Point point, Rectangle bounds)
    {
        return GetResizeHandleAtPoint(point, bounds) != ResizeHandle.None;
    }

    private void DrawResizeHandles(Graphics g, Rectangle bounds)
    {
        // Draw resize handles on all corners and edges
        // Note: Graphics is already scaled by zoom, so handles will be scaled automatically
        using (var brush = new SolidBrush(Color.Blue))
        using (var outlinePen = new Pen(Color.White, 1 / _zoomFactor))
        {
            int size = ResizeHandleSize;
            int halfSize = size / 2;
            
            // Draw corner handles (larger, square)
            DrawHandle(g, brush, outlinePen, bounds.Left - halfSize, bounds.Top - halfSize, size, size); // Top-left
            DrawHandle(g, brush, outlinePen, bounds.Right - halfSize, bounds.Top - halfSize, size, size); // Top-right
            DrawHandle(g, brush, outlinePen, bounds.Right - halfSize, bounds.Bottom - halfSize, size, size); // Bottom-right
            DrawHandle(g, brush, outlinePen, bounds.Left - halfSize, bounds.Bottom - halfSize, size, size); // Bottom-left
            
            // Draw edge handles (smaller, rectangular)
            int edgeHandleWidth = 6;
            int edgeHandleHeight = size;
            
            // Top edge
            DrawHandle(g, brush, outlinePen, bounds.Left + bounds.Width / 2 - edgeHandleWidth / 2, bounds.Top - halfSize, edgeHandleWidth, edgeHandleHeight);
            // Right edge
            DrawHandle(g, brush, outlinePen, bounds.Right - halfSize, bounds.Top + bounds.Height / 2 - edgeHandleWidth / 2, edgeHandleHeight, edgeHandleWidth);
            // Bottom edge
            DrawHandle(g, brush, outlinePen, bounds.Left + bounds.Width / 2 - edgeHandleWidth / 2, bounds.Bottom - halfSize, edgeHandleWidth, edgeHandleHeight);
            // Left edge
            DrawHandle(g, brush, outlinePen, bounds.Left - halfSize, bounds.Top + bounds.Height / 2 - edgeHandleWidth / 2, edgeHandleHeight, edgeHandleWidth);
        }
    }
    
    private void DrawHandle(Graphics g, Brush brush, Pen outlinePen, int x, int y, int width, int height)
    {
        Rectangle handleRect = new Rectangle(x, y, width, height);
        g.FillRectangle(brush, handleRect);
        g.DrawRectangle(outlinePen, handleRect);
    }

    /// <summary>
    /// Zooms in the canvas.
    /// </summary>
    public void ZoomIn()
    {
        _zoomFactor = Math.Min(_zoomFactor * 1.2f, 5.0f);
        UpdateZoom();
    }

    /// <summary>
    /// Zooms out the canvas.
    /// </summary>
    public void ZoomOut()
    {
        _zoomFactor = Math.Max(_zoomFactor / 1.2f, 0.2f);
        UpdateZoom();
    }

    /// <summary>
    /// Resets zoom to 100%.
    /// </summary>
    public void ResetZoom()
    {
        _zoomFactor = 1.0f;
        UpdateZoom();
    }
    
    /// <summary>
    /// Gets the current zoom factor.
    /// </summary>
    public float GetZoomFactor() => _zoomFactor;

    private void UpdateZoom()
    {
        if (_scadaResolution.Width > 0 && _scadaResolution.Height > 0)
        {
            _canvas.Size = new Size(
                (int)(_scadaResolution.Width * _zoomFactor),
                (int)(_scadaResolution.Height * _zoomFactor)
            );
            UpdateCanvasPosition();
            _canvas.Invalidate();
            _canvasContainer.Invalidate();
        }
        else if (_template != null)
        {
            try
            {
                dynamic templateObj = _template;
                var size = templateObj.Size;
                if (size != null)
                {
                    _canvas.Size = new Size(
                        (int)(size.Width * _zoomFactor),
                        (int)(size.Height * _zoomFactor)
                    );
                    UpdateCanvasPosition();
                    _canvas.Invalidate();
                    _canvasContainer.Invalidate();
                }
            }
            catch
            {
                // Fallback
                _canvas.Invalidate();
                _canvasContainer.Invalidate();
            }
        }
    }

    /// <summary>
    /// Checks if a location and size are within the screen bounds.
    /// Uses actual canvas size (not zoomed size).
    /// </summary>
    private bool IsWithinScreenBounds(Point location, Size size)
    {
        int actualCanvasWidth = (int)(_canvas.Width / _zoomFactor);
        int actualCanvasHeight = (int)(_canvas.Height / _zoomFactor);
        return location.X >= 0 && location.Y >= 0 &&
               location.X + size.Width <= actualCanvasWidth &&
               location.Y + size.Height <= actualCanvasHeight;
    }

    /// <summary>
    /// Constrains a location to ensure the component stays within screen bounds.
    /// Uses actual canvas size (not zoomed size).
    /// </summary>
    private Point ConstrainToScreenBounds(Point location, Size size)
    {
        int actualCanvasWidth = (int)(_canvas.Width / _zoomFactor);
        int actualCanvasHeight = (int)(_canvas.Height / _zoomFactor);
        int x = Math.Max(0, Math.Min(location.X, actualCanvasWidth - size.Width));
        int y = Math.Max(0, Math.Min(location.Y, actualCanvasHeight - size.Height));
        return new Point(x, y);
    }

    /// <summary>
    /// Alignment types for component alignment.
    /// </summary>
    private enum Alignment
    {
        Left,
        Right,
        Top,
        Bottom,
        CenterHorizontal,
        CenterVertical
    }

    /// <summary>
    /// Copies the selected component(s) to clipboard.
    /// </summary>
    public void CopySelectedComponents()
    {
        var componentsToCopy = GetSelectedComponents();
        if (componentsToCopy.Count == 0)
            return;

        // Deep copy components using Clone()
        _clipboardComponents = new List<BaseComponent>();
        foreach (var comp in componentsToCopy)
        {
            var copy = comp.Clone();
            copy.Id = Guid.NewGuid(); // New ID for pasted component
            copy.Location = new Point(comp.Location.X + 20, comp.Location.Y + 20); // Offset for paste
            _clipboardComponents.Add(copy);
        }
    }

    /// <summary>
    /// Cuts the selected component(s) to clipboard (copies and deletes).
    /// </summary>
    public void CutSelectedComponents()
    {
        CopySelectedComponents();
        DeleteSelectedComponents();
    }

    /// <summary>
    /// Deletes the selected component(s).
    /// </summary>
    public void DeleteSelectedComponents()
    {
        var selected = GetSelectedComponents();
        if (selected.Count == 0)
            return;

        foreach (var comp in selected)
        {
            _components.Remove(comp);
        }

        _selectedComponent = null;
        _selectedComponents.Clear();
        SetModified(true);
        SelectionChanged?.Invoke(this, null);
        _canvas.Invalidate();
    }

    /// <summary>
    /// Pastes components from clipboard.
    /// </summary>
    public void PasteComponents()
    {
        if (_clipboardComponents == null || _clipboardComponents.Count == 0)
            return;

        // Clear current selection
        _selectedComponent = null;
        _selectedComponents.Clear();

        // Add pasted components
        foreach (var comp in _clipboardComponents)
        {
            // Ensure component is within bounds
            comp.Location = ConstrainToScreenBounds(comp.Location, comp.Size);
            _components.Add(comp);
            _selectedComponents.Add(comp);
        }

        if (_selectedComponents.Count > 0)
        {
            _selectedComponent = _selectedComponents[0];
        }

        SetModified(true);
        SelectionChanged?.Invoke(this, _selectedComponent);
        _canvas.Invalidate();
    }

    /// <summary>
    /// Gets the list of currently selected components.
    /// </summary>
    private List<BaseComponent> GetSelectedComponents()
    {
        var selected = new List<BaseComponent>();
        if (_selectedComponent != null)
        {
            selected.Add(_selectedComponent);
        }
        // Add multi-selected components if any
        foreach (var comp in _selectedComponents)
        {
            if (comp != _selectedComponent && !selected.Contains(comp))
            {
                selected.Add(comp);
            }
        }
        return selected;
    }

    /// <summary>
    /// Aligns selected components according to the specified alignment type.
    /// </summary>
    private void AlignSelectedComponents(Alignment alignment)
    {
        var selected = GetSelectedComponents();
        if (selected.Count < 2)
            return; // Need at least 2 components to align

        // Find reference component (first selected)
        var reference = selected[0];
        int refLeft = reference.Location.X;
        int refRight = reference.Location.X + reference.Size.Width;
        int refTop = reference.Location.Y;
        int refBottom = reference.Location.Y + reference.Size.Height;
        int refCenterX = reference.Location.X + reference.Size.Width / 2;
        int refCenterY = reference.Location.Y + reference.Size.Height / 2;

        // Align all other components
        for (int i = 1; i < selected.Count; i++)
        {
            var comp = selected[i];
            Point newLocation = comp.Location;

            switch (alignment)
            {
                case Alignment.Left:
                    newLocation.X = refLeft;
                    break;
                case Alignment.Right:
                    newLocation.X = refRight - comp.Size.Width;
                    break;
                case Alignment.Top:
                    newLocation.Y = refTop;
                    break;
                case Alignment.Bottom:
                    newLocation.Y = refBottom - comp.Size.Height;
                    break;
                case Alignment.CenterHorizontal:
                    newLocation.X = refCenterX - comp.Size.Width / 2;
                    break;
                case Alignment.CenterVertical:
                    newLocation.Y = refCenterY - comp.Size.Height / 2;
                    break;
            }

            // Constrain to screen bounds
            newLocation = ConstrainToScreenBounds(newLocation, comp.Size);
            comp.Location = SnapToGrid(newLocation);
        }

        SetModified(true);
        _canvas.Invalidate();
    }

    /// <summary>
    /// Checks if there are components in the clipboard.
    /// </summary>
    public bool HasClipboardComponents() => _clipboardComponents != null && _clipboardComponents.Count > 0;

    /// <summary>
    /// Creates a navigation event for a button component.
    /// </summary>
    private void CreateNavigationEvent(ButtonComponent button, string targetScreenName, string targetScreenId)
    {
        if (_eventsModule == null)
        {
            System.Diagnostics.Debug.WriteLine("[ScreenEditor] EventsModule not initialized, cannot create navigation event");
            return;
        }

        try
        {
            // Create event trigger
            var trigger = new EventTrigger
            {
                ComponentId = button.Id.ToString(),
                Type = "OnClick"
            };

            // Create navigation action
            var action = new EventAction
            {
                Type = "NavigateScreen",
                ScreenId = !string.IsNullOrEmpty(targetScreenId) ? targetScreenId : targetScreenName
            };

            // Create event
            var evt = new ScadaEvent
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Navigate to {targetScreenName}",
                Description = $"Navigation event for button '{button.Name}' to screen '{targetScreenName}'",
                Trigger = trigger
            };
            evt.Actions.Add(action);
            evt.Metadata.CreatedBy = "System";
            evt.Metadata.CreatedAt = DateTime.Now.ToString("O");

            // Add event to EventsModule
            if (_eventsModule.AddEvent(evt))
            {
                // Link event to button via EventIds
                button.EventIds.Add(evt.Id);
                System.Diagnostics.Debug.WriteLine($"[ScreenEditor] Created navigation event '{evt.Id}' for button '{button.Name}' to screen '{targetScreenName}'");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenEditor] Failed to add navigation event for button '{button.Name}'");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScreenEditor] Error creating navigation event: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ScreenEditor] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Constrains a size to ensure the component stays within screen bounds.
    /// Uses actual canvas size (not zoomed size).
    /// </summary>
    private Size ConstrainSizeToScreenBounds(Point location, Size size)
    {
        int actualCanvasWidth = (int)(_canvas.Width / _zoomFactor);
        int actualCanvasHeight = (int)(_canvas.Height / _zoomFactor);
        int maxWidth = actualCanvasWidth - location.X;
        int maxHeight = actualCanvasHeight - location.Y;
        return new Size(
            Math.Min(size.Width, maxWidth),
            Math.Min(size.Height, maxHeight)
        );
    }

    /// <summary>
    /// Converts a canvas-space rectangle to client (zoomed) coordinates with padding for handles.
    /// </summary>
    private Rectangle CanvasRectToClient(Rectangle canvasRect)
    {
        int pad = ResizeHandleSize + 2;
        return new Rectangle(
            (int)((canvasRect.X - pad) * _zoomFactor),
            (int)((canvasRect.Y - pad) * _zoomFactor),
            Math.Max(1, (int)((canvasRect.Width + 2 * pad) * _zoomFactor)),
            Math.Max(1, (int)((canvasRect.Height + 2 * pad) * _zoomFactor)));
    }

    /// <summary>
    /// Invalidates only the region affected by a component move (reduces flicker).
    /// </summary>
    private void InvalidateComponentRegion(BaseComponent component, Point oldLocation, Point newLocation, Size? oldSize = null, Size? newSize = null)
    {
        Size os = oldSize ?? component.Size;
        Size ns = newSize ?? component.Size;
        var oldBounds = new Rectangle(oldLocation, os);
        var newBounds = new Rectangle(newLocation, ns);
        var union = Rectangle.Union(oldBounds, newBounds);
        _canvas.Invalidate(CanvasRectToClient(union));
    }

    private ContextMenuStrip CreateCanvasContextMenu()
    {
        var menu = new ContextMenuStrip();

        var bringToFront = new ToolStripMenuItem("Bring to Front");
        bringToFront.Click += (s, e) => BringSelectedToFront();
        menu.Items.Add(bringToFront);

        var sendToBack = new ToolStripMenuItem("Send to Back");
        sendToBack.Click += (s, e) => SendSelectedToBack();
        menu.Items.Add(sendToBack);

        menu.Items.Add(new ToolStripSeparator());

        var alignMenu = new ToolStripMenuItem("Align");
        alignMenu.DropDownItems.Add("Align Left", null, (_, _) => AlignSelectedComponents(Alignment.Left));
        alignMenu.DropDownItems.Add("Align Right", null, (_, _) => AlignSelectedComponents(Alignment.Right));
        alignMenu.DropDownItems.Add("Align Top", null, (_, _) => AlignSelectedComponents(Alignment.Top));
        alignMenu.DropDownItems.Add("Align Bottom", null, (_, _) => AlignSelectedComponents(Alignment.Bottom));
        alignMenu.DropDownItems.Add("Center Horizontal", null, (_, _) => AlignSelectedComponents(Alignment.CenterHorizontal));
        alignMenu.DropDownItems.Add("Center Vertical", null, (_, _) => AlignSelectedComponents(Alignment.CenterVertical));
        menu.Items.Add(alignMenu);

        menu.Items.Add(new ToolStripSeparator());

        var cutItem = new ToolStripMenuItem("Cut");
        cutItem.Click += (s, e) => CutSelectedComponents();
        menu.Items.Add(cutItem);

        var copyItem = new ToolStripMenuItem("Copy");
        copyItem.Click += (s, e) => CopySelectedComponents();
        menu.Items.Add(copyItem);

        var pasteItem = new ToolStripMenuItem("Paste");
        pasteItem.Click += (s, e) => PasteComponents();
        menu.Items.Add(pasteItem);

        var deleteItem = new ToolStripMenuItem("Delete");
        deleteItem.Click += (s, e) => DeleteSelectedComponents();
        menu.Items.Add(deleteItem);

        menu.Items.Add(new ToolStripSeparator());

        var propertiesItem = new ToolStripMenuItem("Properties...");
        propertiesItem.Click += (s, e) =>
        {
            if (_selectedComponent != null)
                ComponentDoubleClicked?.Invoke(this, _selectedComponent);
        };
        menu.Items.Add(propertiesItem);

        menu.Opening += (s, e) =>
        {
            var selected = GetSelectedComponents();
            bool hasSelection = selected.Count > 0;
            bool hasMultiple = selected.Count >= 2;
            bringToFront.Enabled = hasSelection;
            sendToBack.Enabled = hasSelection;
            alignMenu.Enabled = hasMultiple;
            cutItem.Enabled = hasSelection;
            copyItem.Enabled = hasSelection;
            pasteItem.Enabled = HasClipboardComponents();
            deleteItem.Enabled = hasSelection;
            propertiesItem.Enabled = _selectedComponent != null;
        };

        return menu;
    }

    private void BringSelectedToFront()
    {
        var selected = GetSelectedComponents();
        if (selected.Count == 0) return;
        int maxZ = _components.Max(c => c.ZOrder);
        foreach (var comp in selected)
        {
            comp.ZOrder = maxZ + 1;
            maxZ = comp.ZOrder;
        }
        SetModified(true);
        _canvas.Invalidate();
    }

    private void SendSelectedToBack()
    {
        var selected = GetSelectedComponents();
        if (selected.Count == 0) return;
        int minZ = _components.Min(c => c.ZOrder);
        foreach (var comp in selected)
        {
            comp.ZOrder = minZ - 1;
            minZ = comp.ZOrder;
        }
        SetModified(true);
        _canvas.Invalidate();
    }
}

/// <summary>
/// Double-buffered panel to reduce flicker when redrawing the canvas.
/// </summary>
internal sealed class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        var prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
        prop?.SetValue(this, true);
    }
}
