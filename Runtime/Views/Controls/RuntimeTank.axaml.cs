using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTank : UserControl
{
    private double _percent;

    public RuntimeTank()
    {
        InitializeComponent();
        LayoutUpdated += (_, _) => UpdateFill();
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
        _percent = 0;
        if (value is int i) _percent = Math.Clamp(i, 0, 100);
        else if (value is double d) _percent = Math.Clamp(d, 0, 100);
        else if (value != null && double.TryParse(value.ToString(), out var parsed)) _percent = Math.Clamp(parsed, 0, 100);
        UpdateFill();
    }

    private void UpdateFill()
    {
        var h = TankBorder.Bounds.Height;
        if (h > 0) FillBorder.Height = h * _percent / 100.0;
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
