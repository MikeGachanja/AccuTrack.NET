namespace AccuTrack.ScreenEditor;

/// <summary>
/// Screen model: name, size, list of component instances (type, position, size, properties).
/// Serialize to JSON for compiler and Runtime.
/// </summary>
public class ScreenTemplate
{
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public List<ComponentInstance> Components { get; set; } = new();
}

public class ComponentInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
