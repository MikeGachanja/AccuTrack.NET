using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeSpinner : UserControl
{
    public RuntimeSpinner()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Apply size from descriptor
        if (d.Height > 0)
            TheProgressBar.Height = d.Height;
        if (d.Width > 0)
            TheProgressBar.Width = d.Width;
        
        var label = GetProperty(d, "label", "");
        LabelText.Text = string.IsNullOrEmpty(label) ? "Loading..." : label;
        LabelText.IsVisible = true;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
