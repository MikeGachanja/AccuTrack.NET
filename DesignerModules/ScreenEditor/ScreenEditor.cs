using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.Components;
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
    private Size _scadaResolution = new Size(1920, 1080); // Default SCADA resolution
    private bool _modified;
    private float _zoomFactor = 1.0f;
    
    // Component management fields
    private List<BaseComponent> _components = new List<BaseComponent>();
    private BaseComponent? _selectedComponent;
    private Point _dragStartPoint;
    private Point _dragStartComponentLocation; // Original component location when drag starts
    private Size _dragStartComponentSize; // Original component size when resize starts
    private bool _isDragging;
    private bool _isResizing;
    private Rectangle? _selectionRect;
    
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
        toolbar.Items.Add(showGridButton);

        var snapToGridButton = new ToolStripButton("Snap to Grid") { CheckOnClick = true, Checked = true };
        toolbar.Items.Add(snapToGridButton);

        // Container panel with margins for centering the canvas
        _canvasContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.LightGray, // Background color outside the screen bounds
            AutoScroll = true,
            Padding = new Padding(MarginSize)
        };

        // Canvas for screen design - sized to exactly match SCADA resolution
        _canvas = new Panel
        {
            BackColor = Color.White,
            Size = _scadaResolution,
            Location = new Point(MarginSize, MarginSize)
        };

        _canvas.Paint += OnCanvasPaint;
        _canvas.MouseClick += OnCanvasMouseClick;
        _canvas.MouseMove += OnCanvasMouseMove;
        _canvas.MouseDown += OnCanvasMouseDown;
        _canvas.MouseUp += OnCanvasMouseUp;
        _canvas.AllowDrop = true;
        _canvas.DragEnter += OnCanvasDragEnter;
        _canvas.DragDrop += OnCanvasDragDrop;

        _canvasContainer.Paint += OnContainerPaint;
        _canvasContainer.Controls.Add(_canvas);
        _canvasContainer.Scroll += (s, e) => _canvas.Invalidate(); // Invalidate canvas when scrolling
        _canvasContainer.Resize += (s, e) => UpdateCanvasPosition(); // Recenter canvas on resize

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
    /// Sets the SCADA resolution (screen size) for this editor.
    /// </summary>
    public void SetScadaResolution(Size resolution)
    {
        _scadaResolution = resolution;
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

        // Draw grid
        DrawGrid(g);

        // Draw components
        foreach (var component in _components)
        {
            bool isSelected = component == _selectedComponent;
            component.Draw(g, isSelected);
            
            // Draw resize handles for selected component
            if (isSelected)
            {
                DrawResizeHandles(g, component.Bounds);
            }
        }
        
        // Draw selection rectangle if dragging
        if (_selectionRect.HasValue)
        {
            using (var pen = new Pen(Color.Blue, 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
            {
                g.DrawRectangle(pen, _selectionRect.Value);
            }
        }
    }

    private void DrawGrid(Graphics g)
    {
        int gridSize = 20;
        var pen = new Pen(Color.LightGray, 1);

        for (int x = 0; x < _canvas.Width; x += gridSize)
        {
            g.DrawLine(pen, x, 0, x, _canvas.Height);
        }

        for (int y = 0; y < _canvas.Height; y += gridSize)
        {
            g.DrawLine(pen, 0, y, _canvas.Width, y);
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
            _canvas.Size = _scadaResolution;
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
        
        // Handle component drag-drop
        if (e.Data?.GetData(ComponentsView.ComponentDragDropFormat) is BaseComponent component)
        {
            // Clone the component and place it at drop location
            var newComponent = component.Clone();
            Point location = SnapToGrid(dropPoint);
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
                
                if (!string.IsNullOrEmpty(screenName))
                {
                    // Create a navigation button component
                    Point location = SnapToGrid(dropPoint);
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
                        SetSelectedComponent(button);
                        SetModified(true);
                        _canvas.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating navigation button: {ex.Message}");
            }
        }
        // Handle SVG drag-drop - create SVG view component
        else if (e.Data?.GetData("AccuTrack.SCADA.SVG") is string svgPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(svgPath))
                {
                    Point location = SnapToGrid(dropPoint);
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
                        BorderWidth = 1,
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

    private void OnCanvasMouseClick(object? sender, MouseEventArgs e)
    {
        Point clickPoint = e.Location;
        
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

    private void OnCanvasMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && _selectedComponent != null)
        {
            // Calculate the mouse movement delta from the original drag start point
            int deltaX = e.X - _dragStartPoint.X;
            int deltaY = e.Y - _dragStartPoint.Y;
            
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
            _canvas.Invalidate();
        }
        else if (_isResizing && _selectedComponent != null && _activeResizeHandle != ResizeHandle.None)
        {
            Point delta = new Point(e.X - _dragStartPoint.X, e.Y - _dragStartPoint.Y);
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
                // Then adjust location if needed
                if (newLocation.X + newSize.Width > _canvas.Width)
                    newLocation = new Point(_canvas.Width - newSize.Width, newLocation.Y);
                if (newLocation.Y + newSize.Height > _canvas.Height)
                    newLocation = new Point(newLocation.X, _canvas.Height - newSize.Height);
                if (newLocation.X < 0)
                    newLocation = new Point(0, newLocation.Y);
                if (newLocation.Y < 0)
                    newLocation = new Point(newLocation.X, 0);
            }
            
            _selectedComponent.Move(newLocation);
            _selectedComponent.Resize(newSize);
            _canvas.Invalidate();
        }
        else
        {
            // Update cursor based on resize handle
            if (_selectedComponent != null)
            {
                ResizeHandle handle = GetResizeHandleAtPoint(e.Location, _selectedComponent.Bounds);
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
            Point clickPoint = e.Location;
            
            // Check if clicking on selected component
            if (_selectedComponent != null && _selectedComponent.Contains(clickPoint))
            {
                _dragStartPoint = clickPoint;
                _dragStartComponentLocation = _selectedComponent.Location; // Store original location
                _isDragging = true;
            }
            else if (_selectedComponent != null)
            {
                _activeResizeHandle = GetResizeHandleAtPoint(clickPoint, _selectedComponent.Bounds);
                if (_activeResizeHandle != ResizeHandle.None)
                {
                    _dragStartPoint = clickPoint;
                    _dragStartComponentSize = _selectedComponent.Size;
                    _isResizing = true;
                }
            }
            else
            {
                // Start selection rectangle
                _selectionRect = new Rectangle(clickPoint, Size.Empty);
            }
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

    private Point SnapToGrid(Point point)
    {
        int gridSize = 20;
        return new Point(
            (point.X / gridSize) * gridSize,
            (point.Y / gridSize) * gridSize
        );
    }
    
    private Size SnapSizeToGrid(Size size)
    {
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
        using (var brush = new SolidBrush(Color.Blue))
        using (var outlinePen = new Pen(Color.White, 1))
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

    private void ZoomIn()
    {
        _zoomFactor = Math.Min(_zoomFactor * 1.2f, 5.0f);
        UpdateZoom();
    }

    private void ZoomOut()
    {
        _zoomFactor = Math.Max(_zoomFactor / 1.2f, 0.2f);
        UpdateZoom();
    }

    private void ResetZoom()
    {
        _zoomFactor = 1.0f;
        UpdateZoom();
    }

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
    /// </summary>
    private bool IsWithinScreenBounds(Point location, Size size)
    {
        return location.X >= 0 && location.Y >= 0 &&
               location.X + size.Width <= _canvas.Width &&
               location.Y + size.Height <= _canvas.Height;
    }

    /// <summary>
    /// Constrains a location to ensure the component stays within screen bounds.
    /// </summary>
    private Point ConstrainToScreenBounds(Point location, Size size)
    {
        int x = Math.Max(0, Math.Min(location.X, _canvas.Width - size.Width));
        int y = Math.Max(0, Math.Min(location.Y, _canvas.Height - size.Height));
        return new Point(x, y);
    }

    /// <summary>
    /// Constrains a size to ensure the component stays within screen bounds.
    /// </summary>
    private Size ConstrainSizeToScreenBounds(Point location, Size size)
    {
        int maxWidth = _canvas.Width - location.X;
        int maxHeight = _canvas.Height - location.Y;
        return new Size(
            Math.Min(size.Width, maxWidth),
            Math.Min(size.Height, maxHeight)
        );
    }
}
