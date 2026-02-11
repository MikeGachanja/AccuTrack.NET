using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.TagEngine;
using Designer.Modules.Events;
// Removed using Designer.Modules.Project; to break circular dependency
// Removed using Designer.Modules.ScreenEditor; to break circular dependency
// ScadaProject and ScreenTemplate will be handled via object/dynamic

namespace Designer.Modules.Components;

/// <summary>
/// Property editor for editing component properties with tabs.
/// </summary>
public partial class PropertyEditor : UserControl
{
    private TabControl _tabControl;
    private Panel _generalTab;
    private Panel _eventsTab;
    private Panel _animationTab;
    private Panel _tagsTab;
    
    private List<TagTable> _availableTagTables = new List<TagTable>();
    private List<object> _availableScreens = new List<object>(); // ScreenTemplate objects
    private object? _scadaProject; // Changed from ScadaProject to object to break circular dependency
    private BaseComponent? _selectedComponent;
    private EventsModule? _eventsModule;
    
    // Events tab controls
    private ListBox _eventsList;
    private Button _addEventButton;
    private Button _removeEventButton;
    private Button _editEventButton;
    private Button _saveEventButton;
    private ComboBox _eventCategoryCombo;
    private ComboBox _eventActionCombo;
    private ComboBox _eventTriggerCombo;
    private Panel _eventParamsPanel;
    private ScadaEvent? _currentEditingEvent;
    
    // Event parameter controls (stored for access)
    private ComboBox? _screenCombo;
    private TagSelectorWidget? _tagSelector;
    private TextBox? _valueTextBox;
    private TextBox? _scriptPathTextBox;
    private TextBox? _argsTextBox;

    public event EventHandler? RequestAutoSave;

    public PropertyEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // Create tabs
        _generalTab = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _eventsTab = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _animationTab = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _tagsTab = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

        _tabControl.TabPages.Add("General");
        _tabControl.TabPages.Add("Events");
        _tabControl.TabPages.Add("Animation");
        _tabControl.TabPages.Add("Tags");

        _tabControl.TabPages[0].Controls.Add(_generalTab);
        _tabControl.TabPages[1].Controls.Add(_eventsTab);
        _tabControl.TabPages[2].Controls.Add(_animationTab);
        _tabControl.TabPages[3].Controls.Add(_tagsTab);

        SetupEventsTab();
        
