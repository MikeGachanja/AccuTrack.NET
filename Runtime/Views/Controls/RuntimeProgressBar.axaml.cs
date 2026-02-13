using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeProgressBar : UserControl
{
    public RuntimeProgressBar()
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
        
        // Apply min/max if specified
        if (GetPropDouble(d, "minimum", out var min))
            TheProgressBar.Minimum = min;
        if (GetPropDouble(d, "maximum", out var max))
            TheProgressBar.Maximum = max;
        
        var label = GetProperty(d, "label", "");
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }
    
    private static bool GetPropDouble(ComponentDescriptor desc, string key, out double result)
    {
        result = 0;
        if (!desc.Properties.TryGetValue(key, out var v)) return false;
        if (v is int i) { result = i; return true; }
        if (v is double dbl) { result = dbl; return true; }
        if (v is float f) { result = f; return true; }
        return v != null && double.TryParse(v.ToString(), out result);
    }

    public void SetValue(object? value)
    {
        double v = 0;
        if (value is int i) v = i;
        else if (value is double d) v = d;
        else if (value is float f) v = f;
        else if (value != null && double.TryParse(value.ToString(), out var parsed)) v = parsed;
        TheProgressBar.Value = Math.Clamp(v, TheProgressBar.Minimum, TheProgressBar.Maximum);
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
