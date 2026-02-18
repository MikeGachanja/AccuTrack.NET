using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.TagEngine;

namespace Designer.Modules.Components;

/// <summary>
/// Dialog for selecting tags with search, filtering, and tag table navigation.
/// Can be used throughout the project for tag selection in historian, scripts, schedules, alarms, ML, etc.
/// When SetAllowedTagNames is used (e.g. historian tags), only those tags are shown for ML input/output.
/// </summary>
public partial class TagSelectorDialog : Form
{
    private TreeView _tagTablesTree;
    private ListView _tagsList;
    private TextBox _searchBox;
    private ComboBox _dataTypeFilter;
    private Label _selectedTagInfo;
    private Label? _filterInfoLabel;
    private Button _okButton;
    private Button _cancelButton;
    private List<TagTable> _tagTables = new List<TagTable>();
    private List<Tag> _allTags = new List<Tag>(); // Store all tags for filtering
    private Tag? _selectedTag;
    private string? _requiredDataType; // For filtering incompatible tags
    private bool _allowMultiProject = false; // Future: multi-project support
    private HashSet<string>? _allowedTagNames; // When set (e.g. historian tags), only these tags are shown

    /// <summary>
    /// Gets the selected tag.
    /// </summary>
    public Tag? SelectedTag => _selectedTag;

    /// <summary>
    /// Gets the selected tag name.
    /// </summary>
    public string SelectedTagName => _selectedTag?.Name ?? string.Empty;

    /// <summary>
    /// Gets the selected tag table name.
    /// </summary>
    public string SelectedTagTableName { get; private set; } = string.Empty;

    public TagSelectorDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the dialog with available tag tables.
    /// </summary>
    public void SetTagTables(IEnumerable<TagTable> tagTables)
    {
        _tagTables = tagTables?.ToList() ?? new List<TagTable>();
        PopulateTagTables();
    }

    /// <summary>
    /// Sets the required data type for filtering incompatible tags.
    /// </summary>
    public void SetRequiredDataType(string? dataType)
    {
        _requiredDataType = dataType;
        UpdateDataTypeFilter();
        FilterTags();
    }

    /// <summary>
    /// When set, only tags whose names are in this list are shown (e.g. tags configured for Historian).
    /// Use for ML editor so models are fed from historian-configured tags only.
    /// Pass null or empty to show all tags.
    /// </summary>
    public void SetAllowedTagNames(IReadOnlyList<string>? tagNames)
    {
        _allowedTagNames = tagNames != null && tagNames.Count > 0
            ? new HashSet<string>(tagNames, StringComparer.OrdinalIgnoreCase)
            : null;
        if (_filterInfoLabel != null)
        {
            _filterInfoLabel.Visible = _allowedTagNames != null;
            _filterInfoLabel.Text = _allowedTagNames != null
                ? "Showing tags configured for Historian (ML)"
                : "";
        }
        if (_tagTablesTree.SelectedNode?.Tag is TagTable table)
            PopulateTagsList(table);
    }

    /// <summary>
    /// Sets the initially selected tag name.
    /// </summary>
    public void SetSelectedTagName(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return;

        // Find and select the tag in the tree/list
        foreach (TreeNode tableNode in _tagTablesTree.Nodes)
        {
            var table = tableNode.Tag as TagTable;
            if (table == null) continue;

            var tag = table.FindTag(tagName);
            if (tag != null)
            {
                _tagTablesTree.SelectedNode = tableNode;
                tableNode.Expand();
                SelectTagInList(tag);
                break;
            }
        }
    }