        Controls.Add(_tabControl);
    }

    /// <summary>
    /// Updates the editor with the selected item.
    /// </summary>
    public void UpdateEditor(object? item)
    {
        _selectedComponent = item as BaseComponent;
        
        if (_selectedComponent == null)
        {
            ClearAllTabs();
            return;
        }

        UpdateGeneralTab();
        UpdateEventsTab();
        UpdateTagsTab();
        // Animation tab can be implemented later
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
        
        // Initialize events module
        _eventsModule = new EventsModule();
        _eventsModule.SetScadaProject(project);
        
        // Ensure events.json path is set correctly
        // EventsModule will determine path from project, but we can verify it's set
        var eventsPath = _eventsModule.GetEventsJsonPath();
        if (string.IsNullOrEmpty(eventsPath) && project != null)
        {
            try
            {
                dynamic projectObj = project;
                var paths = projectObj.Paths;
                if (paths != null)
                {
                    // ScadaPaths has RootPath, construct json path from it
                    var rootPath = paths.RootPath?.ToString();
                    if (!string.IsNullOrEmpty(rootPath))
                    {
                        _eventsModule.SetEventsJsonPath(Path.Combine(rootPath, "json", "events.json"));
                    }
                }
            }
            catch
            {
                // Fallback handled by EventsModule
            }
        }
    }

    private void ClearAllTabs()
    {
        _generalTab.Controls.Clear();
        _eventsTab.Controls.Clear();
        _tagsTab.Controls.Clear();
    }

    private void UpdateGeneralTab()
    {
        _generalTab.Controls.Clear();
        
        if (_selectedComponent == null)
            return;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(5)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row = 0;

        // Name
        layout.Controls.Add(new Label { Text = "Name:", AutoSize = true }, 0, row);
        var nameTextBox = new TextBox { Text = _selectedComponent.Name, Dock = DockStyle.Fill };
        nameTextBox.TextChanged += (s, e) =>
        {
            _selectedComponent.Name = nameTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(nameTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Location
        layout.Controls.Add(new Label { Text = "Location X:", AutoSize = true }, 0, row);
        var xNumeric = new NumericUpDown { Minimum = int.MinValue, Maximum = int.MaxValue, Value = _selectedComponent.Location.X, Width = 100 };
        xNumeric.ValueChanged += (s, e) =>
        {
            _selectedComponent.Location = new Point((int)xNumeric.Value, _selectedComponent.Location.Y);
            TriggerAutoSave();
        };
        layout.Controls.Add(xNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        layout.Controls.Add(new Label { Text = "Location Y:", AutoSize = true }, 0, row);
        var yNumeric = new NumericUpDown { Minimum = int.MinValue, Maximum = int.MaxValue, Value = _selectedComponent.Location.Y, Width = 100 };
        yNumeric.ValueChanged += (s, e) =>
        {
            _selectedComponent.Location = new Point(_selectedComponent.Location.X, (int)yNumeric.Value);
            TriggerAutoSave();
        };
        layout.Controls.Add(yNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Size
        layout.Controls.Add(new Label { Text = "Width:", AutoSize = true }, 0, row);
        var widthNumeric = new NumericUpDown { Minimum = 1, Maximum = int.MaxValue, Value = _selectedComponent.Size.Width, Width = 100 };
        widthNumeric.ValueChanged += (s, e) =>
        {
            _selectedComponent.Size = new Size((int)widthNumeric.Value, _selectedComponent.Size.Height);
            TriggerAutoSave();
        };
        layout.Controls.Add(widthNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        layout.Controls.Add(new Label { Text = "Height:", AutoSize = true }, 0, row);
        var heightNumeric = new NumericUpDown { Minimum = 1, Maximum = int.MaxValue, Value = _selectedComponent.Size.Height, Width = 100 };
        heightNumeric.ValueChanged += (s, e) =>
        {
            _selectedComponent.Size = new Size(_selectedComponent.Size.Width, (int)heightNumeric.Value);
            TriggerAutoSave();
        };
        layout.Controls.Add(heightNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Visibility
        var visibleCheckBox = new CheckBox { Text = "Visible", Checked = _selectedComponent.Visible };
        visibleCheckBox.CheckedChanged += (s, e) =>
        {
            _selectedComponent.Visible = visibleCheckBox.Checked;
            TriggerAutoSave();
        };
        layout.Controls.Add(visibleCheckBox, 0, row);
        layout.SetColumnSpan(visibleCheckBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Enabled
        var enabledCheckBox = new CheckBox { Text = "Enabled", Checked = _selectedComponent.Enabled };
        enabledCheckBox.CheckedChanged += (s, e) =>
        {
            _selectedComponent.Enabled = enabledCheckBox.Checked;
            TriggerAutoSave();
        };
        layout.Controls.Add(enabledCheckBox, 0, row);
        layout.SetColumnSpan(enabledCheckBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Component-specific properties
        if (_selectedComponent is ButtonComponent button)
        {
            UpdateButtonProperties(layout, button, ref row);
        }
        // Add other component types as needed

        _generalTab.Controls.Add(layout);
    }

    private void UpdateButtonProperties(TableLayoutPanel layout, ButtonComponent button, ref int row)
    {
        // Text
        layout.Controls.Add(new Label { Text = "Text:", AutoSize = true }, 0, row);
        var textTextBox = new TextBox { Text = button.Text, Dock = DockStyle.Fill };
        textTextBox.TextChanged += (s, e) =>
        {
            button.Text = textTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(textTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // BackColor
        layout.Controls.Add(new Label { Text = "Back Color:", AutoSize = true }, 0, row);
        var backColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(backColorButton, button.BackColor);
        backColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = button.BackColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    button.BackColor = colorDialog.Color;
                    UpdateColorButton(backColorButton, button.BackColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(backColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // ForeColor
        layout.Controls.Add(new Label { Text = "Fore Color:", AutoSize = true }, 0, row);
        var foreColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(foreColorButton, button.ForeColor);
        foreColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = button.ForeColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    button.ForeColor = colorDialog.Color;
                    UpdateColorButton(foreColorButton, button.ForeColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(foreColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateColorButton(Button button, Color color)
    {
        button.BackColor = color;
        button.ForeColor = GetContrastColor(color);
    }

    private Color GetContrastColor(Color color)
    {
        // Calculate relative luminance
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
        return luminance > 0.5 ? Color.Black : Color.White;
    }

    private void SetupEventsTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(5)
        };

        // Events list
        var listLabel = new Label { Text = "Events:", AutoSize = true };
        layout.Controls.Add(listLabel, 0, 0);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _eventsList = new ListBox { Dock = DockStyle.Fill, Height = 150 };
        _eventsList.SelectedIndexChanged += OnEventSelected;
        layout.Controls.Add(_eventsList, 0, 1);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

        // Buttons
        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Height = 30 };
        _addEventButton = new Button { Text = "Add Event", Width = 80 };
        _addEventButton.Click += OnAddEvent;
        _removeEventButton = new Button { Text = "Remove", Width = 80 };
        _removeEventButton.Click += OnRemoveEvent;
        _editEventButton = new Button { Text = "Edit", Width = 80 };
        _editEventButton.Click += OnEditEvent;
        _saveEventButton = new Button { Text = "Save", Width = 80, Enabled = false };
        _saveEventButton.Click += OnSaveEvent;
        buttonPanel.Controls.Add(_addEventButton);
        buttonPanel.Controls.Add(_removeEventButton);
        buttonPanel.Controls.Add(_editEventButton);
        buttonPanel.Controls.Add(_saveEventButton);
        layout.Controls.Add(buttonPanel, 0, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

        // Event configuration
        var configGroup = new GroupBox { Text = "Event Configuration", Dock = DockStyle.Fill };
        var configLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(5) };
        configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int configRow = 0;

        // Category
        configLayout.Controls.Add(new Label { Text = "Category:", AutoSize = true }, 0, configRow);
        _eventCategoryCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _eventCategoryCombo.Items.AddRange(new[] { "Screen Navigation", "Tag Operations", "Script Actions", "Component Control", "System Actions", "Data Operations", "Security" });
        _eventCategoryCombo.SelectedIndexChanged += OnCategoryChanged;
        configLayout.Controls.Add(_eventCategoryCombo, 1, configRow);
        configLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        configRow++;

        // Action
        configLayout.Controls.Add(new Label { Text = "Action:", AutoSize = true }, 0, configRow);
        _eventActionCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        configLayout.Controls.Add(_eventActionCombo, 1, configRow);
        configLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        configRow++;

        // Trigger
        configLayout.Controls.Add(new Label { Text = "Trigger:", AutoSize = true }, 0, configRow);
        _eventTriggerCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _eventTriggerCombo.Items.AddRange(new[] { "OnClick", "OnDoubleClick", "OnRightClick", "OnMouseDown", "OnMouseUp", "OnMouseEnter", "OnMouseLeave", "OnKeyPress", "OnValueChange", "OnStateChange", "OnFocusIn", "OnFocusOut", "OnTimer", "OnTagChange", "OnCondition" });
        configLayout.Controls.Add(_eventTriggerCombo, 1, configRow);
        configLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        configRow++;

        // Parameters panel (will be populated based on category)
        _eventParamsPanel = new Panel { Dock = DockStyle.Fill, Height = 200 };
        configLayout.Controls.Add(_eventParamsPanel, 0, configRow);
        configLayout.SetColumnSpan(_eventParamsPanel, 2);
        configLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        configGroup.Controls.Add(configLayout);
        layout.Controls.Add(configGroup, 0, 3);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));

        _eventsTab.Controls.Add(layout);
        
        // Initialize category combo
        _eventCategoryCombo.SelectedIndex = 0;
        OnCategoryChanged(null, EventArgs.Empty);
    }

    private void OnCategoryChanged(object? sender, EventArgs e)
    {
        _eventActionCombo.Items.Clear();
        
        int categoryIndex = _eventCategoryCombo.SelectedIndex;
        switch (categoryIndex)
        {
            case 0: // Screen Navigation
                _eventActionCombo.Items.AddRange(new[] { "Open Screen", "Close Screen", "Switch to Screen", "Previous Screen", "Next Screen", "Show Dialog", "Close Dialog" });
                break;
            case 1: // Tag Operations
                _eventActionCombo.Items.AddRange(new[] { "Set Bit", "Reset Bit", "Toggle Bit", "Write Value", "Increment Value", "Decrement Value", "Copy Tag Value", "Swap Tag Values" });
                break;
            case 2: // Script Actions
                _eventActionCombo.Items.AddRange(new[] { "Run Script", "Stop Script", "Pause Script", "Resume Script", "Execute Function" });
                break;
            case 3: // Component Control
                _eventActionCombo.Items.AddRange(new[] { "Show Component", "Hide Component", "Enable Component", "Disable Component", "Move Component", "Resize Component", "Change Style", "Start Animation", "Stop Animation" });
                break;
            case 4: // System Actions
                _eventActionCombo.Items.AddRange(new[] { "Start Process", "Stop Process", "Restart Application", "Log Event", "Clear Logs", "System Backup", "System Restore", "Print Screen" });
                break;
            case 5: // Data Operations
                _eventActionCombo.Items.AddRange(new[] { "Import Data", "Export Data", "Clear Data", "Save Settings", "Load Settings", "Reset to Default", "Backup Data", "Restore Data" });
                break;
            case 6: // Security
                _eventActionCombo.Items.AddRange(new[] { "Login", "Logout", "Change User", "Change Password", "Lock Screen", "Unlock Screen", "Enable Security", "Disable Security" });
                break;
        }
        
        if (_eventActionCombo.Items.Count > 0)
            _eventActionCombo.SelectedIndex = 0;
            
        UpdateEventParamsPanel();
    }

    private void UpdateEventParamsPanel()
    {
        _eventParamsPanel.Controls.Clear();
        
        if (_selectedComponent == null || _eventsModule == null)
            return;

        int categoryIndex = _eventCategoryCombo.SelectedIndex;
        var paramsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(5) };
        paramsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        paramsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int paramRow = 0;

        // Clear previous controls
        _screenCombo = null;
        _tagSelector = null;
        _valueTextBox = null;
        _scriptPathTextBox = null;
        _argsTextBox = null;

        switch (categoryIndex)
        {
            case 0: // Screen Navigation
                paramsLayout.Controls.Add(new Label { Text = "Screen:", AutoSize = true }, 0, paramRow);
                _screenCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                _screenCombo.Items.Add("Select Screen...");
                foreach (var screen in _availableScreens)
                {
                    try
                    {
                        dynamic screenObj = screen;
                        string screenName = screenObj.Name?.ToString() ?? "";
                        string screenId = screenObj.Id?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(screenName))
                        {
                            _screenCombo.Items.Add(new ScreenComboItem(screenName, screenId));
                        }
                    }
                    catch { }
                }
                paramsLayout.Controls.Add(_screenCombo, 1, paramRow);
                paramsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                paramRow++;
                break;

            case 1: // Tag Operations
                paramsLayout.Controls.Add(new Label { Text = "Tag:", AutoSize = true }, 0, paramRow);
                _tagSelector = new TagSelectorWidget { Dock = DockStyle.Fill, Height = 25 };
                _tagSelector.SetAvailableTags(_availableTagTables);
                paramsLayout.Controls.Add(_tagSelector, 1, paramRow);
                paramsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                paramRow++;

                paramsLayout.Controls.Add(new Label { Text = "Value:", AutoSize = true }, 0, paramRow);
                _valueTextBox = new TextBox { Dock = DockStyle.Fill };
                paramsLayout.Controls.Add(_valueTextBox, 1, paramRow);
                paramsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                paramRow++;
                break;

            case 2: // Script Actions
                paramsLayout.Controls.Add(new Label { Text = "Script Path:", AutoSize = true }, 0, paramRow);
                var scriptPathLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
                _scriptPathTextBox = new TextBox { Width = 300 };
                var browseButton = new Button { Text = "Browse...", Width = 80 };
                browseButton.Click += (s, e) =>
                {
                    using (var dialog = new OpenFileDialog { Filter = "Script Files|*.js;*.lua;*.py|All Files|*.*" })
                    {
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            _scriptPathTextBox!.Text = dialog.FileName;
                        }
                    }
                };
                scriptPathLayout.Controls.Add(_scriptPathTextBox);
                scriptPathLayout.Controls.Add(browseButton);
                paramsLayout.Controls.Add(scriptPathLayout, 1, paramRow);
                paramsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                paramRow++;

                paramsLayout.Controls.Add(new Label { Text = "Arguments:", AutoSize = true }, 0, paramRow);
                _argsTextBox = new TextBox { Dock = DockStyle.Fill };
                paramsLayout.Controls.Add(_argsTextBox, 1, paramRow);
                paramsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                paramRow++;
                break;
        }

        _eventParamsPanel.Controls.Add(paramsLayout);
    }

    private void UpdateEventsTab()
    {
        // Reset event configuration panel when switching components
        ResetEventConfigurationPanel();
        
        if (_selectedComponent == null || _eventsModule == null)
        {
            _eventsList.Items.Clear();
            return;
        }

        _eventsList.Items.Clear();
        var componentId = _selectedComponent.Id.ToString();
        var events = _eventsModule.GetEventsForComponent(componentId);
        
        foreach (var evt in events)
        {
            _eventsList.Items.Add(new EventListItem(evt));
        }
    }
    
    private void ResetEventConfigurationPanel()
    {
        // Clear current editing event
        _currentEditingEvent = null;
        
        // Reset button states
        _saveEventButton.Enabled = false;
        _addEventButton.Enabled = true;
        
        // Clear event list selection
        if (_eventsList != null)
        {
            _eventsList.SelectedIndex = -1;
        }
        
        // Reset combo boxes to defaults
        if (_eventCategoryCombo != null)
        {
            _eventCategoryCombo.SelectedIndex = 0;
        }
        if (_eventTriggerCombo != null)
        {
            _eventTriggerCombo.SelectedIndex = 0;
        }
        
        // Clear parameter controls
        ClearEventParams();
        
        // Refresh the params panel to show default state
        if (_eventParamsPanel != null)
        {
            UpdateEventParamsPanel();
        }
    }

    private void UpdateTagsTab()
    {
        _tagsTab.Controls.Clear();
        
        if (_selectedComponent == null)
            return;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(5)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int row = 0;

        layout.Controls.Add(new Label { Text = "Tag Binding:", AutoSize = true }, 0, row);
        var tagSelector = new TagSelectorWidget { Dock = DockStyle.Fill, Height = 25 };
        tagSelector.SetAvailableTags(_availableTagTables);
        if (!string.IsNullOrEmpty(_selectedComponent.TagName))
        {
            tagSelector.SetSelectedTagName(_selectedComponent.TagName);
        }
        tagSelector.TagSelected += (s, tagName) =>
        {
            _selectedComponent.TagName = tagName ?? string.Empty;
            TriggerAutoSave();
        };
        layout.Controls.Add(tagSelector, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _tagsTab.Controls.Add(layout);
    }

    private void OnAddEvent(object? sender, EventArgs e)
    {
        if (_selectedComponent == null || _eventsModule == null)
            return;

        // Clear UI for new event
        _currentEditingEvent = null;
        _eventTriggerCombo.SelectedIndex = 0;
        _eventCategoryCombo.SelectedIndex = 0;
        if (_eventActionCombo.Items.Count > 0)
            _eventActionCombo.SelectedIndex = 0;
        ClearEventParams();
        _saveEventButton.Enabled = true;
        _addEventButton.Enabled = false;
    }
    
    private void OnSaveEvent(object? sender, EventArgs e)
    {
        if (_selectedComponent == null || _eventsModule == null)
            return;

        var evt = CreateEventFromUI();
        if (evt != null)
        {
            if (_currentEditingEvent != null)
            {
                // Update existing event
                evt.Id = _currentEditingEvent.Id;
                if (_eventsModule.UpdateEvent(evt.Id, evt))
                {
                    System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Updated event: {evt.Id}");
                }
            }
            else
            {
                // Add new event
                if (_eventsModule.AddEvent(evt))
                {
                    System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Added event: {evt.Id}");
                    
                    // Associate event with component
                    if (!_selectedComponent.EventIds.Contains(evt.Id))
                    {
                        _selectedComponent.EventIds.Add(evt.Id);
                    }
                }
            }
            
            UpdateEventsTab();
            ClearEventParams();
            _saveEventButton.Enabled = false;
            _addEventButton.Enabled = true;
            _currentEditingEvent = null;
            TriggerAutoSave();
        }
    }
    
    private void ClearEventParams()
    {
        if (_screenCombo != null)
            _screenCombo.SelectedIndex = 0;
        if (_tagSelector != null)
            _tagSelector.SetSelectedTagName("");
        if (_valueTextBox != null)
            _valueTextBox.Text = "";
        if (_scriptPathTextBox != null)
            _scriptPathTextBox.Text = "";
        if (_argsTextBox != null)
            _argsTextBox.Text = "";
    }

    private void OnRemoveEvent(object? sender, EventArgs e)
    {
        if (_eventsList.SelectedItem is EventListItem item && _eventsModule != null && _selectedComponent != null)
        {
            _eventsModule.RemoveEvent(item.Event.Id);
            
            // Remove event ID from component
            _selectedComponent.EventIds.Remove(item.Event.Id);
            
            UpdateEventsTab();
            ClearEventParams();
            _saveEventButton.Enabled = false;
            _addEventButton.Enabled = true;
            _currentEditingEvent = null;
            TriggerAutoSave();
        }
    }

    private void OnEventSelected(object? sender, EventArgs e)
    {
        if (_eventsList.SelectedItem is EventListItem item)
        {
            LoadEventToUI(item.Event);
        }
    }

    private ScadaEvent? CreateEventFromUI()
    {
        if (_selectedComponent == null)
            return null;

        var evt = new ScadaEvent
        {
            Id = _currentEditingEvent?.Id ?? Guid.NewGuid().ToString(),
            Name = _currentEditingEvent?.Name ?? $"Event {_eventsList.Items.Count + 1}",
            Description = _currentEditingEvent?.Description ?? "",
            Trigger = new EventTrigger
            {
                ComponentId = _selectedComponent.Id.ToString(),
                Type = _eventTriggerCombo.SelectedItem?.ToString() ?? "OnClick"
            }
        };

        var action = new EventAction();
        int categoryIndex = _eventCategoryCombo.SelectedIndex;
        string actionText = _eventActionCombo.SelectedItem?.ToString() ?? "";

        switch (categoryIndex)
        {
            case 0: // Screen Navigation
                action.Type = "NavigateScreen";
                if (_screenCombo != null && _screenCombo.SelectedItem is ScreenComboItem screenItem)
                {
                    action.ScreenId = screenItem.Id;
                }
                break;
            case 1: // Tag Operations
                if (actionText == "Set Bit")
                    action.Type = "SetBit";
                else if (actionText == "Reset Bit")
                    action.Type = "ResetBit";
                else if (actionText == "Toggle Bit")
                    action.Type = "ToggleBit";
                else if (actionText == "Write Value")
                    action.Type = "WriteTag";
                else if (actionText == "Increment Value")
                    action.Type = "IncrementTag";
                else if (actionText == "Decrement Value")
                    action.Type = "DecrementTag";
                else if (actionText == "Copy Tag Value")
                    action.Type = "CopyTagValue";
                else if (actionText == "Swap Tag Values")
                    action.Type = "SwapTagValues";
                
                if (_tagSelector != null)
                {
                    action.Tag = _tagSelector.SelectedTagName();
                }
                if (_valueTextBox != null && !string.IsNullOrEmpty(_valueTextBox.Text))
                {
                    action.Value = _valueTextBox.Text;
                }
                break;
            case 2: // Script Actions
                action.Type = "RunScript";
                if (_scriptPathTextBox != null)
                {
                    action.Script = _scriptPathTextBox.Text;
                }
                if (_argsTextBox != null)
                {
                    action.Arguments = _argsTextBox.Text;
                }
                break;
        }

        evt.Actions.Add(action);
        
        if (_currentEditingEvent == null)
        {
            evt.Metadata.CreatedBy = "User";
            evt.Metadata.CreatedAt = DateTime.Now.ToString("O");
        }
        else
        {
            evt.Metadata = _currentEditingEvent.Metadata;
            evt.Metadata.ModifiedBy = "User";
            evt.Metadata.ModifiedAt = DateTime.Now.ToString("O");
        }

        return evt;
    }

    private void LoadEventToUI(ScadaEvent evt)
    {
        _currentEditingEvent = evt;
        
        // Set trigger
        int triggerIndex = _eventTriggerCombo.Items.IndexOf(evt.Trigger.Type);
        if (triggerIndex >= 0)
            _eventTriggerCombo.SelectedIndex = triggerIndex;

        // Set category and action based on first action
        if (evt.Actions.Count > 0)
        {
            var action = evt.Actions[0];
            
            // Determine category from action type
            int categoryIndex = 0;
            string actionText = "";
            
            if (action.Type == "NavigateScreen" || action.Type == "CloseScreen" || action.Type == "SwitchToScreen")
            {
                categoryIndex = 0; // Screen Navigation
                actionText = action.Type == "NavigateScreen" ? "Open Screen" : 
                           action.Type == "CloseScreen" ? "Close Screen" : "Switch to Screen";
            }
            else if (action.Type == "SetBit" || action.Type == "ResetBit" || action.Type == "ToggleBit" || 
                     action.Type == "WriteTag" || action.Type == "IncrementTag" || action.Type == "DecrementTag")
            {
                categoryIndex = 1; // Tag Operations
                actionText = action.Type == "SetBit" ? "Set Bit" :
                           action.Type == "ResetBit" ? "Reset Bit" :
                           action.Type == "ToggleBit" ? "Toggle Bit" :
                           action.Type == "WriteTag" ? "Write Value" :
                           action.Type == "IncrementTag" ? "Increment Value" : "Decrement Value";
            }
            else if (action.Type == "RunScript")
            {
                categoryIndex = 2; // Script Actions
                actionText = "Run Script";
            }
            
            _eventCategoryCombo.SelectedIndex = categoryIndex;
            OnCategoryChanged(null, EventArgs.Empty); // This will populate action combo
            
            // Set action
            int actionIndex = _eventActionCombo.Items.IndexOf(actionText);
            if (actionIndex >= 0)
                _eventActionCombo.SelectedIndex = actionIndex;
            
            // Load parameters
            UpdateEventParamsPanel();
            
            if (categoryIndex == 0 && _screenCombo != null && !string.IsNullOrEmpty(action.ScreenId))
            {
                for (int i = 0; i < _screenCombo.Items.Count; i++)
                {
                    if (_screenCombo.Items[i] is ScreenComboItem item && item.Id == action.ScreenId)
                    {
                        _screenCombo.SelectedIndex = i;
                        break;
                    }
                }
            }
            else if (categoryIndex == 1)
            {
                if (_tagSelector != null && !string.IsNullOrEmpty(action.Tag))
                {
                    _tagSelector.SetSelectedTagName(action.Tag);
                }
                if (_valueTextBox != null && !string.IsNullOrEmpty(action.Value))
                {
                    _valueTextBox.Text = action.Value;
                }
            }
            else if (categoryIndex == 2)
            {
                if (_scriptPathTextBox != null && !string.IsNullOrEmpty(action.Script))
                {
                    _scriptPathTextBox.Text = action.Script;
                }
                if (_argsTextBox != null && !string.IsNullOrEmpty(action.Arguments))
                {
                    _argsTextBox.Text = action.Arguments;
                }
            }
        }
        
        _saveEventButton.Enabled = true;
        _addEventButton.Enabled = false;
    }
    
    private void OnEditEvent(object? sender, EventArgs e)
    {
        if (_eventsList.SelectedItem is EventListItem item)
        {
            LoadEventToUI(item.Event);
        }
    }

    private void TriggerAutoSave()
    {
        RequestAutoSave?.Invoke(this, EventArgs.Empty);
        
        // Also ensure events are saved immediately when changed
        if (_selectedComponent != null && _eventsModule != null)
        {
            // Events are saved immediately when added/updated/removed in EventsModule
            // Component EventIds are updated in memory and will be saved when screen is saved
        }
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

    private class EventListItem
    {
        public ScadaEvent Event { get; }

        public EventListItem(ScadaEvent evt)
        {
            Event = evt;
        }

        public override string ToString() => string.IsNullOrEmpty(Event.Name) ? Event.Id : Event.Name;
    }

    private class ScreenComboItem
    {
        public string Name { get; }
        public string Id { get; }

        public ScreenComboItem(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public override string ToString() => Name;
    }
}
