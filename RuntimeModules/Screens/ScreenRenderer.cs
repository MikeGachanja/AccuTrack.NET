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
            if (item.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object?>();
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
                comp.Properties = dict;
            }
            return comp;
        }
        catch
        {
            return null;
        }
    }
}
