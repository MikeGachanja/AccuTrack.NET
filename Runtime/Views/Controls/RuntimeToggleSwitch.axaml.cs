using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeToggleSwitch : UserControl
{
    public RuntimeToggleSwitch()
    {
        InitializeComponent();
        TheSwitch.IsCheckedChanged += (_, _) => Toggled?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        var label = GetProperty(d, "label", "");
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    public void SetValue(object? value)
    {
        TheSwitch.IsChecked = value is bool b ? b : (value is 1 or "1" || (value?.ToString()?.Equals("1", StringComparison.Ordinal) == true));
    }

    public event EventHandler<PointerPressedEventArgs>? MouseDown
    {
        add => TheSwitch.PointerPressed += value;
        remove => TheSwitch.PointerPressed -= value;
    }

    public event EventHandler<PointerReleasedEventArgs>? MouseUp
    {
        add => TheSwitch.PointerReleased += value;
        remove => TheSwitch.PointerReleased -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseEnter
    {
        add => TheSwitch.PointerEntered += value;
        remove => TheSwitch.PointerEntered -= value;
    }

    public event EventHandler<PointerEventArgs>? MouseLeave
    {
        add => TheSwitch.PointerExited += value;
        remove => TheSwitch.PointerExited -= value;
    }

    public event EventHandler<Avalonia.Input.TappedEventArgs>? DoubleClick
    {
        add => TheSwitch.DoubleTapped += value;
        remove => TheSwitch.DoubleTapped -= value;
    }

    public event EventHandler<RoutedEventArgs>? RightClick
    {
        add => TheSwitch.PointerPressed += (s, e) => { if (e.GetCurrentPoint(TheSwitch).Properties.IsRightButtonPressed) value?.Invoke(s, new RoutedEventArgs()); };
        remove { } // Note: Can't easily remove anonymous handlers, but this is acceptable for event forwarding
    }

    public event EventHandler<EventArgs>? Toggled;

    public bool? IsChecked => TheSwitch.IsChecked;
    public string? ComponentId { get; set; }
    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
