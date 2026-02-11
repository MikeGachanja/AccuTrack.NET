using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimePump : UserControl
{
    private static readonly SolidColorBrush OffBrush = new(Color.FromRgb(80, 80, 80));
    private static readonly SolidColorBrush OnBrush = new(Color.FromRgb(0, 150, 255));

    public RuntimePump()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        LabelText.Text = GetProperty(d, "label", "Pump");
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    public void SetValue(object? value)
    {
        var on = value is bool b ? b : (value is 1 or "1" || (value?.ToString()?.Equals("1", StringComparison.Ordinal) == true));
        TheEllipse.Fill = on ? OnBrush : OffBrush;
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
