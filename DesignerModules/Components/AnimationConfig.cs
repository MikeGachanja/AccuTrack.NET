using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Components;

/// <summary>
/// Represents an animation configuration for a component.
/// </summary>
public class AnimationConfig
{
    public string Name { get; set; } = string.Empty;
    public AnimationType Type { get; set; } = AnimationType.Visibility;
    public string TagName { get; set; } = string.Empty;
    public bool BitValue { get; set; } = true; // For Visibility animation
    public string Color { get; set; } = "#FF0000"; // For ColorChange/Flashing (legacy single color)
    
    // For ColorChange animations: maps value/range to color
    // Key format: "true"/"false" for boolean, "value" or "min-max" for numeric ranges
    public Dictionary<string, string> ColorMap { get; set; } = new Dictionary<string, string>();
    
    public double Frequency { get; set; } = 1.0; // For Flashing (Hz)
    public double Speed { get; set; } = 1.0; // For Translation (pixels/second)
    public bool Enabled { get; set; } = true;

    public JObject ToJson()
    {
        var json = new JObject
        {
            ["name"] = Name,
            ["type"] = Type.ToString(),
            ["tagName"] = TagName,
            ["bitValue"] = BitValue,
            ["color"] = Color, // Keep for backward compatibility
            ["frequency"] = Frequency,
            ["speed"] = Speed,
            ["enabled"] = Enabled
        };
        
        // Add color map if it exists (for ColorChange animations)
        if (ColorMap != null && ColorMap.Count > 0)
        {
            var colorMapObj = new JObject();
            foreach (var kvp in ColorMap)
            {
                colorMapObj[kvp.Key] = kvp.Value;
            }
            json["colorMap"] = colorMapObj;
        }
        
        return json;
    }

    public static AnimationConfig FromJson(JObject json)
    {
        var config = new AnimationConfig
        {
            Name = json["name"]?.ToString() ?? string.Empty,
            TagName = json["tagName"]?.ToString() ?? string.Empty,
            BitValue = json["bitValue"]?.ToObject<bool>() ?? true,
            Color = json["color"]?.ToString() ?? "#FF0000",
            Frequency = json["frequency"]?.ToObject<double>() ?? 1.0,
            Speed = json["speed"]?.ToObject<double>() ?? 1.0,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true
        };

        if (Enum.TryParse<AnimationType>(json["type"]?.ToString(), out AnimationType type))
        {
            config.Type = type;
        }
        
        // Load color map if it exists (for ColorChange animations)
        if (json["colorMap"] is JObject colorMapObj)
        {
            config.ColorMap = new Dictionary<string, string>();
            foreach (var prop in colorMapObj.Properties())
            {
                config.ColorMap[prop.Name] = prop.Value?.ToString() ?? "#FF0000";
            }
        }
        // If no colorMap but has color, use it as default for backward compatibility
        else if (!string.IsNullOrEmpty(config.Color) && config.Type == AnimationType.ColorChange)
        {
            config.ColorMap["default"] = config.Color;
        }

        return config;
    }
}

/// <summary>
/// Animation types supported by components.
/// </summary>
public enum AnimationType
{
    Visibility,
    ColorChange,
    Flashing,
    Translation
}
