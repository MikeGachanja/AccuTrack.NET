using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeComboBox : UserControl
{
    public RuntimeComboBox()
    {
        InitializeComponent();
        TheComboBox.SelectionChanged += (_, _) => SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        
        // Apply size from descriptor - remove MinWidth constraint and use configured size
        if (d.Width > 0)
        {
            TheComboBox.Width = d.Width;
            TheComboBox.MinWidth = 0; // Remove MinWidth constraint
        }
        if (d.Height > 0)
            TheComboBox.Height = d.Height;
        
        var label = GetProperty(d, "label", "");
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
        var items = GetItems(d);
        TheComboBox.ItemsSource = items;
        if (items.Count > 0) TheComboBox.SelectedIndex = 0;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    public void SetValue(object? value)
    {
        var str = value?.ToString() ?? "";
        if (TheComboBox.ItemsSource is IEnumerable<object?> en)
        {
            var i = 0;
            foreach (var item in en)
            {
                if (item?.ToString() == str) { TheComboBox.SelectedIndex = i; return; }
                i++;
            }
        }
    }

    public event EventHandler<EventArgs>? SelectionChanged;

    public object? SelectedItem => TheComboBox.SelectedItem;
    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static List<string> GetItems(ComponentDescriptor d)
    {
        var list = new List<string>();
        if (d.Properties.TryGetValue("items", out var v))
        {
            if (v is IEnumerable<object?> arr)
                foreach (var x in arr)
                    list.Add(x?.ToString() ?? "");
            else if (v is string s && !string.IsNullOrEmpty(s))
                foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    list.Add(part);
        }
        if (list.Count == 0) list.Add("—");
        return list;
    }
}
