using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Designer.Modules.Components;

namespace Designer.Modules.Components;

/// <summary>
/// Components view displaying available components in a palette.
/// </summary>
public partial class ComponentsView : UserControl
{
    private ListBox _componentsList;
    private ComponentsModule? _componentsModule;

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
        var searchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Height = 25,
            PlaceholderText = "Search components..."
        };
        searchBox.TextChanged += (s, e) => FilterComponents(searchBox.Text);

        // Components list
        _componentsList = new ListBox
        {
            Dock = DockStyle.Fill,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 30
        };
        _componentsList.DrawItem += OnDrawComponentItem;
        _componentsList.MouseDown += OnComponentMouseDown;

        mainLayout.Controls.Add(searchBox, 0, 0);
        mainLayout.Controls.Add(_componentsList, 0, 1);
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
        LoadComponents();
    }

    /// <summary>
    /// Loads components into the list.
    /// </summary>
    private void LoadComponents()
    {
        _componentsList.Items.Clear();

        if (_componentsModule == null)
            return;

        var components = _componentsModule.GetAvailableComponents();
        foreach (var component in components)
        {
            _componentsList.Items.Add(component);
        }
    }

    /// <summary>
    /// Filters components by search text.
    /// </summary>
    private void FilterComponents(string searchText)
    {
        _componentsList.Items.Clear();

        if (_componentsModule == null)
            return;

        var components = _componentsModule.GetAvailableComponents();
        foreach (var component in components)
        {
            string componentName = GetComponentName(component);
            if (string.IsNullOrEmpty(searchText) || 
                componentName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                _componentsList.Items.Add(component);
            }
        }
    }

    /// <summary>
    /// Gets component name for display.
    /// </summary>
    private string GetComponentName(object component)
    {
        // TODO: Get name from component object
        return component.GetType().Name.Replace("Component", "");
    }

    /// <summary>
    /// Handles drawing component items.
    /// </summary>
    private void OnDrawComponentItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _componentsList.Items.Count)
            return;

        e.DrawBackground();

        var component = _componentsList.Items[e.Index];
        string componentName = GetComponentName(component);

        // Draw icon (placeholder)
        var iconRect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 2, 24, 24);
        e.Graphics.FillRectangle(Brushes.LightGray, iconRect);

        // Draw text
        var textRect = new Rectangle(e.Bounds.X + 30, e.Bounds.Y, e.Bounds.Width - 30, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, componentName, e.Font, textRect, e.ForeColor, 
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

        e.DrawFocusRectangle();
    }

    /// <summary>
    /// Data format name for drag-drop so drop target can accept any BaseComponent.
    /// </summary>
    public const string ComponentDragDropFormat = "AccuTrack.SCADA.Component";

    /// <summary>
    /// Handles mouse down for drag and drop.
    /// </summary>
    private void OnComponentMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _componentsList.SelectedItem != null)
        {
            var componentName = _componentsList.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(componentName) && _componentsModule != null)
            {
                var component = _componentsModule.CreateComponent(componentName);
                if (component != null)
                {
                    // Use custom format so drop target can retrieve as BaseComponent (WinForms stores by concrete type otherwise)
                    var data = new DataObject(ComponentDragDropFormat, component);
                    _componentsList.DoDragDrop(data, DragDropEffects.Copy);
                }
            }
        }
    }
}
