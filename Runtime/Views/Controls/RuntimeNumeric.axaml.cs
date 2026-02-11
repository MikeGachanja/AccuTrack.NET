using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeNumeric : UserControl
{
    private int _decimalPlaces = 0;
    private string _suffix = "";

    public RuntimeNumeric()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        var label = GetProperty(d, "label", "");
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
        _decimalPlaces = GetPropInt(d, "decimalPlaces", 0);
        _suffix = GetProperty(d, "suffix", "");
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    public void SetValue(object? value)
    {
        double v = 0;
        if (value is int i) v = i;
        else if (value is double d) v = d;
        else if (value is float f) v = f;
        else if (value != null && double.TryParse(value.ToString(), out var parsed)) v = parsed;
        var numStr = _decimalPlaces > 0
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, $"{{0:F{_decimalPlaces}}}", v)
            : v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ValueText.Text = numStr + _suffix;
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static int GetPropInt(ComponentDescriptor d, string key, int fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        return int.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }
}
