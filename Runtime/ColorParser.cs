using Avalonia.Media;

namespace Runtime;

/// <summary>Parses color strings (hex or .NET named colors) for runtime controls.</summary>
public static class ColorParser
{
    /// <summary>Parse a color string (e.g. "Black", "#000000", "White") to a brush. Handles named colors so designer "textColor": "Black" is readable.</summary>
    public static IBrush ParseBrush(string? colorOrHex)
    {
        if (string.IsNullOrWhiteSpace(colorOrHex)) return new SolidColorBrush(Colors.Gray);
        var s = colorOrHex.Trim();

        // Named colors (designer may serialize "Black" instead of "#000000")
        var c = TryParseNamedColor(s);
        if (c.HasValue) return new SolidColorBrush(c.Value);

        // Hex with or without #
        if (!s.StartsWith("#")) s = "#" + s;
        if (s.Length >= 7)
        {
            try
            {
                var r = System.Convert.ToInt32(s.Substring(1, 2), 16);
                var g = System.Convert.ToInt32(s.Substring(3, 2), 16);
                var b = System.Convert.ToInt32(s.Substring(5, 2), 16);
                return new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));
            }
            catch { }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    /// <summary>Parse to Color (for controls that use ParseColor).</summary>
    public static Color ParseColor(string? colorOrHex)
    {
        if (string.IsNullOrWhiteSpace(colorOrHex)) return Colors.Gray;
        var s = colorOrHex.Trim();
        var c = TryParseNamedColor(s);
        if (c.HasValue) return c.Value;
        if (!s.StartsWith("#")) s = "#" + s;
        if (s.Length >= 7)
        {
            try
            {
                var r = System.Convert.ToInt32(s.Substring(1, 2), 16);
                var g = System.Convert.ToInt32(s.Substring(3, 2), 16);
                var b = System.Convert.ToInt32(s.Substring(5, 2), 16);
                return Color.FromRgb((byte)r, (byte)g, (byte)b);
            }
            catch { }
        }
        return Colors.Gray;
    }

    private static Color? TryParseNamedColor(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "black" => Colors.Black,
            "white" => Colors.White,
            "gray" or "grey" => Colors.Gray,
            "red" => Colors.Red,
            "green" => Colors.Green,
            "blue" => Colors.Blue,
            "yellow" => Colors.Yellow,
            "cyan" => Colors.Cyan,
            "magenta" => Colors.Magenta,
            "orange" => Color.FromRgb(255, 165, 0),
            "purple" => Color.FromRgb(128, 0, 128),
            "brown" => Color.FromRgb(165, 42, 42),
            "darkgray" or "darkgrey" => Colors.DarkGray,
            "lightgray" or "lightgrey" => Colors.LightGray,
            "transparent" => Colors.Transparent,
            _ => null
        };
    }
}
