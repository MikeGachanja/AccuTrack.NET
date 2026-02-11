using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        TheCheckBox.IsEnabled = d.Enabled;
        TheCheckBox.IsVisible = d.Visible;
    }

    public void SetValue(object? value)
    {
        TheCheckBox.IsChecked = value is bool b ? b : (value is 1 or "1" or "true" || (value?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true));
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
