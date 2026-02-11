using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeImageView : UserControl
{
    public RuntimeImageView()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    /// <summary>Set image from full file path. Clears if path is null or load fails.</summary>
    public void SetSourcePath(string? fullPath)
    {
        if (string.IsNullOrEmpty(fullPath) || !System.IO.File.Exists(fullPath))
        {
            TheImage.Source = null;
            return;
        }
        try
        {
            TheImage.Source = new Bitmap(fullPath);
        }
        catch
        {
            TheImage.Source = null;
        }
    }

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
