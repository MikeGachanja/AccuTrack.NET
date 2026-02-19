using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Designer.Modules.TagEngine;
using Designer.Modules.Events;
using Newtonsoft.Json.Linq;
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
    private TextBox _eventNameTextBox;
    private ComboBox _eventCategoryCombo;
    private ComboBox _eventActionCombo;
    private ComboBox _eventTriggerCombo;
    private Panel _eventParamsPanel;
    private EventHandler? _paramsPanelResizeHandler;
    private ScadaEvent? _currentEditingEvent;
    
    // Event parameter controls (stored for access)
    private ComboBox? _screenCombo;
    private TagSelectorWidget? _tagSelector;
    private TextBox? _valueTextBox;
    private TextBox? _scriptPathTextBox;
    private TextBox? _argsTextBox;
    
    // Animation tab controls
    private ListBox? _animationsList;
    private Button? _addAnimationButton;
    private Button? _removeAnimationButton;
    private Button? _saveAnimationButton;
    private ComboBox? _animationTypeCombo;
    private TagSelectorWidget? _animationTagSelector;
    private CheckBox? _animationBitValueCheckBox;
    private Button? _animationColorButton;
    private NumericUpDown? _animationFrequencyNumeric;
    private NumericUpDown? _animationSpeedNumeric;
    private CheckBox? _animationEnabledCheckBox;
    private AnimationConfig? _currentEditingAnimation;
    private List<AnimationConfig> _componentAnimations = new List<AnimationConfig>();
    
    // ColorChange animation table
    private DataGridView? _colorMapTable;
    private Button? _addColorMapRowButton;
    private Button? _removeColorMapRowButton;
    private Panel? _colorMapPanel;

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
        SetupAnimationTab();
        
        Controls.Add(_tabControl);
    }

    /// <summary>
    /// Updates the editor with the selected item.
    /// Must be called on the UI thread so that Events/Animation tabs paint correctly.
    /// </summary>
    public void UpdateEditor(object? item)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => UpdateEditor(item)));
            return;
        }

        _selectedComponent = item as BaseComponent;

        if (_selectedComponent == null)
        {
            ClearAllTabs();
            return;
        }

        // Ensure EventsModule is initialized if we have a SCADA project (so Events tab populates)
        if (_eventsModule == null && _scadaProject != null)
        {
            SetScadaProject(_scadaProject);
        }

        UpdateGeneralTab();
        UpdateEventsTab();
        UpdateAnimationTab();
        UpdateTagsTab();

        // Force the tab control to repaint so the visible tab (and any previously invisible tabs) show updated content
        _tabControl?.Refresh();
    }

    /// <summary>
    /// Sets available tag tables for tag binding properties.
    /// </summary>
    public void SetAvailableTagTables(List<TagTable> tagTables)
    {
        _availableTagTables = tagTables ?? new List<TagTable>();
        
        // Update tag selector in animation tab if it exists
        if (_animationTagSelector != null)
        {
            _animationTagSelector.SetAvailableTags(_availableTagTables);
        }
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
        
        // Initialize events module if not already initialized
        if (_eventsModule == null)
        {
            _eventsModule = new EventsModule();
        }
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
        _animationTab.Controls.Clear();
        _tagsTab.Controls.Clear();
    }

    private void UpdateGeneralTab()
    {
        _generalTab.Controls.Clear();
        
        if (_selectedComponent == null)
            return;

        // Create a scrollable container for the layout
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(5)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        // Set layout to anchor top-left and adjust width on resize
        layout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        layout.Location = new Point(0, 0);
        
        // Handle resize to update layout width
        scrollPanel.Resize += (s, e) =>
        {
            int scrollBarWidth = scrollPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            layout.Width = Math.Max(100, scrollPanel.ClientSize.Width - scrollBarWidth);
        };

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

        // Location - store current values to avoid null reference during ValueChanged
        var currentLocation = _selectedComponent?.Location ?? new Point(0, 0);
        var currentX = currentLocation.X;
        var currentY = currentLocation.Y;
        
        layout.Controls.Add(new Label { Text = "Location X:", AutoSize = true }, 0, row);
        var xNumeric = new NumericUpDown { Minimum = int.MinValue, Maximum = int.MaxValue, Value = currentX, Width = 100 };
        xNumeric.ValueChanged += (s, e) =>
        {
            if (_selectedComponent != null)
            {
                // Store current Y before updating
                var y = _selectedComponent.Location.Y;
                _selectedComponent.Location = new Point((int)xNumeric.Value, y);
                TriggerAutoSave();
            }
        };
        layout.Controls.Add(xNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        layout.Controls.Add(new Label { Text = "Location Y:", AutoSize = true }, 0, row);
        var yNumeric = new NumericUpDown { Minimum = int.MinValue, Maximum = int.MaxValue, Value = currentY, Width = 100 };
        yNumeric.ValueChanged += (s, e) =>
        {
            if (_selectedComponent != null)
            {
                // Store current X before updating
                var x = _selectedComponent.Location.X;
                _selectedComponent.Location = new Point(x, (int)yNumeric.Value);
                TriggerAutoSave();
            }
        };
        layout.Controls.Add(yNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Size - store current values to avoid null reference during ValueChanged
        var currentSize = _selectedComponent?.Size ?? new Size(100, 100);
        var currentWidth = currentSize.Width;
        var currentHeight = currentSize.Height;
        
        layout.Controls.Add(new Label { Text = "Width:", AutoSize = true }, 0, row);
        var widthNumeric = new NumericUpDown { Minimum = 1, Maximum = int.MaxValue, Value = currentWidth, Width = 100 };
        widthNumeric.ValueChanged += (s, e) =>
        {
            if (_selectedComponent != null)
            {
                // Store current height before updating
                var height = _selectedComponent.Size.Height;
                _selectedComponent.Size = new Size((int)widthNumeric.Value, height);
                TriggerAutoSave();
            }
        };
        layout.Controls.Add(widthNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        layout.Controls.Add(new Label { Text = "Height:", AutoSize = true }, 0, row);
        var heightNumeric = new NumericUpDown { Minimum = 1, Maximum = int.MaxValue, Value = currentHeight, Width = 100 };
        heightNumeric.ValueChanged += (s, e) =>
        {
            if (_selectedComponent != null)
            {
                // Store current width before updating
                var width = _selectedComponent.Size.Width;
                _selectedComponent.Size = new Size(width, (int)heightNumeric.Value);
                TriggerAutoSave();
            }
        };
        layout.Controls.Add(heightNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Visibility
        var visibleCheckBox = new CheckBox { Text = "Visible", Checked = _selectedComponent?.Visible ?? true };
        visibleCheckBox.CheckedChanged += (s, e) =>
        {
            if (_selectedComponent != null)
            {
                _selectedComponent.Visible = visibleCheckBox.Checked;
                TriggerAutoSave();
            }
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
        else if (_selectedComponent is SvgViewComponent svgView)
        {
            UpdateSvgViewProperties(layout, svgView, ref row);
        }
        else if (_selectedComponent is NumericComponent numeric)
        {
            UpdateNumericProperties(layout, numeric, ref row);
        }
        else if (_selectedComponent is TextLabelComponent textLabel)
        {
            UpdateTextLabelProperties(layout, textLabel, ref row);
        }
        else if (_selectedComponent is DateTimeComponent dateTime)
        {
            UpdateDateTimeProperties(layout, dateTime, ref row);
        }
        else if (_selectedComponent is IndicatorComponent indicator)
        {
            UpdateIndicatorProperties(layout, indicator, ref row);
        }
        else if (_selectedComponent is MotorComponent motor)
        {
            UpdateMotorProperties(layout, motor, ref row);
        }
        else if (_selectedComponent is PumpComponent pump)
        {
            UpdatePumpProperties(layout, pump, ref row);
        }
        else if (_selectedComponent is TankComponent tank)
        {
            UpdateTankProperties(layout, tank, ref row);
        }
        else if (_selectedComponent is ConveyorComponent conveyor)
        {
            UpdateConveyorProperties(layout, conveyor, ref row);
        }
        else if (_selectedComponent is ToggleSwitchComponent toggleSwitch)
        {
            UpdateToggleSwitchProperties(layout, toggleSwitch, ref row);
        }
        else if (_selectedComponent is RectangleComponent rectangle)
        {
            UpdateRectangleProperties(layout, rectangle, ref row);
        }
        else if (_selectedComponent is TriangleComponent triangle)
        {
            UpdateTriangleProperties(layout, triangle, ref row);
        }
        else if (_selectedComponent is LineComponent line)
        {
            UpdateLineProperties(layout, line, ref row);
        }
        // Add other component types as needed

        scrollPanel.Controls.Add(layout);
        _generalTab.Controls.Add(scrollPanel);
        
        // Trigger initial width calculation
        int initialScrollBarWidth = scrollPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        layout.Width = Math.Max(100, scrollPanel.ClientSize.Width - initialScrollBarWidth);
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

        // Action (for legacy/navigation buttons)
        layout.Controls.Add(new Label { Text = "Action:", AutoSize = true }, 0, row);
        var actionTextBox = new TextBox { Text = button.Action ?? string.Empty, Dock = DockStyle.Fill };
        actionTextBox.TextChanged += (s, e) =>
        {
            button.Action = actionTextBox.Text;
            TriggerAutoSave();
        };
        var actionHelpLabel = new Label 
        { 
            Text = "Format: NavigateScreen:ScreenName or action type", 
            AutoSize = true, 
            ForeColor = Color.Gray,
            Font = new Font("Arial", 7)
        };
        var actionLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true };
        actionLayout.Controls.Add(actionTextBox);
        actionLayout.Controls.Add(actionHelpLabel);
        layout.Controls.Add(actionLayout, 1, row);
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

        // Text Color (color of the button text, independent of background)
        layout.Controls.Add(new Label { Text = "Text Color:", AutoSize = true }, 0, row);
        var textColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(textColorButton, button.TextColor);
        textColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = button.TextColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    button.TextColor = colorDialog.Color;
                    UpdateColorButton(textColorButton, button.TextColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(textColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // BorderColor
        layout.Controls.Add(new Label { Text = "Border Color:", AutoSize = true }, 0, row);
        var borderColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(borderColorButton, button.BorderColor);
        borderColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = button.BorderColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    button.BorderColor = colorDialog.Color;
                    UpdateColorButton(borderColorButton, button.BorderColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(borderColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // BorderWidth
        layout.Controls.Add(new Label { Text = "Border Width:", AutoSize = true }, 0, row);
        var borderWidthNumeric = new NumericUpDown { Minimum = 0, Maximum = 10, Value = button.BorderWidth, Width = 100 };
        borderWidthNumeric.ValueChanged += (s, e) =>
        {
            button.BorderWidth = (int)borderWidthNumeric.Value;
            TriggerAutoSave();
        };
        layout.Controls.Add(borderWidthNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Font (family, size, style) - so components can have different fonts at runtime
        AddFontEditorRows(layout, button.Font, (name, size, style) =>
        {
            var oldFont = button.Font;
            button.Font = new Font(name, size, style);
            oldFont?.Dispose();
            TriggerAutoSave();
        }, ref row);
    }

    private void UpdateNumericProperties(TableLayoutPanel layout, NumericComponent numeric, ref int row)
    {
        // Label
        layout.Controls.Add(new Label { Text = "Label:", AutoSize = true }, 0, row);
        var labelTextBox = new TextBox { Text = numeric.Label, Dock = DockStyle.Fill };
        labelTextBox.TextChanged += (s, e) =>
        {
            numeric.Label = labelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(labelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Decimal Places
        layout.Controls.Add(new Label { Text = "Decimal Places:", AutoSize = true }, 0, row);
        var decimalPlacesNumeric = new NumericUpDown { Minimum = 0, Maximum = 10, Value = numeric.DecimalPlaces, Width = 100 };
        decimalPlacesNumeric.ValueChanged += (s, e) =>
        {
            numeric.DecimalPlaces = (int)decimalPlacesNumeric.Value;
            TriggerAutoSave();
        };
        layout.Controls.Add(decimalPlacesNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Suffix
        layout.Controls.Add(new Label { Text = "Suffix:", AutoSize = true }, 0, row);
        var suffixTextBox = new TextBox { Text = numeric.Suffix, Dock = DockStyle.Fill };
        suffixTextBox.TextChanged += (s, e) =>
        {
            numeric.Suffix = suffixTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(suffixTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Label Color
        layout.Controls.Add(new Label { Text = "Label Color:", AutoSize = true }, 0, row);
        var labelColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(labelColorButton, numeric.LabelColor);
        labelColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = numeric.LabelColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    numeric.LabelColor = colorDialog.Color;
                    UpdateColorButton(labelColorButton, numeric.LabelColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(labelColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Value Color
        layout.Controls.Add(new Label { Text = "Value Color:", AutoSize = true }, 0, row);
        var valueColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(valueColorButton, numeric.ValueColor);
        valueColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = numeric.ValueColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    numeric.ValueColor = colorDialog.Color;
                    UpdateColorButton(valueColorButton, numeric.ValueColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(valueColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Label font (family, size, style) - so runtime can show different fonts per component
        layout.Controls.Add(new Label { Text = "Label font:", AutoSize = true, Font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold) }, 0, row);
        layout.SetColumnSpan(layout.Controls[layout.Controls.Count - 1], 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
        AddFontEditorRows(layout, numeric.LabelFont, (name, size, style) =>
        {
            var oldF = numeric.LabelFont;
            numeric.LabelFont = new Font(name, size, style);
            oldF?.Dispose();
            TriggerAutoSave();
        }, ref row);

        // Value font (family, size, style)
        layout.Controls.Add(new Label { Text = "Value font:", AutoSize = true, Font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold) }, 0, row);
        layout.SetColumnSpan(layout.Controls[layout.Controls.Count - 1], 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
        AddFontEditorRows(layout, numeric.ValueFont, (name, size, style) =>
        {
            var oldF = numeric.ValueFont;
            numeric.ValueFont = new Font(name, size, style);
            oldF?.Dispose();
            TriggerAutoSave();
        }, ref row);
    }

    private void UpdateTextLabelProperties(TableLayoutPanel layout, TextLabelComponent textLabel, ref int row)
    {
        // Text (this is the label text for TextLabelComponent)
        layout.Controls.Add(new Label { Text = "Text:", AutoSize = true }, 0, row);
        var textTextBox = new TextBox { Text = textLabel.Text, Dock = DockStyle.Fill };
        textTextBox.TextChanged += (s, e) =>
        {
            textLabel.Text = textTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(textTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // ForeColor
        layout.Controls.Add(new Label { Text = "Fore Color:", AutoSize = true }, 0, row);
        var foreColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(foreColorButton, textLabel.ForeColor);
        foreColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = textLabel.ForeColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    textLabel.ForeColor = colorDialog.Color;
                    UpdateColorButton(foreColorButton, textLabel.ForeColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(foreColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Text Color (color of the label text, independent of background)
        layout.Controls.Add(new Label { Text = "Text Color:", AutoSize = true }, 0, row);
        var textColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(textColorButton, textLabel.TextColor);
        textColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = textLabel.TextColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    textLabel.TextColor = colorDialog.Color;
                    UpdateColorButton(textColorButton, textLabel.TextColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(textColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Font (family, size, style) - so components can have different fonts at runtime
        AddFontEditorRows(layout, textLabel.Font, (name, size, style) =>
        {
            var oldFont = textLabel.Font;
            textLabel.Font = new Font(name, size, style);
            oldFont?.Dispose();
            TriggerAutoSave();
        }, ref row);
    }

    private void UpdateDateTimeProperties(TableLayoutPanel layout, DateTimeComponent dateTime, ref int row)
    {
        // Show Label
        var showLabelCheckBox = new CheckBox { Text = "Show Label", Checked = dateTime.ShowLabel };
        showLabelCheckBox.CheckedChanged += (s, e) =>
        {
            dateTime.ShowLabel = showLabelCheckBox.Checked;
            TriggerAutoSave();
        };
        layout.Controls.Add(showLabelCheckBox, 0, row);
        layout.SetColumnSpan(showLabelCheckBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Label
        layout.Controls.Add(new Label { Text = "Label:", AutoSize = true }, 0, row);
        var labelTextBox = new TextBox { Text = dateTime.Label, Dock = DockStyle.Fill };
        labelTextBox.TextChanged += (s, e) =>
        {
            dateTime.Label = labelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(labelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Text Color (color of the date/time text)
        layout.Controls.Add(new Label { Text = "Text Color:", AutoSize = true }, 0, row);
        var textColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(textColorButton, dateTime.TextColor);
        textColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = dateTime.TextColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    dateTime.TextColor = colorDialog.Color;
                    UpdateColorButton(textColorButton, dateTime.TextColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(textColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateIndicatorProperties(TableLayoutPanel layout, IndicatorComponent indicator, ref int row)
    {
        // Label
        layout.Controls.Add(new Label { Text = "Label:", AutoSize = true }, 0, row);
        var labelTextBox = new TextBox { Text = indicator.Label, Dock = DockStyle.Fill };
        labelTextBox.TextChanged += (s, e) =>
        {
            indicator.Label = labelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(labelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateMotorProperties(TableLayoutPanel layout, MotorComponent motor, ref int row)
    {
        // Label
        layout.Controls.Add(new Label { Text = "Label:", AutoSize = true }, 0, row);
        var labelTextBox = new TextBox { Text = motor.Label, Dock = DockStyle.Fill };
        labelTextBox.TextChanged += (s, e) =>
        {
            motor.Label = labelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(labelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdatePumpProperties(TableLayoutPanel layout, PumpComponent pump, ref int row)
    {
        // Label
        layout.Controls.Add(new Label { Text = "Label:", AutoSize = true }, 0, row);
        var labelTextBox = new TextBox { Text = pump.Label, Dock = DockStyle.Fill };
        labelTextBox.TextChanged += (s, e) =>
        {
            pump.Label = labelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(labelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateTankProperties(TableLayoutPanel layout, TankComponent tank, ref int row)
    {
        // Label
        layout.Controls.Add(new Label { Text = "Label:", AutoSize = true }, 0, row);
        var labelTextBox = new TextBox { Text = tank.Label, Dock = DockStyle.Fill };
        labelTextBox.TextChanged += (s, e) =>
        {
            tank.Label = labelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(labelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateConveyorProperties(TableLayoutPanel layout, ConveyorComponent conveyor, ref int row)
    {
        // Label
        layout.Controls.Add(new Label { Text = "Label:", AutoSize = true }, 0, row);
        var labelTextBox = new TextBox { Text = conveyor.Label, Dock = DockStyle.Fill };
        labelTextBox.TextChanged += (s, e) =>
        {
            conveyor.Label = labelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(labelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateToggleSwitchProperties(TableLayoutPanel layout, ToggleSwitchComponent toggleSwitch, ref int row)
    {
        // On Label
        layout.Controls.Add(new Label { Text = "On Label:", AutoSize = true }, 0, row);
        var onLabelTextBox = new TextBox { Text = toggleSwitch.OnLabel, Dock = DockStyle.Fill };
        onLabelTextBox.TextChanged += (s, e) =>
        {
            toggleSwitch.OnLabel = onLabelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(onLabelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Off Label
        layout.Controls.Add(new Label { Text = "Off Label:", AutoSize = true }, 0, row);
        var offLabelTextBox = new TextBox { Text = toggleSwitch.OffLabel, Dock = DockStyle.Fill };
        offLabelTextBox.TextChanged += (s, e) =>
        {
            toggleSwitch.OffLabel = offLabelTextBox.Text;
            TriggerAutoSave();
        };
        layout.Controls.Add(offLabelTextBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Show Labels
        var showLabelsCheckBox = new CheckBox { Text = "Show Labels", Checked = toggleSwitch.ShowLabels };
        showLabelsCheckBox.CheckedChanged += (s, e) =>
        {
            toggleSwitch.ShowLabels = showLabelsCheckBox.Checked;
            TriggerAutoSave();
        };
        layout.Controls.Add(showLabelsCheckBox, 0, row);
        layout.SetColumnSpan(showLabelsCheckBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateRectangleProperties(TableLayoutPanel layout, RectangleComponent rectangle, ref int row)
    {
        // Fill Color
        layout.Controls.Add(new Label { Text = "Fill Color:", AutoSize = true }, 0, row);
        var fillColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(fillColorButton, rectangle.FillColor);
        fillColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = rectangle.FillColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    rectangle.FillColor = colorDialog.Color;
                    UpdateColorButton(fillColorButton, rectangle.FillColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(fillColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Border Color
        layout.Controls.Add(new Label { Text = "Border Color:", AutoSize = true }, 0, row);
        var borderColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(borderColorButton, rectangle.BorderColor);
        borderColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = rectangle.BorderColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    rectangle.BorderColor = colorDialog.Color;
                    UpdateColorButton(borderColorButton, rectangle.BorderColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(borderColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Border Width
        layout.Controls.Add(new Label { Text = "Border Width:", AutoSize = true }, 0, row);
        var borderWidthNumeric = new NumericUpDown { Minimum = 0, Maximum = 20, Value = rectangle.BorderWidth, Width = 100 };
        borderWidthNumeric.ValueChanged += (s, e) =>
        {
            rectangle.BorderWidth = (int)borderWidthNumeric.Value;
            TriggerAutoSave();
        };
        layout.Controls.Add(borderWidthNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Filled
        var filledCheckBox = new CheckBox { Text = "Filled", Checked = rectangle.Filled };
        filledCheckBox.CheckedChanged += (s, e) =>
        {
            rectangle.Filled = filledCheckBox.Checked;
            TriggerAutoSave();
        };
        layout.Controls.Add(filledCheckBox, 0, row);
        layout.SetColumnSpan(filledCheckBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateTriangleProperties(TableLayoutPanel layout, TriangleComponent triangle, ref int row)
    {
        // Fill Color
        layout.Controls.Add(new Label { Text = "Fill Color:", AutoSize = true }, 0, row);
        var fillColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(fillColorButton, triangle.FillColor);
        fillColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = triangle.FillColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    triangle.FillColor = colorDialog.Color;
                    UpdateColorButton(fillColorButton, triangle.FillColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(fillColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Border Color
        layout.Controls.Add(new Label { Text = "Border Color:", AutoSize = true }, 0, row);
        var borderColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(borderColorButton, triangle.BorderColor);
        borderColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = triangle.BorderColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    triangle.BorderColor = colorDialog.Color;
                    UpdateColorButton(borderColorButton, triangle.BorderColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(borderColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Border Width
        layout.Controls.Add(new Label { Text = "Border Width:", AutoSize = true }, 0, row);
        var borderWidthNumeric = new NumericUpDown { Minimum = 0, Maximum = 20, Value = triangle.BorderWidth, Width = 100 };
        borderWidthNumeric.ValueChanged += (s, e) =>
        {
            triangle.BorderWidth = (int)borderWidthNumeric.Value;
            TriggerAutoSave();
        };
        layout.Controls.Add(borderWidthNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Filled
        var filledCheckBox = new CheckBox { Text = "Filled", Checked = triangle.Filled };
        filledCheckBox.CheckedChanged += (s, e) =>
        {
            triangle.Filled = filledCheckBox.Checked;
            TriggerAutoSave();
        };
        layout.Controls.Add(filledCheckBox, 0, row);
        layout.SetColumnSpan(filledCheckBox, 2);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Direction
        layout.Controls.Add(new Label { Text = "Direction:", AutoSize = true }, 0, row);
        var directionCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        directionCombo.Items.AddRange(new object[] { "Up", "Down", "Left", "Right" });
        directionCombo.SelectedItem = triangle.Direction.ToString();
        directionCombo.SelectedIndexChanged += (s, e) =>
        {
            if (Enum.TryParse<TriangleDirection>(directionCombo.SelectedItem?.ToString(), out var dir))
            {
                triangle.Direction = dir;
                TriggerAutoSave();
            }
        };
        layout.Controls.Add(directionCombo, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateLineProperties(TableLayoutPanel layout, LineComponent line, ref int row)
    {
        // Line Color
        layout.Controls.Add(new Label { Text = "Line Color:", AutoSize = true }, 0, row);
        var lineColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(lineColorButton, line.LineColor);
        lineColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = line.LineColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    line.LineColor = colorDialog.Color;
                    UpdateColorButton(lineColorButton, line.LineColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(lineColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Line Width
        layout.Controls.Add(new Label { Text = "Line Width:", AutoSize = true }, 0, row);
        var lineWidthNumeric = new NumericUpDown { Minimum = 1, Maximum = 20, Value = line.LineWidth, Width = 100 };
        lineWidthNumeric.ValueChanged += (s, e) =>
        {
            line.LineWidth = (int)lineWidthNumeric.Value;
            TriggerAutoSave();
        };
        layout.Controls.Add(lineWidthNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Style
        layout.Controls.Add(new Label { Text = "Style:", AutoSize = true }, 0, row);
        var styleCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
        styleCombo.Items.AddRange(new object[] { "Solid", "Dashed", "Dotted" });
        styleCombo.SelectedItem = line.Style.ToString();
        styleCombo.SelectedIndexChanged += (s, e) =>
        {
            if (Enum.TryParse<LineStyle>(styleCombo.SelectedItem?.ToString(), out var style))
            {
                line.Style = style;
                TriggerAutoSave();
            }
        };
        layout.Controls.Add(styleCombo, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    private void UpdateSvgViewProperties(TableLayoutPanel layout, SvgViewComponent svgView, ref int row)
    {
        // SVG Path
        layout.Controls.Add(new Label { Text = "SVG Path:", AutoSize = true }, 0, row);
        var svgPathLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var svgPathTextBox = new TextBox { Text = svgView.SvgPath, Width = 300 };
        svgPathTextBox.TextChanged += (s, e) =>
        {
            svgView.SvgPath = svgPathTextBox.Text;
            TriggerAutoSave();
            // Invalidate canvas to refresh SVG display
            if (_selectedComponent != null)
            {
                // Trigger redraw by invalidating parent canvas if available
                // This will be handled by the screen editor's canvas invalidation
            }
        };
        var browseButton = new Button { Text = "Browse...", Width = 80 };
        browseButton.Click += (s, e) =>
        {
            using (var dialog = new OpenFileDialog 
            { 
                Filter = "SVG Files|*.svg|All Files|*.*",
                InitialDirectory = GetSvgBasePath()
            })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Convert to relative path if in SVG directory
                    string selectedPath = dialog.FileName;
                    string svgBasePath = GetSvgBasePath();
                    
                    if (selectedPath.StartsWith(svgBasePath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract relative path
                        string relativePath = Path.GetRelativePath(svgBasePath, selectedPath);
                        svgView.SvgPath = relativePath;
                        svgPathTextBox.Text = relativePath;
                    }
                    else
                    {
                        // Use full path or just filename as fallback
                        svgView.SvgPath = Path.GetFileName(selectedPath);
                        svgPathTextBox.Text = svgView.SvgPath;
                        EmitWarning("Selected SVG file is not in the SVG directory. Using filename only.");
                    }
                    TriggerAutoSave();
                }
            }
        };
        svgPathLayout.Controls.Add(svgPathTextBox);
        svgPathLayout.Controls.Add(browseButton);
        layout.Controls.Add(svgPathLayout, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Border Color
        layout.Controls.Add(new Label { Text = "Border Color:", AutoSize = true }, 0, row);
        var borderColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(borderColorButton, svgView.BorderColor);
        borderColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = svgView.BorderColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    svgView.BorderColor = colorDialog.Color;
                    UpdateColorButton(borderColorButton, svgView.BorderColor);
                    TriggerAutoSave();
                }
            }
        };
        layout.Controls.Add(borderColorButton, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        // Border Width
        layout.Controls.Add(new Label { Text = "Border Width:", AutoSize = true }, 0, row);
        var borderWidthNumeric = new NumericUpDown { Minimum = 0, Maximum = 10, Value = svgView.BorderWidth, Width = 100 };
        borderWidthNumeric.ValueChanged += (s, e) =>
        {
            svgView.BorderWidth = (int)borderWidthNumeric.Value;
            TriggerAutoSave();
        };
        layout.Controls.Add(borderWidthNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;
    }

    /// <summary>
    /// Gets the base SVG directory path (executable directory/svg/).
    /// </summary>
    private static string GetSvgBasePath()
    {
        string exeDir = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) ?? System.Windows.Forms.Application.StartupPath;
        return Path.Combine(exeDir, "svg");
    }

    /// <summary>
    /// Emits a warning message (placeholder - can be connected to console if needed).
    /// </summary>
    private void EmitWarning(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[PropertyEditor Warning] {message}");
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

    private void AddFontEditorRows(TableLayoutPanel layout, Font currentFont, Action<string, float, FontStyle> onFontChanged, ref int row)
    {
        var fontName = currentFont?.Name ?? "Arial";
        var fontSize = currentFont?.Size ?? 9f;
        var fontStyle = currentFont?.Style ?? FontStyle.Regular;

        layout.Controls.Add(new Label { Text = "Font:", AutoSize = true }, 0, row);
        var fontNameBox = new TextBox { Text = fontName, Dock = DockStyle.Fill };
        layout.Controls.Add(fontNameBox, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        layout.Controls.Add(new Label { Text = "Font Size:", AutoSize = true }, 0, row);
        var fontSizeNumeric = new NumericUpDown { Minimum = 6, Maximum = 72, Value = (decimal)fontSize, Width = 100, DecimalPlaces = 1, Increment = 1 };
        layout.Controls.Add(fontSizeNumeric, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        layout.Controls.Add(new Label { Text = "Font Style:", AutoSize = true }, 0, row);
        var fontStyleCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        fontStyleCombo.Items.AddRange(new object[] { "Regular", "Bold", "Italic", "Bold, Italic" });
        fontStyleCombo.SelectedIndex = fontStyle switch { FontStyle.Bold => 1, FontStyle.Italic => 2, FontStyle.Bold | FontStyle.Italic => 3, _ => 0 };
        layout.Controls.Add(fontStyleCombo, 1, row);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        row++;

        void ApplyFont()
        {
            var name = fontNameBox.Text;
            if (string.IsNullOrWhiteSpace(name)) name = "Arial";
            var size = (float)fontSizeNumeric.Value;
            var style = fontStyleCombo.SelectedIndex switch { 1 => FontStyle.Bold, 2 => FontStyle.Italic, 3 => FontStyle.Bold | FontStyle.Italic, _ => FontStyle.Regular };
            try
            {
                onFontChanged(name, size, style);
            }
            catch { /* ignore invalid font */ }
        }
        fontNameBox.TextChanged += (s, e) => ApplyFont();
        fontSizeNumeric.ValueChanged += (s, e) => ApplyFont();
        fontStyleCombo.SelectedIndexChanged += (s, e) => ApplyFont();
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

        // Event configuration - wrapped in scrollable panel
        var configGroup = new GroupBox { Text = "Event Configuration", Dock = DockStyle.Fill };
        var configScrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var configLayout = new TableLayoutPanel { ColumnCount = 2, Padding = new Padding(5), AutoSize = true };
        configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        int configRow = 0;

        // Event Name
        configLayout.Controls.Add(new Label { Text = "Event Name:", AutoSize = true }, 0, configRow);
        _eventNameTextBox = new TextBox { Dock = DockStyle.Fill, Height = 23 };
        _eventNameTextBox.TextChanged += (s, e) =>
        {
            // Update the event name in memory when typing
            if (_currentEditingEvent != null)
            {
                _currentEditingEvent.Name = _eventNameTextBox.Text;
                // Refresh the list display to show updated name
                int selectedIndex = _eventsList.SelectedIndex;
                if (selectedIndex >= 0)
                {
                    _eventsList.Items[selectedIndex] = new EventListItem(_currentEditingEvent);
                    _eventsList.SelectedIndex = selectedIndex;
                }
            }
        };
        configLayout.Controls.Add(_eventNameTextBox, 1, configRow);
        configLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        configRow++;

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

        // Parameters panel (will be populated based on category) - scrollable
        _eventParamsPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, MinimumSize = new Size(0, 150) };
        configLayout.Controls.Add(_eventParamsPanel, 0, configRow);
        configLayout.SetColumnSpan(_eventParamsPanel, 2);
        configLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize, 200F));

        // Add configLayout to scroll panel
        configScrollPanel.Controls.Add(configLayout);
        configLayout.Location = new Point(0, 0);
        configLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        configLayout.Width = configScrollPanel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
        
        // Handle resize to update configLayout width
        configScrollPanel.Resize += (s, e) =>
        {
            configLayout.Width = configScrollPanel.ClientSize.Width - (configScrollPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0);
        };

        configGroup.Controls.Add(configScrollPanel);
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
        // Use AutoSize instead of Dock to allow scrolling
        var paramsLayout = new TableLayoutPanel { ColumnCount = 2, Padding = new Padding(5), AutoSize = true };
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

        // Add paramsLayout to scrollable panel
        _eventParamsPanel.Controls.Add(paramsLayout);
        paramsLayout.Location = new Point(0, 0);
        paramsLayout.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        
        // Remove previous resize handler if exists
        if (_paramsPanelResizeHandler != null)
        {
            _eventParamsPanel.Resize -= _paramsPanelResizeHandler;
        }
        
        // Update width when panel is resized
        _paramsPanelResizeHandler = (sender, e) =>
        {
            if (paramsLayout != null && paramsLayout.Parent == _eventParamsPanel)
            {
                int scrollBarWidth = _eventParamsPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
                paramsLayout.Width = Math.Max(100, _eventParamsPanel.ClientSize.Width - scrollBarWidth);
            }
        };
        
        _eventParamsPanel.Resize += _paramsPanelResizeHandler;
        _paramsPanelResizeHandler(null, EventArgs.Empty);
    }

    private void UpdateEventsTab()
    {
        // Reset event configuration panel when switching components
        ResetEventConfigurationPanel();
        
        if (_selectedComponent == null)
        {
            if (_eventsList != null)
                _eventsList.Items.Clear();
            return;
        }

        // Ensure EventsModule is initialized
        if (_eventsModule == null && _scadaProject != null)
        {
            SetScadaProject(_scadaProject);
        }

        if (_eventsModule == null)
        {
            if (_eventsList != null)
                _eventsList.Items.Clear();
            return;
        }

        if (_eventsList != null)
        {
            _eventsList.Items.Clear();
            var componentId = _selectedComponent.Id.ToString();
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Loading events for component ID: {componentId}");
            System.Diagnostics.Debug.WriteLine($"[PropertyEditor] EventsModule initialized: {_eventsModule != null}");
            if (_eventsModule != null)
            {
                var eventsPath = _eventsModule.GetEventsJsonPath();
                System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Events JSON path: {eventsPath ?? "null"}");
                if (!string.IsNullOrEmpty(eventsPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Events JSON exists: {System.IO.File.Exists(eventsPath)}");
                }
            }
            
            var events = _eventsModule?.GetEventsForComponent(componentId) ?? new List<ScadaEvent>();
            System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Found {events.Count} event(s) for component {componentId}");
            
            foreach (var evt in events)
            {
                _eventsList.Items.Add(new EventListItem(evt));
            }

            // Force repaint so Events tab shows new data when user switches to it (fixes stale display for subsequent components)
            _eventsList.Refresh();
            _eventsTab.Invalidate(true);
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
        
        // Clear event name
        if (_eventNameTextBox != null)
        {
            _eventNameTextBox.Text = string.Empty;
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
    
    private void SetupAnimationTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(5)
        };

        // Animations list group
        var listGroup = new GroupBox { Text = "Animations", Dock = DockStyle.Fill };
        var listLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(5) };

        _animationsList = new ListBox { Dock = DockStyle.Fill, Height = 150 };
        _animationsList.SelectedIndexChanged += OnAnimationSelected;
        listLayout.Controls.Add(_animationsList, 0, 0);
        listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));

        // Animation buttons
        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Height = 30 };
        _addAnimationButton = new Button { Text = "Add Animation", Width = 100 };
        _addAnimationButton.Click += OnAddAnimation;
        _removeAnimationButton = new Button { Text = "Remove", Width = 80 };
        _removeAnimationButton.Click += OnRemoveAnimation;
        _saveAnimationButton = new Button { Text = "Save", Width = 80, Enabled = false };
        _saveAnimationButton.Click += OnSaveAnimation;
        buttonPanel.Controls.Add(_addAnimationButton);
        buttonPanel.Controls.Add(_removeAnimationButton);
        buttonPanel.Controls.Add(_saveAnimationButton);
        listLayout.Controls.Add(buttonPanel, 0, 1);
        listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

        listGroup.Controls.Add(listLayout);
        layout.Controls.Add(listGroup, 0, 0);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

        // Animation properties group (scrollable)
        var propsGroup = new GroupBox { Text = "Animation Properties", Dock = DockStyle.Fill };
        var propsScrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var propsLayout = new TableLayoutPanel { ColumnCount = 2, Padding = new Padding(5), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        propsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        propsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        propsScrollPanel.Controls.Add(propsLayout);

        int propRow = 0;

        // Animation type
        propsLayout.Controls.Add(new Label { Text = "Type:", AutoSize = true }, 0, propRow);
        _animationTypeCombo = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        _animationTypeCombo.Items.AddRange(new[] { "Visibility", "ColorChange", "Flashing", "Translation" });
        _animationTypeCombo.SelectedIndexChanged += OnAnimationTypeChanged;
        propsLayout.Controls.Add(_animationTypeCombo, 1, propRow);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        propRow++;

        // Tag selector
        propsLayout.Controls.Add(new Label { Text = "Tag:", AutoSize = true }, 0, propRow);
        _animationTagSelector = new TagSelectorWidget { Dock = DockStyle.Fill, Height = 25 };
        _animationTagSelector.TagSelected += (s, tagName) => 
        {
            // When tag changes, update color map table if ColorChange is selected
            if (_animationTypeCombo?.SelectedItem?.ToString() == "ColorChange")
            {
                UpdateColorMapTable();
            }
        };
        propsLayout.Controls.Add(_animationTagSelector, 1, propRow);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        propRow++;

        // Bit value (for Visibility)
        _animationBitValueCheckBox = new CheckBox { Text = "Show when tag is true", Checked = true };
        propsLayout.Controls.Add(_animationBitValueCheckBox, 0, propRow);
        propsLayout.SetColumnSpan(_animationBitValueCheckBox, 2);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        propRow++;

        // Color (for Flashing only - ColorChange uses table below)
        propsLayout.Controls.Add(new Label { Text = "Color:", AutoSize = true }, 0, propRow);
        _animationColorButton = new Button { Text = "", Width = 50, Height = 25 };
        UpdateColorButton(_animationColorButton, Color.Red);
        _animationColorButton.Click += (s, e) =>
        {
            using (var colorDialog = new ColorDialog { Color = _animationColorButton.BackColor })
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    UpdateColorButton(_animationColorButton, colorDialog.Color);
                }
            }
        };
        propsLayout.Controls.Add(_animationColorButton, 1, propRow);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        propRow++;

        // Color Map Table (for ColorChange animations)
        _colorMapPanel = new Panel { Dock = DockStyle.Fill, Height = 200, Visible = false };
        var colorMapLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(0, 5, 0, 0) };
        
        var colorMapLabel = new Label { Text = "Value to Color Mapping:", AutoSize = true, Dock = DockStyle.Top };
        colorMapLayout.Controls.Add(colorMapLabel, 0, 0);
        colorMapLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        
        _colorMapTable = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        
        // Add columns: Value/Range, Color
        _colorMapTable.Columns.Add("ValueRange", "Value/Range");
        _colorMapTable.Columns.Add("Color", "Color");
        _colorMapTable.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _colorMapTable.Columns[1].Width = 100;
        
        // Make color column show color picker
        _colorMapTable.CellClick += OnColorMapCellClick;
        _colorMapTable.CellFormatting += OnColorMapCellFormatting;
        
        colorMapLayout.Controls.Add(_colorMapTable, 0, 1);
        colorMapLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        
        // Buttons for managing color map rows
        var colorMapButtonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Height = 30 };
        _addColorMapRowButton = new Button { Text = "Add Row", Width = 80 };
        _addColorMapRowButton.Click += OnAddColorMapRow;
        _removeColorMapRowButton = new Button { Text = "Remove Row", Width = 90 };
        _removeColorMapRowButton.Click += OnRemoveColorMapRow;
        colorMapButtonPanel.Controls.Add(_addColorMapRowButton);
        colorMapButtonPanel.Controls.Add(_removeColorMapRowButton);
        
        colorMapLayout.Controls.Add(colorMapButtonPanel, 0, 2);
        colorMapLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
        
        _colorMapPanel.Controls.Add(colorMapLayout);
        propsLayout.Controls.Add(_colorMapPanel, 0, propRow);
        propsLayout.SetColumnSpan(_colorMapPanel, 2);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
        propRow++;

        // Frequency (for Flashing)
        propsLayout.Controls.Add(new Label { Text = "Frequency (Hz):", AutoSize = true }, 0, propRow);
        _animationFrequencyNumeric = new NumericUpDown { Minimum = 0.1m, Maximum = 10m, DecimalPlaces = 1, Value = 1.0m, Width = 100 };
        propsLayout.Controls.Add(_animationFrequencyNumeric, 1, propRow);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        propRow++;

        // Speed (for Translation)
        propsLayout.Controls.Add(new Label { Text = "Speed (px/s):", AutoSize = true }, 0, propRow);
        _animationSpeedNumeric = new NumericUpDown { Minimum = 0.1m, Maximum = 1000m, DecimalPlaces = 1, Value = 1.0m, Width = 100 };
        propsLayout.Controls.Add(_animationSpeedNumeric, 1, propRow);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        propRow++;

        // Enabled
        _animationEnabledCheckBox = new CheckBox { Text = "Enabled", Checked = true };
        propsLayout.Controls.Add(_animationEnabledCheckBox, 0, propRow);
        propsLayout.SetColumnSpan(_animationEnabledCheckBox, 2);
        propsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        propsGroup.Controls.Add(propsScrollPanel);
        layout.Controls.Add(propsGroup, 0, 1);
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));

        _animationTab.Controls.Add(layout);
        
        // Initialize type combo
        _animationTypeCombo.SelectedIndex = 0;
        OnAnimationTypeChanged(null, EventArgs.Empty);
    }
    
    private void UpdateAnimationTab()
    {
        System.Diagnostics.Debug.WriteLine($"[PropertyEditor] UpdateAnimationTab called, component: {_selectedComponent?.Name ?? "null"}");
        
        if (_selectedComponent == null)
        {
            if (_animationsList != null)
                _animationsList.Items.Clear();
            _componentAnimations.Clear();
            return;
        }

        // Load animations from component properties
        _componentAnimations.Clear();
        if (_animationsList != null)
            _animationsList.Items.Clear();

        System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Component has {_selectedComponent.Properties.Count} properties");
        System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Properties keys: {string.Join(", ", _selectedComponent.Properties.Keys)}");

        if (_selectedComponent.Properties.ContainsKey("animations"))
        {
            try
            {
                var animationsObj = _selectedComponent.Properties["animations"];
                System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Found animations property, type: {animationsObj?.GetType().Name}");
                
                if (animationsObj is JObject animationsJson)
                {
                    var animationsArray = animationsJson["animations"] as Newtonsoft.Json.Linq.JArray;
                    if (animationsArray != null && _animationsList != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Found {animationsArray.Count} animation(s) in JSON object");
                        foreach (var item in animationsArray)
                        {
                            if (item is JObject animObj)
                            {
                                var config = AnimationConfig.FromJson(animObj);
                                _componentAnimations.Add(config);
                                _animationsList.Items.Add(config.Name);
                            }
                        }
                    }
                }
                else if (animationsObj is Newtonsoft.Json.Linq.JArray directArray && _animationsList != null)
                {
                    // Handle case where animations is stored directly as an array
                    System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Found {directArray.Count} animation(s) in direct array");
                    foreach (var item in directArray)
                    {
                        if (item is JObject animObj)
                        {
                            var config = AnimationConfig.FromJson(animObj);
                            _componentAnimations.Add(config);
                            _animationsList.Items.Add(config.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Failed to load animations, start fresh
                System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Failed to load animations: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Stack trace: {ex.StackTrace}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[PropertyEditor] No animations property found on component");
        }

        System.Diagnostics.Debug.WriteLine($"[PropertyEditor] Loaded {_componentAnimations.Count} animation(s) total");

        // Force repaint so Animation tab shows new data when user switches to it (fixes stale display for subsequent components)
        if (_animationsList != null)
        {
            _animationsList.Refresh();
        }
        _animationTab?.Invalidate(true);
    }

    private void OnAddAnimation(object? sender, EventArgs e)
    {
        if (_selectedComponent == null)
            return;

        string baseName = $"Animation {_componentAnimations.Count + 1}";
        string uniqueName = GenerateUniqueAnimationName(baseName);

        var config = new AnimationConfig
        {
            Name = uniqueName,
            Type = AnimationType.Visibility,
            TagName = string.Empty,
            BitValue = true,
            Color = "#FF0000",
            Frequency = 1.0,
            Speed = 1.0,
            Enabled = true
        };

        _componentAnimations.Add(config);
        if (_animationsList != null)
        {
            _animationsList.Items.Add(config.Name);
            _animationsList.SelectedItem = config.Name;
        }
        _currentEditingAnimation = config;
        if (_saveAnimationButton != null)
            _saveAnimationButton.Enabled = true;
    }
    
    private void OnRemoveAnimation(object? sender, EventArgs e)
    {
        if (_animationsList?.SelectedItem == null)
            return;

        string selectedName = _animationsList.SelectedItem.ToString() ?? string.Empty;
        var config = _componentAnimations.FirstOrDefault(a => a.Name == selectedName);
        if (config != null)
        {
            _componentAnimations.Remove(config);
            _animationsList.Items.Remove(selectedName);
            SaveAnimationsToComponent();
            TriggerAutoSave();
        }
    }
    
    private void OnSaveAnimation(object? sender, EventArgs e)
    {
        if (_currentEditingAnimation == null || _selectedComponent == null)
            return;

        // Update animation config from UI
        if (_animationTypeCombo != null)
        {
            if (Enum.TryParse<AnimationType>(_animationTypeCombo.SelectedItem?.ToString(), out AnimationType type))
            {
                _currentEditingAnimation.Type = type;
            }
        }

        if (_animationTagSelector != null)
        {
            _currentEditingAnimation.TagName = _animationTagSelector.SelectedTagName();
        }

        if (_animationBitValueCheckBox != null)
        {
            _currentEditingAnimation.BitValue = _animationBitValueCheckBox.Checked;
        }

        // Handle color - for Flashing use button, for ColorChange use table
        if (_currentEditingAnimation.Type == AnimationType.ColorChange && _colorMapTable != null)
        {
            // Save color map from table
            _currentEditingAnimation.ColorMap = new Dictionary<string, string>();
            foreach (DataGridViewRow row in _colorMapTable.Rows)
            {
                if (row.IsNewRow) continue;
                string? valueRange = row.Cells[0].Value?.ToString();
                string? color = row.Cells[1].Value?.ToString();
                if (!string.IsNullOrEmpty(valueRange) && !string.IsNullOrEmpty(color))
                {
                    _currentEditingAnimation.ColorMap[valueRange] = color;
                }
            }
            // Keep legacy Color property for backward compatibility (use first color or default)
            if (_currentEditingAnimation.ColorMap.Count > 0)
            {
                _currentEditingAnimation.Color = _currentEditingAnimation.ColorMap.Values.First();
            }
        }
        else if (_animationColorButton != null)
        {
            _currentEditingAnimation.Color = ColorToHex(_animationColorButton.BackColor);
        }

        if (_animationFrequencyNumeric != null)
        {
            _currentEditingAnimation.Frequency = (double)_animationFrequencyNumeric.Value;
        }

        if (_animationSpeedNumeric != null)
        {
            _currentEditingAnimation.Speed = (double)_animationSpeedNumeric.Value;
        }

        if (_animationEnabledCheckBox != null)
        {
            _currentEditingAnimation.Enabled = _animationEnabledCheckBox.Checked;
        }
        
        // Save color map from table if ColorChange animation
        if (_currentEditingAnimation.Type == AnimationType.ColorChange && _colorMapTable != null)
        {
            _currentEditingAnimation.ColorMap = new Dictionary<string, string>();
            foreach (DataGridViewRow row in _colorMapTable.Rows)
            {
                if (row.IsNewRow) continue;
                string? valueRange = row.Cells[0].Value?.ToString();
                string? color = row.Cells[1].Value?.ToString();
                if (!string.IsNullOrEmpty(valueRange) && !string.IsNullOrEmpty(color))
                {
                    _currentEditingAnimation.ColorMap[valueRange] = color;
                }
            }
        }
        else if (_animationColorButton != null && (_currentEditingAnimation.Type == AnimationType.Flashing || 
                  (_currentEditingAnimation.Type == AnimationType.ColorChange && (_currentEditingAnimation.ColorMap == null || _currentEditingAnimation.ColorMap.Count == 0))))
        {
            // For Flashing or legacy ColorChange, use single color button
            _currentEditingAnimation.Color = ColorTranslator.ToHtml(_animationColorButton.BackColor);
        }

        // Update list if name changed
        if (_animationsList != null && _animationsList.SelectedIndex >= 0)
        {
            int index = _animationsList.SelectedIndex;
            _animationsList.Items[index] = _currentEditingAnimation.Name;
        }

        SaveAnimationsToComponent();
        TriggerAutoSave();
        _saveAnimationButton!.Enabled = false;
    }
    
    private void OnAnimationSelected(object? sender, EventArgs e)
    {
        if (_animationsList?.SelectedItem == null)
        {
            _currentEditingAnimation = null;
            _saveAnimationButton!.Enabled = false;
            return;
        }

        string selectedName = _animationsList.SelectedItem.ToString() ?? string.Empty;
        _currentEditingAnimation = _componentAnimations.FirstOrDefault(a => a.Name == selectedName);
        
        if (_currentEditingAnimation != null)
        {
            LoadAnimationToUI(_currentEditingAnimation);
            _saveAnimationButton!.Enabled = true;
        }
    }
    
    private void LoadAnimationToUI(AnimationConfig config)
    {
        if (_animationTypeCombo != null)
        {
            int index = _animationTypeCombo.Items.IndexOf(config.Type.ToString());
            if (index >= 0)
                _animationTypeCombo.SelectedIndex = index;
        }

        if (_animationTagSelector != null)
        {
            _animationTagSelector.SetSelectedTagName(config.TagName);
        }

        if (_animationBitValueCheckBox != null)
        {
            _animationBitValueCheckBox.Checked = config.BitValue;
        }

        if (_animationColorButton != null)
        {
            UpdateColorButton(_animationColorButton, ParseColor(config.Color));
        }

        if (_animationFrequencyNumeric != null)
        {
            _animationFrequencyNumeric.Value = (decimal)config.Frequency;
        }

        if (_animationSpeedNumeric != null)
        {
            _animationSpeedNumeric.Value = (decimal)config.Speed;
        }

        if (_animationEnabledCheckBox != null)
        {
            _animationEnabledCheckBox.Checked = config.Enabled;
        }

        OnAnimationTypeChanged(null, EventArgs.Empty);
        
        // Load color map into table if ColorChange animation
        if (config.Type == AnimationType.ColorChange && _colorMapTable != null)
        {
            _colorMapTable.Rows.Clear();
            if (config.ColorMap != null && config.ColorMap.Count > 0)
            {
                foreach (var kvp in config.ColorMap)
                {
                    _colorMapTable.Rows.Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                // No color map, populate based on tag type
                UpdateColorMapTable();
            }
        }
    }
    
    private void OnAnimationTypeChanged(object? sender, EventArgs e)
    {
        if (_animationTypeCombo == null)
            return;

        bool isVisibility = _animationTypeCombo.SelectedItem?.ToString() == "Visibility";
        bool isColorChange = _animationTypeCombo.SelectedItem?.ToString() == "ColorChange";
        bool isFlashing = _animationTypeCombo.SelectedItem?.ToString() == "Flashing";
        bool isTranslation = _animationTypeCombo.SelectedItem?.ToString() == "Translation";

        if (_animationBitValueCheckBox != null)
            _animationBitValueCheckBox.Visible = isVisibility;
        if (_animationColorButton != null)
            _animationColorButton.Visible = isFlashing; // Only for Flashing, not ColorChange
        if (_animationFrequencyNumeric != null)
            _animationFrequencyNumeric.Visible = isFlashing;
        if (_animationSpeedNumeric != null)
            _animationSpeedNumeric.Visible = isTranslation;
        if (_colorMapPanel != null)
            _colorMapPanel.Visible = isColorChange;
        
        // When ColorChange is selected, populate table based on tag type
        if (isColorChange)
        {
            UpdateColorMapTable();
        }
    }
    
    private void UpdateColorMapTable()
    {
        if (_colorMapTable == null || _animationTagSelector == null)
            return;
        
        _colorMapTable.Rows.Clear();
        
        // Get selected tag to determine type
        string tagName = _animationTagSelector?.SelectedTagName() ?? string.Empty;
        Tag? selectedTag = GetTagByName(tagName);
        
        // Determine if tag is boolean
        bool isBoolean = false;
        if (selectedTag != null)
        {
            string? dataType = selectedTag.DataType;
            isBoolean = dataType != null && (dataType.Equals("Bit", StringComparison.OrdinalIgnoreCase) || 
                                             dataType.Equals("Boolean", StringComparison.OrdinalIgnoreCase) ||
                                             dataType.Equals("Bool", StringComparison.OrdinalIgnoreCase));
        }
        
        // Load existing color map from current animation if available (takes precedence)
        if (_currentEditingAnimation != null && _currentEditingAnimation.ColorMap != null && _currentEditingAnimation.ColorMap.Count > 0)
        {
            foreach (var kvp in _currentEditingAnimation.ColorMap)
            {
                // Convert legacy "true"/"false" to "1"/"0" for boolean tags
                string key = kvp.Key;
                if (isBoolean)
                {
                    if (key.Equals("true", StringComparison.OrdinalIgnoreCase))
                        key = "1";
                    else if (key.Equals("false", StringComparison.OrdinalIgnoreCase))
                        key = "0";
                }
                _colorMapTable.Rows.Add(key, kvp.Value);
            }
        }
        else if (selectedTag != null)
        {
            // No existing color map, populate defaults based on tag type
            if (isBoolean)
            {
                // For boolean tags, use 0 and 1 (runtime converts true->1, false->0)
                _colorMapTable.Rows.Add("1", "#00FF00"); // Green for true (1)
                _colorMapTable.Rows.Add("0", "#FF0000"); // Red for false (0)
            }
            else
            {
                // For numeric tags, add default range rows
                _colorMapTable.Rows.Add("< 0", "#FF0000"); // Red for negative
                _colorMapTable.Rows.Add("0-50", "#FFFF00"); // Yellow for low
                _colorMapTable.Rows.Add("50-100", "#00FF00"); // Green for medium
                _colorMapTable.Rows.Add("> 100", "#0000FF"); // Blue for high
            }
        }
        else
        {
            // No tag selected, add default rows
            _colorMapTable.Rows.Add("default", "#FF0000");
        }
    }
    
    private Tag? GetTagByName(string tagName)
    {
        if (string.IsNullOrEmpty(tagName) || _availableTagTables == null)
            return null;
        
        foreach (var table in _availableTagTables)
        {
            if (table == null) continue;
            var tags = table.GetTags();
            foreach (var tag in tags)
            {
                if (tag.Name == tagName)
                    return tag;
            }
        }
        return null;
    }
    
    private void OnColorMapCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_colorMapTable == null || e.RowIndex < 0 || e.ColumnIndex != 1)
            return;
        
        // Color column clicked - show color picker
        var cell = _colorMapTable.Rows[e.RowIndex].Cells[1];
        string currentColor = cell.Value?.ToString() ?? "#FF0000";
        Color currentColorObj = ParseColor(currentColor);
        
        using (var colorDialog = new ColorDialog { Color = currentColorObj })
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                string hexColor = ColorTranslator.ToHtml(colorDialog.Color);
                cell.Value = hexColor;
                _colorMapTable.InvalidateCell(e.ColumnIndex, e.RowIndex);
            }
        }
    }
    
    private void OnColorMapCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_colorMapTable == null || e.ColumnIndex != 1 || e.RowIndex < 0)
            return;
        
        // Format color column to show color preview
        string? colorHex = e.Value?.ToString();
        if (!string.IsNullOrEmpty(colorHex))
        {
            try
            {
                Color color = ParseColor(colorHex);
                e.CellStyle.BackColor = color;
                e.CellStyle.ForeColor = GetContrastColor(color);
                e.Value = colorHex; // Show hex code as text
                e.FormattingApplied = true;
            }
            catch
            {
                // Invalid color, use default
                e.CellStyle.BackColor = Color.White;
                e.CellStyle.ForeColor = Color.Black;
            }
        }
    }
    
    private void OnAddColorMapRow(object? sender, EventArgs e)
    {
        if (_colorMapTable == null)
            return;
        
        _colorMapTable.Rows.Add("new", "#FF0000");
    }
    
    private void OnRemoveColorMapRow(object? sender, EventArgs e)
    {
        if (_colorMapTable == null || _colorMapTable.SelectedRows.Count == 0)
            return;
        
        foreach (DataGridViewRow row in _colorMapTable.SelectedRows)
        {
            _colorMapTable.Rows.Remove(row);
        }
    }
    
    private void SaveAnimationsToComponent()
    {
        if (_selectedComponent == null)
            return;

        var animationsArray = new Newtonsoft.Json.Linq.JArray();
        foreach (var config in _componentAnimations)
        {
            animationsArray.Add(config.ToJson());
        }

        var animationsObj = new JObject
        {
            ["animations"] = animationsArray
        };

        _selectedComponent.Properties["animations"] = animationsObj;
    }
    
    private string GenerateUniqueAnimationName(string baseName)
    {
        // Get all animation names from current component and other components in the scene
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Add names from current component
        foreach (var anim in _componentAnimations)
        {
            if (!string.IsNullOrEmpty(anim.Name))
                usedNames.Add(anim.Name);
        }

        // Check if base name is unique
        if (!usedNames.Contains(baseName))
            return baseName;

        // Generate unique name
        int counter = 1;
        while (true)
        {
            string candidate = $"{baseName} {counter}";
            if (!usedNames.Contains(candidate))
                return candidate;
            counter++;
            
            if (counter > 1000)
                return $"{baseName} {Guid.NewGuid()}";
        }
    }
    
    private Color ParseColor(string colorHex)
    {
        try
        {
            if (colorHex.StartsWith("#"))
            {
                colorHex = colorHex.Substring(1);
            }
            
            if (colorHex.Length == 6)
            {
                int r = Convert.ToInt32(colorHex.Substring(0, 2), 16);
                int g = Convert.ToInt32(colorHex.Substring(2, 2), 16);
                int b = Convert.ToInt32(colorHex.Substring(4, 2), 16);
                return Color.FromArgb(r, g, b);
            }
        }
        catch { }
        
        return Color.Red; // Default
    }
    
    private string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void OnAddEvent(object? sender, EventArgs e)
    {
        if (_selectedComponent == null || _eventsModule == null)
            return;

        // Clear UI for new event
        _currentEditingEvent = null;
        if (_eventNameTextBox != null)
        {
            _eventNameTextBox.Text = $"Event {_eventsList.Items.Count + 1}";
        }
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
                    // Refresh the events list to show updated name
                    UpdateEventsTab();
                    // Reselect the updated event
                    for (int i = 0; i < _eventsList.Items.Count; i++)
                    {
                        if (_eventsList.Items[i] is EventListItem item && item.Event.Id == evt.Id)
                        {
                            _eventsList.SelectedIndex = i;
                            break;
                        }
                    }
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
                    
                    // Refresh the events list and select the new event
                    UpdateEventsTab();
                    for (int i = 0; i < _eventsList.Items.Count; i++)
                    {
                        if (_eventsList.Items[i] is EventListItem item && item.Event.Id == evt.Id)
                        {
                            _eventsList.SelectedIndex = i;
                            break;
                        }
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

        // Get event name from TextBox, or generate default name
        string eventName = string.Empty;
        if (_eventNameTextBox != null && !string.IsNullOrWhiteSpace(_eventNameTextBox.Text))
        {
            eventName = _eventNameTextBox.Text.Trim();
        }
        else if (_currentEditingEvent != null && !string.IsNullOrEmpty(_currentEditingEvent.Name))
        {
            eventName = _currentEditingEvent.Name;
        }
        else
        {
            eventName = $"Event {_eventsList.Items.Count + 1}";
        }

        var evt = new ScadaEvent
        {
            Id = _currentEditingEvent?.Id ?? Guid.NewGuid().ToString(),
            Name = eventName,
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
        
        // Set event name
        if (_eventNameTextBox != null)
        {
            _eventNameTextBox.Text = evt.Name ?? string.Empty;
        }
        
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
