using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTrendView : UserControl
{
    public RuntimeTrendView()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        LabelText.Text = GetProperty(d, "label", "Trend");
        TagText.Text = string.IsNullOrEmpty(d.TagName) ? "" : "Tag: " + d.TagName;
        TagText.IsVisible = !string.IsNullOrEmpty(d.TagName);
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
