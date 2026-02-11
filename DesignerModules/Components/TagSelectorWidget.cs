using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.TagEngine;

namespace Designer.Modules.Components;

/// <summary>
/// Widget for selecting tags from available tag tables.
/// </summary>
public partial class TagSelectorWidget : UserControl
{
    private ComboBox _tagCombo;
    private List<TagTable> _tagTables = new List<TagTable>();
    
    public event EventHandler<string>? TagSelected;
    public event Action<object, string, string, bool>? TagSelectedWithDevice; // sender, tagName, deviceName, isRemote

    public TagSelectorWidget()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _tagCombo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        
        _tagCombo.SelectedIndexChanged += OnTagComboSelectedIndexChanged;
        
        Controls.Add(_tagCombo);
    }

    /// <summary>
    /// Sets available tag tables.
    /// </summary>
    public void SetAvailableTags(List<TagTable> tagTables)
    {
        _tagTables = tagTables ?? new List<TagTable>();
        PopulateTagCombo();
    }

    /// <summary>
    /// Gets the selected tag name.
    /// </summary>
    public string SelectedTagName()
    {
        if (_tagCombo.SelectedIndex <= 0)
            return string.Empty;
            
        var selectedItem = _tagCombo.SelectedItem;
        if (selectedItem is TagComboItem item)
        {
            return item.TagName;
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Sets the selected tag name.
    /// </summary>
    public void SetSelectedTagName(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            _tagCombo.SelectedIndex = 0;
            return;
        }

        for (int i = 1; i < _tagCombo.Items.Count; i++)
        {
            if (_tagCombo.Items[i] is TagComboItem item && item.TagName == tagName)
            {
                _tagCombo.SelectedIndex = i;
                return;
            }
        }
    }

    /// <summary>
    /// Gets the full tag identifier (for remote tags: "device::tagName", for local: "tagName").
    /// </summary>
    public string SelectedTagFullIdentifier()
    {
        if (_tagCombo.SelectedIndex <= 0)
            return string.Empty;
            
        var selectedItem = _tagCombo.SelectedItem;
        if (selectedItem is TagComboItem item)
        {
            if (!string.IsNullOrEmpty(item.DeviceName))
            {
                return $"{item.DeviceName}::{item.TagName}";
            }
            return item.TagName;
        }
        
        return string.Empty;
    }

    /// <summary>
    /// Sets the selected tag by full identifier.
    /// </summary>
    public void SetSelectedTagFullIdentifier(string fullIdentifier)
    {
        if (string.IsNullOrEmpty(fullIdentifier))
        {
            _tagCombo.SelectedIndex = 0;
            return;
        }

        string[] parts = fullIdentifier.Split(new[] { "::" }, StringSplitOptions.None);
        string tagName = parts.Length == 2 ? parts[1] : parts[0];
        string? deviceName = parts.Length == 2 ? parts[0] : null;

        for (int i = 1; i < _tagCombo.Items.Count; i++)
        {
            if (_tagCombo.Items[i] is TagComboItem item && 
                item.TagName == tagName && 
                item.DeviceName == deviceName)
            {
                _tagCombo.SelectedIndex = i;
                return;
            }
        }
    }

    private void PopulateTagCombo()
    {
        _tagCombo.Items.Clear();
        _tagCombo.Items.Add(""); // Empty option

        // Group tags by device/communication module
        var tagsByDevice = new Dictionary<string, List<Tag>>();
        var localTags = new List<Tag>();

        foreach (var table in _tagTables)
        {
            if (table == null)
                continue;

            foreach (var tag in table.GetTags())
            {
                if (tag == null)
                    continue;

                // Check if tag has a communication module (remote tag)
                if (!string.IsNullOrEmpty(tag.CommunicationModule))
                {
                    if (!tagsByDevice.ContainsKey(tag.CommunicationModule))
                    {
                        tagsByDevice[tag.CommunicationModule] = new List<Tag>();
                    }
                    tagsByDevice[tag.CommunicationModule].Add(tag);
                }
                else
                {
                    localTags.Add(tag);
                }
            }
        }

        // Add local tags first
        foreach (var tag in localTags)
        {
            var displayName = FormatTagDisplayName(tag);
            _tagCombo.Items.Add(new TagComboItem(tag.Name, null, displayName));
        }

        // Add separator if we have both local and remote tags
        if (localTags.Count > 0 && tagsByDevice.Count > 0)
        {
            _tagCombo.Items.Add(new TagComboItem("---", null, "---")); // Separator
        }

        // Add remote tags grouped by device
        foreach (var kvp in tagsByDevice)
        {
            string deviceName = kvp.Key;
            var deviceTags = kvp.Value;

            // Add device header (non-selectable)
            _tagCombo.Items.Add(new TagComboItem($"[{deviceName}]", null, $"[{deviceName}]", true));

            // Add tags for this device
            foreach (var tag in deviceTags)
            {
                var displayName = FormatTagDisplayName(tag, deviceName);
                _tagCombo.Items.Add(new TagComboItem(tag.Name, deviceName, displayName));
            }
        }
    }

    private string FormatTagDisplayName(Tag tag, string? deviceName = null)
    {
        if (!string.IsNullOrEmpty(deviceName))
        {
            return $"  {tag.Name} [{deviceName}]";
        }
        return tag.Name;
    }

    private void OnTagComboSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_tagCombo.SelectedIndex <= 0)
        {
            TagSelected?.Invoke(this, string.Empty);
            TagSelectedWithDevice?.Invoke(this, string.Empty, string.Empty, false);
            return;
        }

        var selectedItem = _tagCombo.SelectedItem;
        if (selectedItem is TagComboItem item && !item.IsHeader)
        {
            TagSelected?.Invoke(this, item.TagName);
            TagSelectedWithDevice?.Invoke(this, item.TagName, item.DeviceName ?? string.Empty, !string.IsNullOrEmpty(item.DeviceName));
        }
    }

    /// <summary>
    /// Helper class for combo box items.
    /// </summary>
    private class TagComboItem
    {
        public string TagName { get; }
        public string? DeviceName { get; }
        public string DisplayName { get; }
        public bool IsHeader { get; }

        public TagComboItem(string tagName, string? deviceName, string displayName, bool isHeader = false)
        {
            TagName = tagName;
            DeviceName = deviceName;
            DisplayName = displayName;
            IsHeader = isHeader;
        }

        public override string ToString() => DisplayName;
    }
}
