using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.ScreenEditor;
using Designer.Modules.Components;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.ScreenEditor;

/// <summary>
/// Editor for screen templates with visual design capabilities.
/// </summary>
public partial class ScreenEditor : UserControl
{
    private Panel _canvas;
    private object? _template; // Changed to object to avoid circular dependency
    private string? _scadaProjectName; // Store name instead of object to avoid circular dependency
    private bool _modified;
    private float _zoomFactor = 1.0f;
    
    // Component management fields
    private List<BaseComponent> _components = new List<BaseComponent>();
    private BaseComponent? _selectedComponent;
    private Point _dragStartPoint;
    private bool _isDragging;
    private bool _isResizing;
    private Rectangle? _selectionRect;
    
    private const int ResizeHandleSize = 8;

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

        // Canvas for screen design
        _canvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            AutoScroll = true
        };

        _canvas.Paint += OnCanvasPaint;
        _canvas.MouseClick += OnCanvasMouseClick;
        _canvas.MouseMove += OnCanvasMouseMove;
        _canvas.MouseDown += OnCanvasMouseDown;
        _canvas.MouseUp += OnCanvasMouseUp;
        _canvas.AllowDrop = true;
        _canvas.DragEnter += OnCanvasDragEnter;
        _canvas.DragDrop += OnCanvasDragDrop;

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_canvas, 0, 1);
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
                _canvas.Size = new Size(size.Width, size.Height);
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
            newComponent.Location = SnapToGrid(dropPoint);
            newComponent.Name = $"{component.ComponentType}_{_components.Count + 1}";
            
            _components.Add(newComponent);
            SetSelectedComponent(newComponent);
            SetModified(true);
            _canvas.Invalidate();
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
                    var button = new ButtonComponent
                    {
                        Id = Guid.NewGuid(),
                        Name = $"NavButton_{screenName}_{_components.Count + 1}",
                        Location = SnapToGrid(dropPoint),
                        Size = new Size(120, 35),
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
                    
                    _components.Add(button);
                    SetSelectedComponent(button);
                    SetModified(true);
                    _canvas.Invalidate();
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
                    // Create an SVG view component
                    var svgComponent = new SvgViewComponent
                    {
                        Id = Guid.NewGuid(),
                        Name = $"SVGView_{Path.GetFileNameWithoutExtension(svgPath)}_{_components.Count + 1}",
                        Location = SnapToGrid(dropPoint),
                        Size = new Size(100, 100), // Default size for SVG
                        SvgPath = svgPath, // Store relative path from svg/ directory
                        BorderColor = Color.Gray,
                        BorderWidth = 1,
                        Visible = true,
                        Enabled = true
                    };
                    
                    _components.Add(svgComponent);
                    SetSelectedComponent(svgComponent);
                    SetModified(true);
                    _canvas.Invalidate();
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
        
        // Check if clicking on resize handle
        if (_selectedComponent != null && IsPointOnResizeHandle(clickPoint, _selectedComponent.Bounds))
        {
            _isResizing = true;
        }
    }

    private void OnCanvasMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && _selectedComponent != null)
        {
            Point newLocation = SnapToGrid(new Point(
                e.X - _dragStartPoint.X + _selectedComponent.Location.X,
                e.Y - _dragStartPoint.Y + _selectedComponent.Location.Y
            ));
            _selectedComponent.Move(newLocation);
            _canvas.Invalidate();
        }
        else if (_isResizing && _selectedComponent != null)
        {
            Point delta = new Point(e.X - _dragStartPoint.X, e.Y - _dragStartPoint.Y);
            Size newSize = new Size(
                Math.Max(20, _selectedComponent.Size.Width + delta.X),
                Math.Max(20, _selectedComponent.Size.Height + delta.Y)
            );
            _selectedComponent.Resize(newSize);
            _canvas.Invalidate();
        }
        else
        {
            // Update cursor if over resize handle
            if (_selectedComponent != null && IsPointOnResizeHandle(e.Location, _selectedComponent.Bounds))
            {
                _canvas.Cursor = Cursors.SizeNWSE;
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
                _isDragging = true;
            }
            else if (_selectedComponent != null && IsPointOnResizeHandle(clickPoint, _selectedComponent.Bounds))
            {
                _dragStartPoint = clickPoint;
                _isResizing = true;
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
            _selectionRect = null;
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

    private bool IsPointOnResizeHandle(Point point, Rectangle bounds)
    {
        Rectangle handleRect = new Rectangle(
            bounds.Right - ResizeHandleSize,
            bounds.Bottom - ResizeHandleSize,
            ResizeHandleSize,
            ResizeHandleSize
        );
        return handleRect.Contains(point);
    }

    private void DrawResizeHandles(Graphics g, Rectangle bounds)
    {
        using (var brush = new SolidBrush(Color.Blue))
        {
            // Draw resize handle at bottom-right corner
            Rectangle handleRect = new Rectangle(
                bounds.Right - ResizeHandleSize,
                bounds.Bottom - ResizeHandleSize,
                ResizeHandleSize,
                ResizeHandleSize
            );
            g.FillRectangle(brush, handleRect);
            g.DrawRectangle(Pens.Black, handleRect);
        }
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
        if (_template != null)
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
                    _canvas.Invalidate();
                }
            }
            catch
            {
                // Fallback
                _canvas.Invalidate();
            }
        }
    }
}
