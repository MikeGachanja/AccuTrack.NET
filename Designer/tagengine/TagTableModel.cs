namespace AccuTrack.TagEngine;

/// <summary>
/// Tag table model: list of tags (name, address, data type, scaling, etc.).
/// Save to project JSON (tags.json) for Runtime.
/// </summary>
public class TagTableModel
{
    public string Name { get; set; } = string.Empty;
    public List<TagEntry> Tags { get; set; } = new();
}

public class TagEntry
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string DataType { get; set; } = "Float";
    public double Scale { get; set; } = 1.0;
    public double Offset { get; set; }
}
