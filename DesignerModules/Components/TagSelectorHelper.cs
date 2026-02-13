using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.TagEngine;

namespace Designer.Modules.Components;

/// <summary>
/// Helper class for showing the tag selector dialog throughout the project.
/// Provides a consistent interface for tag selection in historian, scripts, schedules, alarms, ML, etc.
/// </summary>
public static class TagSelectorHelper
{
    /// <summary>
    /// Shows the tag selector dialog and returns the selected tag name.
    /// </summary>
    /// <param name="parent">Parent form for the dialog</param>
    /// <param name="tagTables">Available tag tables</param>
    /// <param name="currentTagName">Currently selected tag name (optional)</param>
    /// <param name="requiredDataType">Required data type for filtering (optional)</param>
    /// <returns>Selected tag name, or empty string if cancelled</returns>
    public static string ShowTagSelector(
        Form parent,
        IEnumerable<TagTable> tagTables,
        string? currentTagName = null,
        string? requiredDataType = null)
    {
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(tagTables);
        
        if (!string.IsNullOrEmpty(currentTagName))
        {
            dialog.SetSelectedTagName(currentTagName);
        }
        
        if (!string.IsNullOrEmpty(requiredDataType))
        {
            dialog.SetRequiredDataType(requiredDataType);
        }
        
        if (dialog.ShowDialog(parent) == DialogResult.OK)
        {
            return dialog.SelectedTagName;
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Shows the tag selector dialog and returns the selected tag object.
    /// </summary>
    /// <param name="parent">Parent form for the dialog</param>
    /// <param name="tagTables">Available tag tables</param>
    /// <param name="currentTagName">Currently selected tag name (optional)</param>
    /// <param name="requiredDataType">Required data type for filtering (optional)</param>
    /// <returns>Selected tag, or null if cancelled</returns>
    public static Tag? ShowTagSelectorWithTag(
        Form parent,
        IEnumerable<TagTable> tagTables,
        string? currentTagName = null,
        string? requiredDataType = null)
    {
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(tagTables);
        
        if (!string.IsNullOrEmpty(currentTagName))
        {
            dialog.SetSelectedTagName(currentTagName);
        }
        
        if (!string.IsNullOrEmpty(requiredDataType))
        {
            dialog.SetRequiredDataType(requiredDataType);
        }
        
        if (dialog.ShowDialog(parent) == DialogResult.OK)
        {
            return dialog.SelectedTag;
        }
        
        return null;
    }

    /// <summary>
    /// Shows the tag selector dialog and returns both tag name and table name.
    /// </summary>
    /// <param name="parent">Parent form for the dialog</param>
    /// <param name="tagTables">Available tag tables</param>
    /// <param name="currentTagName">Currently selected tag name (optional)</param>
    /// <param name="requiredDataType">Required data type for filtering (optional)</param>
    /// <returns>Tuple with (tagName, tagTableName), or (empty, empty) if cancelled</returns>
    public static (string tagName, string tagTableName) ShowTagSelectorWithTable(
        Form parent,
        IEnumerable<TagTable> tagTables,
        string? currentTagName = null,
        string? requiredDataType = null)
    {
        using var dialog = new TagSelectorDialog();
        dialog.SetTagTables(tagTables);
        
        if (!string.IsNullOrEmpty(currentTagName))
        {
            dialog.SetSelectedTagName(currentTagName);
        }
        
        if (!string.IsNullOrEmpty(requiredDataType))
        {
            dialog.SetRequiredDataType(requiredDataType);
        }
        
        if (dialog.ShowDialog(parent) == DialogResult.OK)
        {
            return (dialog.SelectedTagName, dialog.SelectedTagTableName);
        }
        
        return (string.Empty, string.Empty);
    }
}
