using System.Globalization;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeDateTime : UserControl
{
    private string _format = "yyyy-MM-dd HH:mm:ss";

    public RuntimeDateTime()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        var label = GetProperty(d, "label", "");
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
        _format = GetProperty(d, "format", _format);
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    public void SetValue(object? value)
    {
        if (value is DateTime dt)
        {
            ValueText.Text = dt.ToString(_format, CultureInfo.InvariantCulture);
            return;
        }
        if (value is DateTimeOffset dto)
        {
            ValueText.Text = dto.ToString(_format, CultureInfo.InvariantCulture);
            return;
        }
        if (value != null && DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            ValueText.Text = parsed.ToString(_format, CultureInfo.InvariantCulture);
            return;
        }
        ValueText.Text = DateTime.Now.ToString(_format, CultureInfo.InvariantCulture);
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
