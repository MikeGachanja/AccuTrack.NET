using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeAlarmView : UserControl
{
    public RuntimeAlarmView()
    {
        InitializeComponent();
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        TitleText.Text = GetProperty(d, "title", "Alarms");
        // Font from designer (headerFont, headerFontSize, headerFontStyle for title)
        var fontName = GetProperty(d, "headerFont", "Arial");
        var fontSize = GetPropDouble(d, "headerFontSize", 12);
        if (fontSize <= 0) fontSize = 12;
        var fontStyleStr = GetProperty(d, "headerFontStyle", "Bold");
        TitleText.FontFamily = new FontFamily(fontName);
        TitleText.FontSize = fontSize;
        TitleText.FontWeight = fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.SemiBold;
        TitleText.FontStyle = fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    private static string GetProperty(ComponentDescriptor d, string key, string fallback)
    {
        if (d.Properties.TryGetValue(key, out var v) && v is string s) return s;
        return fallback;
    }

    private static double GetPropDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
    }
}
