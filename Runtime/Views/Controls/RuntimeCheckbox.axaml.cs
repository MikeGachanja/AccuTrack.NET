using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeCheckbox : UserControl
{
    public RuntimeCheckbox()
    {
        InitializeComponent();
        TheCheckBox.IsCheckedChanged += (_, _) => CheckedChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        TheCheckBox.Content = GetProperty(d, "text", d.Name);
        // Text color: use textColor first, then foreColor
        var textColor = GetProperty(d, "textColor", "");
        if (string.IsNullOrEmpty(textColor)) textColor = GetProperty(d, "foreColor", "#333333");
        TheCheckBox.Foreground = ColorParser.ParseBrush(textColor);
        // Font from designer (font, fontSize, fontStyle)
        var fontName = GetProperty(d, "font", "Arial");
        var fontSize = Math.Max(12, GetPropDouble(d, "fontSize", 12));
        if (fontSize <= 0) fontSize = 12;
        var fontStyleStr = GetProperty(d, "fontStyle", "Regular");
        TheCheckBox.FontFamily = new FontFamily(fontName);
        TheCheckBox.FontSize = fontSize;
        TheCheckBox.FontWeight = fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        TheCheckBox.FontStyle = fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
        TheCheckBox.IsEnabled = d.Enabled;
        TheCheckBox.IsVisible = d.Visible;
    }

    private static double GetPropDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }

    public void SetValue(object? value)
    {
        TheCheckBox.IsChecked = value is bool b ? b : (value is 1 or "1" or "true" || (value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true));
    }

    public event EventHandler<PointerPressedEventArgs>? MouseDown
    {
        add => TheCheckBox.PointerPressed += value;
        remove => TheCheckBox.PointerPressed -= value;
    }

    public event EventHandler<PointerReleasedEventArgs>? MouseUp
    {
        add => TheCheckBox.PointerReleased += value;
        remove => TheCheckBox.PointerReleased -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseEnter
    {
        add => TheCheckBox.PointerEntered += value;
        remove => TheCheckBox.PointerEntered -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseLeave
    {
        add => TheCheckBox.PointerExited += value;
        remove => TheCheckBox.PointerExited -= value;
    }

    public event EventHandler<Avalonia.Input.TappedEventArgs>? DoubleClick
    {
        add => TheCheckBox.DoubleTapped += value;
        remove => TheCheckBox.DoubleTapped -= value;
    }

    public event EventHandler<RoutedEventArgs>? RightClick
    {
        add => TheCheckBox.PointerPressed += (s, e) => { if (e.GetCurrentPoint(TheCheckBox).Properties.IsRightButtonPressed) value?.Invoke(s, new RoutedEventArgs()); };
        remove { } // Note: Can't easily remove anonymous handlers, but this is acceptable for event forwarding
    }

    public event EventHandler<EventArgs>? CheckedChanged;

    public string? ComponentId { get; set; }
    public string? TagName { get; set; }

    public bool? IsChecked => TheCheckBox.IsChecked;

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

}
