using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeNumeric : UserControl
{
    private int _decimalPlaces = 0;
    private string _suffix = "";

    public RuntimeNumeric()
    {
        InitializeComponent();
        // Ensure component is visible by default
        IsVisible = true;
        Opacity = 1.0;
    }

    public void ApplyDescriptor(ComponentDescriptor d)
    {
        if (d == null) return;
        var label = GetProperty(d, "label", "");
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
        _decimalPlaces = GetPropInt(d, "decimalPlaces", 0);
        _suffix = GetProperty(d, "suffix", "");

        // Designer colors and fonts (labelColor, valueColor, labelFontSize, valueFontSize)
        var labelColor = GetProperty(d, "labelColor", "#333333");
        LabelText.Foreground = ColorParser.ParseBrush(labelColor);
        var valueColor = GetProperty(d, "valueColor", "#0066cc");
        ValueText.Foreground = ColorParser.ParseBrush(valueColor);

        // Font family and style from designer (different fonts per component)
        var labelFont = GetProperty(d, "labelFont", "Arial");
        var valueFont = GetProperty(d, "valueFont", "Arial");
        LabelText.FontFamily = new FontFamily(labelFont);
        ValueText.FontFamily = new FontFamily(valueFont);

        var labelFontSize = GetPropDouble(d, "labelFontSize", 12);
        LabelText.FontSize = Math.Max(12, labelFontSize > 0 ? labelFontSize : 12);
        var valueFontSize = GetPropDouble(d, "valueFontSize", 18);
        ValueText.FontSize = Math.Max(14, valueFontSize > 0 ? valueFontSize : 18);

        var labelFontStyle = GetProperty(d, "labelFontStyle", "Regular");
        LabelText.FontWeight = labelFontStyle.Contains("Bold", System.StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        LabelText.FontStyle = labelFontStyle.Contains("Italic", System.StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;

        var valueFontStyle = GetProperty(d, "valueFontStyle", "Bold");
        ValueText.FontWeight = valueFontStyle.Contains("Bold", System.StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        ValueText.FontStyle = valueFontStyle.Contains("Italic", System.StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;

        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
        Opacity = 1.0; // Ensure full opacity
        
        // Ensure value text is visible and shows initial value (0) if SetValue hasn't been called yet
        ValueText.IsVisible = true;
        ValueText.Opacity = 1.0;
        if (string.IsNullOrEmpty(ValueText.Text))
            ValueText.Text = "0";
        
        // Ensure the component has minimum size so it's always visible (even if descriptor has 0 size)
        MinWidth = Math.Max(50, d.Width > 0 ? d.Width : 100);
        MinHeight = Math.Max(20, d.Height > 0 ? d.Height : 30);
        if (d.Width <= 0) Width = 100;
        if (d.Height <= 0) Height = 30;
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
        double v = 0;
        if (value is int i) v = i;
        else if (value is double d) v = d;
        else if (value is float f) v = f;
        else if (value != null && double.TryParse(value.ToString(), out var parsed)) v = parsed;
        // Always display the value (including 0) so component is visible
        var numStr = _decimalPlaces > 0
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, $"{{0:F{_decimalPlaces}}}", v)
            : v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ValueText.Text = numStr + _suffix;
        // Ensure ValueText is always visible when component is visible
        ValueText.IsVisible = true;
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
