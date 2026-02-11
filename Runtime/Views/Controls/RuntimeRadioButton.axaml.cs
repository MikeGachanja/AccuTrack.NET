using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        TheRadio.IsEnabled = d.Enabled;
        IsVisible = d.Visible;
    }

    public void SetValue(object? value)
    {
        TheRadio.IsChecked = value is bool b ? b : (value is 1 or "1" || (value?.ToString()?.Equals("1", StringComparison.Ordinal) == true));
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
