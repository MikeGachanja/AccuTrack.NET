using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.TagEngine;
// Removed using Designer.Modules.Project; to break circular dependency
// ScadaProject will be handled via object/dynamic

namespace Designer.Modules.Components;

/// <summary>
/// Property editor for editing component properties.
/// </summary>
public partial class PropertyEditor : UserControl
{
    private PropertyGrid _propertyGrid;
    private List<TagTable> _availableTagTables = new List<TagTable>();
    private List<object> _availableScreens = new List<object>(); // ScreenTemplate objects
    private object? _scadaProject; // Changed from ScadaProject to object to break circular dependency
    private object? _selectedItem;

    public event EventHandler? RequestAutoSave;

    public PropertyEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _propertyGrid = new PropertyGrid
        {
            Dock = DockStyle.Fill,
            ToolbarVisible = true,
            HelpVisible = true
        };

        Controls.Add(_propertyGrid);
    }

    /// <summary>
    /// Updates the editor with the selected item.
    /// </summary>
    public void UpdateEditor(object? item)
    {
        _selectedItem = item;
        
        if (item == null)
        {
            _propertyGrid.SelectedObject = null;
            return;
        }

        // Create a wrapper object that exposes properties for editing
        var wrapper = CreatePropertyWrapper(item);
        _propertyGrid.SelectedObject = wrapper;
    }

    /// <summary>
    /// Sets available tag tables for tag binding properties.
    /// </summary>
    public void SetAvailableTagTables(List<TagTable> tagTables)
    {
        _availableTagTables = tagTables ?? new List<TagTable>();
    }

    /// <summary>
    /// Sets available screens for screen navigation properties.
    /// </summary>
    public void SetAvailableScreens(List<object> screens)
    {
        _availableScreens = screens ?? new List<object>();
    }

    /// <summary>
    /// Sets the SCADA project for context.
    /// </summary>
    public void SetScadaProject(object? project)
    {
        _scadaProject = project;
    }

    /// <summary>
    /// Creates a property wrapper for the selected item.
    /// </summary>
    private object CreatePropertyWrapper(object item)
    {
        // For now, return the item directly
        // TODO: Create a proper wrapper class that handles:
        // - Tag binding properties (dropdown with available tags)
        // - Screen navigation properties (dropdown with available screens)
        // - Color properties (color picker)
        // - Other complex property types
        
        return item;
    }

    /// <summary>
    /// Gets all available tags from tag tables.
    /// </summary>
    public List<Tag> GetAvailableTags()
    {
        var tags = new List<Tag>();
        foreach (var table in _availableTagTables)
        {
            tags.AddRange(table.GetTags());
        }
        return tags;
    }

    /// <summary>
    /// Gets tag names for autocomplete.
    /// </summary>
    public List<string> GetAvailableTagNames()
    {
        return GetAvailableTags().Select(t => t.Name).ToList();
    }
}
