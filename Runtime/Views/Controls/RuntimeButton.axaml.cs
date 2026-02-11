using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeButton : UserControl
{
    public RuntimeButton()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        TheButton.Content = GetProperty(d, "text", d.Name);
        TheButton.IsEnabled = d.Enabled;
        TheButton.IsVisible = d.Visible;
    }

    public event EventHandler<RoutedEventArgs>? Click
    {
        add => TheButton.Click += value;
        remove => TheButton.Click -= value;
    }

    public string? ComponentId { get; set; }
    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
