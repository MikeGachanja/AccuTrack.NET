using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Runtime.Modules.Screens;

namespace Runtime.Views.Controls;

public partial class RuntimeNumeric : UserControl
{
    private int _decimalPlaces = 0;
    private string _suffix = "";

    public RuntimeNumeric()
    {
        InitializeComponent();
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
        LabelText.Foreground = ParseBrush(labelColor);
        var valueColor = GetProperty(d, "valueColor", "#0066cc");
        ValueText.Foreground = ParseBrush(valueColor);

        // Font family and style from designer (different fonts per component)
        var labelFont = GetProperty(d, "labelFont", "Arial");
        var valueFont = GetProperty(d, "valueFont", "Arial");
        LabelText.FontFamily = new FontFamily(labelFont);
        ValueText.FontFamily = new FontFamily(valueFont);

        var labelFontSize = GetPropDouble(d, "labelFontSize", 11);
        LabelText.FontSize = labelFontSize > 0 ? labelFontSize : 11;
        var valueFontSize = GetPropDouble(d, "valueFontSize", 18);
        ValueText.FontSize = valueFontSize > 0 ? valueFontSize : 18;

        var labelFontStyle = GetProperty(d, "labelFontStyle", "Regular");
        LabelText.FontWeight = labelFontStyle.Contains("Bold", System.StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        LabelText.FontStyle = labelFontStyle.Contains("Italic", System.StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;

        var valueFontStyle = GetProperty(d, "valueFontStyle", "Bold");
        ValueText.FontWeight = valueFontStyle.Contains("Bold", System.StringComparison.OrdinalIgnoreCase) ? FontWeight.Bold : FontWeight.Normal;
        ValueText.FontStyle = valueFontStyle.Contains("Italic", System.StringComparison.OrdinalIgnoreCase) ? FontStyle.Italic : FontStyle.Normal;

        IsVisible = d.Visible;
        IsEnabled = d.Enabled;
    }

    private static IBrush ParseBrush(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return new SolidColorBrush(Colors.Black);
        if (!hex.StartsWith("#")) hex = "#" + hex;
        if (hex.Length >= 7)
        {
            try
            {
                var r = System.Convert.ToInt32(hex.Substring(1, 2), 16);
                var g = System.Convert.ToInt32(hex.Substring(3, 2), 16);
                var b = System.Convert.ToInt32(hex.Substring(5, 2), 16);
                return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
            catch { }
        }
        return new SolidColorBrush(Colors.Black);
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
        var numStr = _decimalPlaces > 0
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, $"{{0:F{_decimalPlaces}}}", v)
            : v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ValueText.Text = numStr + _suffix;
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
