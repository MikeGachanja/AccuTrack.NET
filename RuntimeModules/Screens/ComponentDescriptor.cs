using System.Collections.Generic;

namespace Runtime.Modules.Screens;

/// <summary>Runtime descriptor for a screen component (from screen JSON). Used by host to build Avalonia controls.</summary>
public sealed class ComponentDescriptor
{
    public string Id { get; set; } = "";
    public string ComponentType { get; set; } = "";
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int ZOrder { get; set; }
    public string TagName { get; set; } = "";
    public IReadOnlyDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}
