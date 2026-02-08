namespace AccuTrack.Components;

/// <summary>
/// Component type definitions for the palette (align with Runtime Avalonia components).
/// </summary>
public static class ComponentTypes
{
    public static IReadOnlyList<ComponentTypeDefinition> All { get; } = new[]
    {
        new ComponentTypeDefinition("Button", 100, 32),
        new ComponentTypeDefinition("Indicator", 40, 40),
        new ComponentTypeDefinition("Tank", 80, 120),
        new ComponentTypeDefinition("Gauge", 120, 120),
        new ComponentTypeDefinition("Motor", 60, 60),
        new ComponentTypeDefinition("Pump", 60, 60),
        new ComponentTypeDefinition("TrendView", 300, 200),
        new ComponentTypeDefinition("AlarmView", 280, 150),
        new ComponentTypeDefinition("TextLabel", 120, 24),
        new ComponentTypeDefinition("Numeric", 80, 24),
        new ComponentTypeDefinition("CheckBox", 120, 24),
        new ComponentTypeDefinition("ProgressBar", 200, 24),
        new ComponentTypeDefinition("Slider", 200, 32),
        new ComponentTypeDefinition("Rectangle", 100, 80),
        new ComponentTypeDefinition("Line", 100, 4),
        new ComponentTypeDefinition("Image", 64, 64),
    };
}

public class ComponentTypeDefinition
{
    public string Type { get; }
    public int DefaultWidth { get; }
    public int DefaultHeight { get; }

    public ComponentTypeDefinition(string type, int defaultWidth, int defaultHeight)
    {
        Type = type;
        DefaultWidth = defaultWidth;
        DefaultHeight = defaultHeight;
    }
}