    private void InitializeComponent()
    {
        Text = "Select Tag";
        Size = new System.Drawing.Size(800, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(10)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

        // Search box (spans both columns)
        _searchBox = new TextBox
        {
            Dock = DockStyle.Fill
        };
        // Set placeholder text using native Windows API or just use default text
        _searchBox.Text = "Search tags...";
        _searchBox.ForeColor = System.Drawing.Color.Gray;
        _searchBox.Enter += (s, e) =>
        {
            if (_searchBox.Text == "Search tags...")
            {
                _searchBox.Text = "";
                _searchBox.ForeColor = System.Drawing.Color.Black;
            }
        };
        _searchBox.Leave += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_searchBox.Text))
            {
                _searchBox.Text = "Search tags...";
                _searchBox.ForeColor = System.Drawing.Color.Gray;
            }
        };
        _searchBox.TextChanged += (s, e) =>
        {
            if (_searchBox.Text != "Search tags...")
                FilterTags();
        };
        mainLayout.Controls.Add(_searchBox, 0, 0);
        mainLayout.SetColumnSpan(_searchBox, 2);

        _filterInfoLabel = new Label
        {
            Text = "",
            AutoSize = true,
            ForeColor = System.Drawing.Color.DarkSlateGray,
            Visible = false
        };

        // Data type filter (spans both columns)
        var filterLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };
        filterLayout.Controls.Add(new Label { Text = "Filter by Data Type:", AutoSize = true, Anchor = AnchorStyles.Left });
        _dataTypeFilter = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _dataTypeFilter.SelectedIndexChanged += (s, e) => FilterTags();
        filterLayout.Controls.Add(_dataTypeFilter);
        if (_filterInfoLabel != null)
            filterLayout.Controls.Add(_filterInfoLabel);
        mainLayout.Controls.Add(filterLayout, 0, 1);
        mainLayout.SetColumnSpan(filterLayout, 2);

        // Tag tables tree (left)
        var tablesGroup = new GroupBox
        {
            Text = "Tag Tables",
            Dock = DockStyle.Fill
        };
        _tagTablesTree = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false
        };
        _tagTablesTree.AfterSelect += OnTagTableSelected;
        tablesGroup.Controls.Add(_tagTablesTree);
        mainLayout.Controls.Add(tablesGroup, 0, 2);

        // Tags list (right)
        var tagsGroup = new GroupBox
        {
            Text = "Tags",
            Dock = DockStyle.Fill
        };
        _tagsList = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false
        };
        _tagsList.Columns.Add("Name", 150);
        _tagsList.Columns.Add("Data Type", 100);
        _tagsList.Columns.Add("Address", 120);
        _tagsList.Columns.Add("Description", 200);
        _tagsList.SelectedIndexChanged += OnTagSelected;
        _tagsList.DoubleClick += (s, e) => { if (_selectedTag != null) { DialogResult = DialogResult.OK; Close(); } };
        tagsGroup.Controls.Add(_tagsList);
        mainLayout.Controls.Add(tagsGroup, 1, 2);

        // Selected tag info (spans both columns)
        _selectedTagInfo = new Label
        {
            Dock = DockStyle.Fill,
            Text = "No tag selected",
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        mainLayout.Controls.Add(_selectedTagInfo, 0, 3);
        mainLayout.SetColumnSpan(_selectedTagInfo, 2);

        // Buttons
        var buttonLayout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        _okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Width = 75,
            Enabled = false
        };
        _okButton.Click += (s, e) => Close();
        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Width = 75
        };
        _cancelButton.Click += (s, e) => Close();
        buttonLayout.Controls.Add(_cancelButton);
        buttonLayout.Controls.Add(_okButton);

        var buttonPanel = new Panel { Dock = DockStyle.Fill };
        buttonPanel.Controls.Add(buttonLayout);
        mainLayout.Controls.Add(buttonPanel, 0, 3);
        mainLayout.SetColumnSpan(buttonPanel, 2);

        Controls.Add(mainLayout);

        // Initialize data type filter
        UpdateDataTypeFilter();
    }

    private void UpdateDataTypeFilter()
    {
        _dataTypeFilter.Items.Clear();
        _dataTypeFilter.Items.Add("All Types");
        
        // Add common data types
        var dataTypes = new[] { "Bit", "Byte", "Word", "DWord", "Int8", "UInt8", "Int16", "UInt16", 
            "Int32", "UInt32", "Float32", "Float64", "String8", "String16", "Real", "Boolean" };
        _dataTypeFilter.Items.AddRange(dataTypes);
        
        _dataTypeFilter.SelectedIndex = 0;
        
        // If required data type is set, select it
        if (!string.IsNullOrEmpty(_requiredDataType))
        {
            for (int i = 0; i < _dataTypeFilter.Items.Count; i++)
            {
                if (_dataTypeFilter.Items[i].ToString() == _requiredDataType)
                {
                    _dataTypeFilter.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void PopulateTagTables()
    {
        _tagTablesTree.Nodes.Clear();
        
        foreach (var table in _tagTables)
        {
            if (table == null) continue;
            
            var node = new TreeNode(table.Name)
            {
                Tag = table,
                ImageIndex = 0,
                SelectedImageIndex = 0
            };
            _tagTablesTree.Nodes.Add(node);
        }
        
        if (_tagTablesTree.Nodes.Count > 0)
        {
            _tagTablesTree.SelectedNode = _tagTablesTree.Nodes[0];
        }
    }

    private void OnTagTableSelected(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is TagTable table)
        {
            PopulateTagsList(table);
        }
    }

    private void PopulateTagsList(TagTable table)
    {
        _tagsList.Items.Clear();
        SelectedTagTableName = table.Name;
        _allTags.Clear();

        var tags = table.GetTags();
        foreach (var tag in tags)
        {
            if (tag == null) continue;
            if (_allowedTagNames != null && !_allowedTagNames.Contains(tag.Name))
                continue;
            _allTags.Add(tag);
            var item = new ListViewItem(tag.Name)
            {
                Tag = tag,
                SubItems = {
                    tag.DataType ?? "Unknown",
                    tag.Address ?? "",
                    tag.Description ?? ""
                }
            };
            _tagsList.Items.Add(item);
        }

        FilterTags();
    }

    private void FilterTags()
    {
        var searchText = _searchBox.Text.ToLowerInvariant();
        if (searchText == "search tags...")
            searchText = string.Empty;
        
        var selectedDataType = _dataTypeFilter.SelectedItem?.ToString() ?? "All Types";
        
        // Clear and rebuild list based on filters
        _tagsList.Items.Clear();
        
        foreach (var tag in _allTags)
        {
            if (tag == null) continue;

            bool matchesSearch = string.IsNullOrEmpty(searchText) || 
                                tag.Name.ToLowerInvariant().Contains(searchText) ||
                                (tag.Description?.ToLowerInvariant().Contains(searchText) ?? false);

            bool matchesDataType = selectedDataType == "All Types" || 
                                  tag.DataType == selectedDataType;

            // Check data type compatibility if required data type is set
            bool isCompatible = true;
            if (!string.IsNullOrEmpty(_requiredDataType))
            {
                isCompatible = IsDataTypeCompatible(tag.DataType, _requiredDataType);
            }

            if (matchesSearch && matchesDataType && isCompatible)
            {
                var item = new ListViewItem(tag.Name)
                {
                    Tag = tag,
                    SubItems = {
                        tag.DataType ?? "Unknown",
                        tag.Address ?? "",
                        tag.Description ?? ""
                    }
                };
                _tagsList.Items.Add(item);
            }
        }
    }

    private bool IsDataTypeCompatible(string? tagDataType, string requiredDataType)
    {
        if (string.IsNullOrEmpty(tagDataType))
            return false;

        // Normalize data types
        tagDataType = tagDataType.Trim();
        requiredDataType = requiredDataType.Trim();

        // Exact match
        if (tagDataType.Equals(requiredDataType, StringComparison.OrdinalIgnoreCase))
            return true;

        // Compatibility rules
        var compatibilityMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // Real/Float types can accept Float32, Float64, Real
            ["Real"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Float32", "Float64", "Real", "Float" },
            ["Float32"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Float32", "Real", "Float" },
            ["Float64"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Float64", "Real", "Float", "Double" },
            
            // Integer types
            ["Int32"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Int32", "Int16", "Int8", "DWord", "Word" },
            ["Int16"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Int16", "Int8", "Word" },
            ["Int8"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Int8", "Byte" },
            
            // Unsigned integer types
            ["UInt32"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UInt32", "UInt16", "UInt8", "DWord", "Word", "Byte" },
            ["UInt16"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UInt16", "UInt8", "Word", "Byte" },
            ["UInt8"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "UInt8", "Byte" },
            
            // Boolean/Bit
            ["Boolean"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Boolean", "Bit", "Bool" },
            ["Bit"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Bit", "Boolean", "Bool" },
            
            // String types
            ["String"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "String8", "String16", "String" },
            ["String8"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "String8", "String" },
            ["String16"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "String16", "String" },
        };

        // Check if required type has compatibility rules
        if (compatibilityMap.TryGetValue(requiredDataType, out var compatibleTypes))
        {
            return compatibleTypes.Contains(tagDataType);
        }

        // Default: no compatibility (strict matching)
        return false;
    }

    private void OnTagSelected(object? sender, EventArgs e)
    {
        if (_tagsList.SelectedItems.Count == 0)
        {
            _selectedTag = null;
            _okButton.Enabled = false;
            _selectedTagInfo.Text = "No tag selected";
            return;
        }

        var selectedItem = _tagsList.SelectedItems[0];
        if (selectedItem.Tag is Tag tag)
        {
            _selectedTag = tag;
            _okButton.Enabled = true;
            _selectedTagInfo.Text = $"Selected: {tag.Name} ({tag.DataType}) - {tag.Description}";
        }
    }

    private void SelectTagInList(Tag tag)
    {
        foreach (ListViewItem item in _tagsList.Items)
        {
            if (item.Tag == tag)
            {
                item.Selected = true;
                item.EnsureVisible();
                OnTagSelected(null, EventArgs.Empty);
                break;
            }
        }
    }
}
