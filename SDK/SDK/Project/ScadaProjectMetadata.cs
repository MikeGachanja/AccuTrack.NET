namespace AccuTrack.SDK.Project;

/// <summary>
/// Project metadata stored as metadata.iscr (or equivalent) for Runtime compatibility.
/// </summary>
public class ScadaProjectMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string? Description { get; set; }
}
