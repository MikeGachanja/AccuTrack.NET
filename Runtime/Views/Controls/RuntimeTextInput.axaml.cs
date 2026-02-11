using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeTextInput : UserControl
{
    public RuntimeTextInput()
    {
        InitializeComponent();
        TheTextBox.LostFocus += (_, _) => TextCommitted?.Invoke(this, EventArgs.Empty);
        TheTextBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) TextCommitted?.Invoke(this, EventArgs.Empty); };
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        TheTextBox.Watermark = GetProperty(d, "placeholder", "");
        TheTextBox.IsEnabled = d.Enabled;
        IsVisible = d.Visible;
    }

    public void SetValue(object? value)
    {
        TheTextBox.Text = value?.ToString() ?? "";
    }

    public string Text => TheTextBox.Text ?? "";

    public event EventHandler<EventArgs>? TextCommitted;

    public string? TagName { get; set; }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }
}
