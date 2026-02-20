using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTextInput : UserControl
{
    public RuntimeTextInput()
    {
        InitializeComponent();
        TheTextBox.LostFocus += (_, _) => TextCommitted?.Invoke(this, EventArgs.Empty);
        TheTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) TextCommitted?.Invoke(this, EventArgs.Empty); };
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Apply size from descriptor - remove MinWidth constraint and use configured size
        if (d.Width > 0)
        {
            TheTextBox.Width = d.Width;
            TheTextBox.MinWidth = 0; // Remove MinWidth constraint
        }
        if (d.Height > 0)
            TheTextBox.Height = d.Height;
        
        TheTextBox.Watermark = GetProperty(d, "placeholder", "");

        // Text color: use textColor first, then foreColor (supports "Black" and hex)
        var textColor = GetProperty(d, "textColor", "");
        if (string.IsNullOrEmpty(textColor)) textColor = GetProperty(d, "foreColor", "#333333");
        TheTextBox.Foreground = ColorParser.ParseBrush(textColor);

        var backColor = GetProperty(d, "backColor", "");
        if (!string.IsNullOrEmpty(backColor))
            TheTextBox.Background = ColorParser.ParseBrush(backColor);
        else
            TheTextBox.Background = new SolidColorBrush(Colors.White);
        
        // Font from designer (font, fontSize, fontStyle) so each component can have different fonts
        var fontName = GetProperty(d, "font", "Arial");
        var fontSize = GetPropertyDouble(d, "fontSize", 12);
        if (fontSize <= 0) fontSize = 12;
        var fontStyleStr = GetProperty(d, "fontStyle", "Regular");
        TheTextBox.FontFamily = new FontFamily(fontName);
        TheTextBox.FontSize = fontSize;
        TheTextBox.FontWeight = fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        TheTextBox.FontStyle = fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
        
        TheTextBox.IsEnabled = d.Enabled;
        IsVisible = d.Visible;
    }

    public void SetValue(object? value)
    {
        TheTextBox.Text = value?.ToString() ?? "";
    }

    public string Text => TheTextBox.Text ?? "";

    public event EventHandler<EventArgs>? TextCommitted;

    public event EventHandler<PointerPressedEventArgs>? MouseDown
    {
        add => TheTextBox.PointerPressed += value;
        remove => TheTextBox.PointerPressed -= value;
    }

    public event EventHandler<PointerReleasedEventArgs>? MouseUp
    {
        add => TheTextBox.PointerReleased += value;
        remove => TheTextBox.PointerReleased -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseEnter
    {
        add => TheTextBox.PointerEntered += value;
        remove => TheTextBox.PointerEntered -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseLeave
    {
        add => TheTextBox.PointerExited += value;
        remove => TheTextBox.PointerExited -= value;
    }

    public event EventHandler<KeyEventArgs>? KeyPress
    {
        add => TheTextBox.KeyDown += value;
        remove => TheTextBox.KeyDown -= value;
    }

    public event EventHandler<GotFocusEventArgs>? FocusIn
    {
        add => TheTextBox.GotFocus += value;
        remove => TheTextBox.GotFocus -= value;
    }

    public event EventHandler<RoutedEventArgs>? FocusOut
    {
        add => TheTextBox.LostFocus += value;
        remove => TheTextBox.LostFocus -= value;
    }

    public string? ComponentId { get; set; }
    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
    
    private static double GetPropertyDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }
    
}
