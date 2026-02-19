using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Runtime.Modules.Screens;

/// <summary>Parses screen JSON and returns component descriptors for the host to render.</summary>
public static class ScreenRenderer
{
    public sealed class ScreenDescriptor
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Width { get; set; } = 1920;
        public int Height { get; set; } = 1080;
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public List<ComponentDescriptor> Components { get; set; } = new();
    }

    /// <summary>Parse screen JSON file and return screen + component descriptors.</summary>
    public static ScreenDescriptor? ParseScreen(string jsonPath)
    {
        if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
            return null;
        try
        {
            var json = File.ReadAllText(jsonPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var screen = new ScreenDescriptor();
            screen.Id = root.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
            screen.Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            screen.BackgroundColor = root.TryGetProperty("backgroundColor", out var bg) ? bg.GetString() ?? "#FFFFFF" : "#FFFFFF";
            if (root.TryGetProperty("size", out var size))
            {
                screen.Width = size.TryGetProperty("width", out var w) ? w.GetInt32() : 1920;
                screen.Height = size.TryGetProperty("height", out var h) ? h.GetInt32() : 1080;
            }
            if (root.TryGetProperty("components", out var compArr) && compArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in compArr.EnumerateArray())
                {
                    var comp = ParseComponent(item);
                    if (comp != null)
                        screen.Components.Add(comp);
                }
            }
            return screen;
        }
        catch
        {
            return null;
        }
    }

    private static ComponentDescriptor? ParseComponent(JsonElement item)
    {
        try
        {
            var comp = new ComponentDescriptor
            {
                Id = item.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                ComponentType = item.TryGetProperty("componentType", out var ct) ? ct.GetString() ?? "" : "",
                Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Visible = item.TryGetProperty("visible", out var vis) ? vis.GetBoolean() : true,
                Enabled = item.TryGetProperty("enabled", out var en) ? en.GetBoolean() : true,
                ZOrder = item.TryGetProperty("zOrder", out var z) ? z.GetInt32() : 0,
                TagName = item.TryGetProperty("tagName", out var tag) ? tag.GetString() ?? "" : ""
            };
            if (item.TryGetProperty("location", out var loc))
            {
                comp.X = loc.TryGetProperty("x", out var x) ? x.GetInt32() : 0;
                comp.Y = loc.TryGetProperty("y", out var y) ? y.GetInt32() : 0;
            }
            if (item.TryGetProperty("size", out var sz))
            {
                comp.Width = sz.TryGetProperty("width", out var w) ? w.GetInt32() : 100;
                comp.Height = sz.TryGetProperty("height", out var h) ? h.GetInt32() : 30;
            }
            var dict = new Dictionary<string, object?>();
            
            // Load properties from properties object
            if (item.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in props.EnumerateObject())
                {
                    object? val = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.TryGetInt32(out var i) ? i : prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.ToString()
                    };
                    dict[prop.Name] = val;
                }
            }
            
            // Also check root level for backward compatibility (designer writes many props at root, e.g. fillColor, lineColor)
            // Only add if not already in properties dict
            var rootLevelProperties = new[] {
                "text", "backColor", "foreColor", "textColor", "borderColor", "borderWidth", "font", "fontSize", "fontStyle", "action",
                "maxLines",
                "value", "minimum", "maximum", "needleColor", "textColor", "showValue", "showMinMax", "unit",
                "level", "tankColor", "fillColor", "lowLevelColor", "highLevelColor", "lowLevelThreshold", "highLevelThreshold", "label", "showLevel",
                "onColor", "offColor", "faultColor", "warningColor", "errorColor", "color", "backgroundColor", "running", "faulted", "state", "direction", "shape",
                "labelColor", "valueColor", "labelFont", "labelFontSize", "labelFontStyle", "valueFont", "valueFontSize", "valueFontStyle", "decimalPlaces", "suffix",
                "lineColor", "lineWidth", "style", "startPoint", "endPoint", "filled",
                "headerFont", "headerFontSize", "headerFontStyle", "rowFont", "rowFontSize", "rowFontStyle", "title", "format",
                "svgPath"
            };
            foreach (var propName in rootLevelProperties)
            {
                if (!dict.ContainsKey(propName) && item.TryGetProperty(propName, out var rootProp))
                {
                    object? val = rootProp.ValueKind switch
                    {
                        JsonValueKind.String => rootProp.GetString(),
                        JsonValueKind.Number => rootProp.TryGetInt32(out var i) ? i : rootProp.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => rootProp.ToString()
                    };
                    dict[propName] = val;
                    
                    // Log svgPath parsing for debugging
                    if (propName == "svgPath" && comp.ComponentType == "SVGView")
                    {
                        System.Diagnostics.Trace.WriteLine($"[ScreenRenderer] Parsed svgPath for {comp.ComponentType} (ID: {comp.Id}): '{val}'");
                    }
                }
            }
            
            comp.Properties = dict;
            
            // Log all properties for SVG components to help debug
            if (comp.ComponentType == "SVGView")
            {
                System.Diagnostics.Trace.WriteLine($"[ScreenRenderer] SVGView component (ID: {comp.Id}) properties count: {dict.Count}");
                if (dict.ContainsKey("svgPath"))
                {
                    System.Diagnostics.Trace.WriteLine($"[ScreenRenderer] ✓ svgPath found in properties: '{dict["svgPath"]}'");
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"[ScreenRenderer] ✗ svgPath NOT found in properties");
                    System.Diagnostics.Trace.WriteLine($"[ScreenRenderer] Available properties: {string.Join(", ", dict.Keys)}");
                }
            }
            
            return comp;
        }
        catch
        {
            return null;
        }
    }
}
