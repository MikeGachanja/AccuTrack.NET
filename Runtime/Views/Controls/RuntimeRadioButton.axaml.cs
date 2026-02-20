using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeRadioButton : UserControl
{
    public RuntimeRadioButton()
    {
        InitializeComponent();
        TheRadio.IsCheckedChanged += (_, _) => CheckedChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        TheRadio.Content = GetProperty(d, "text", d.Name);
        // Text color: use textColor first, then foreColor
        var textColor = GetProperty(d, "textColor", "");
        if (string.IsNullOrEmpty(textColor)) textColor = GetProperty(d, "foreColor", "#333333");
        TheRadio.Foreground = ColorParser.ParseBrush(textColor);
        // Font from designer (font, fontSize, fontStyle)
        var fontName = GetProperty(d, "font", "Arial");
        var fontSize = Math.Max(12, GetPropDouble(d, "fontSize", 12));
        if (fontSize <= 0) fontSize = 12;
        var fontStyleStr = GetProperty(d, "fontStyle", "Regular");
        TheRadio.FontFamily = new FontFamily(fontName);
        TheRadio.FontSize = fontSize;
        TheRadio.FontWeight = fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        TheRadio.FontStyle = fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
        TheRadio.IsEnabled = d.Enabled;
        IsVisible = d.Visible;
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
        TheRadio.IsChecked = value is bool b ? b : (value is 1 or "1" || (value?.ToString()?.Equals("1", StringComparison.Ordinal) == true));
    }

    public event EventHandler<PointerPressedEventArgs>? MouseDown
    {
        add => TheRadio.PointerPressed += value;
        remove => TheRadio.PointerPressed -= value;
    }

    public event EventHandler<PointerReleasedEventArgs>? MouseUp
    {
        add => TheRadio.PointerReleased += value;
        remove => TheRadio.PointerReleased -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseEnter
    {
        add => TheRadio.PointerEntered += value;
        remove => TheRadio.PointerEntered -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseLeave
    {
        add => TheRadio.PointerExited += value;
        remove => TheRadio.PointerExited -= value;
    }

    public event EventHandler<Avalonia.Input.TappedEventArgs>? DoubleClick
    {
        add => TheRadio.DoubleTapped += value;
        remove => TheRadio.DoubleTapped -= value;
    }

    public event EventHandler<RoutedEventArgs>? RightClick
    {
        add => TheRadio.PointerPressed += (s, e) => { if (e.GetCurrentPoint(TheRadio).Properties.IsRightButtonPressed) value?.Invoke(s, new RoutedEventArgs()); };
        remove { } // Note: Can't easily remove anonymous handlers, but this is acceptable for event forwarding
    }

    public event EventHandler<EventArgs>? CheckedChanged;

    public bool? IsChecked => TheRadio.IsChecked;
    public string? ComponentId { get; set; }
    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

}
