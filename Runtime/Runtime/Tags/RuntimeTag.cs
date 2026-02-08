namespace AccuTrack.Runtime.Tags;

/// <summary>
/// In-memory tag with name, address, data type, and value.
/// </summary>
public class RuntimeTag
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string DataType { get; set; } = "Double";
    private object? _value;

    public object? Value
    {
        get => _value;
        set => _value = value;
    }

    public DateTime LastUpdated { get; set; }
    public string? Quality { get; set; }
}
