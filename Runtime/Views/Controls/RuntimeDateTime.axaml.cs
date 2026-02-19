using System.Globalization;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
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

        // Font from designer; minimum 12pt so text is readable
        var fontName = GetProperty(d, "font", "Arial");
        var fontSize = Math.Max(12, GetPropDouble(d, "fontSize", 12));
        if (fontSize <= 0) fontSize = 12;
        var fontStyleStr = GetProperty(d, "fontStyle", "Regular");
        var fontWeight = fontStyleStr.Contains("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        var fontStyle = fontStyleStr.Contains("Italic", StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;
        var textColor = GetProperty(d, "textColor", "");
        if (string.IsNullOrEmpty(textColor)) textColor = GetProperty(d, "foreColor", "#333333");
        var brush = new SolidColorBrush(ColorParser.ParseColor(textColor));

        ValueText.FontFamily = new FontFamily(fontName);
        ValueText.FontSize = fontSize;
        ValueText.FontWeight = fontWeight;
        ValueText.FontStyle = fontStyle;
        ValueText.Foreground = brush;
        LabelText.FontFamily = new FontFamily(fontName);
        LabelText.FontSize = Math.Max(10, fontSize * 0.7);
        LabelText.FontWeight = fontWeight;
        LabelText.FontStyle = fontStyle;
        LabelText.Foreground = brush;

        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    private static double GetPropDouble(ComponentDescriptor d, string key, double fallback)
    {
        if (!d.Properties.TryGetValue(key, out var v)) return fallback;
        if (v is int i) return i;
        if (v is double dbl) return dbl;
        if (v is float f) return f;
        return double.TryParse(v?.ToString(), out var parsed) ? parsed : fallback;
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
