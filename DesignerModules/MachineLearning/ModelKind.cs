namespace Designer.Modules.MachineLearning;

/// <summary>
/// Represents a standard ML model kind (algorithm/structure), not a trained artifact.
/// </summary>
public class ModelKind
{
    /// <summary>Unique identifier, e.g. "FastForestRegression".</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name for UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Short description for UI and Package Manager.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Number of numeric inputs, or -1 for flexible (from tag count).</summary>
    public int InputSchema { get; set; } = -1;

    /// <summary>Output schema: "SingleNumeric", "SingleLabel", etc.</summary>
    public string OutputSchema { get; set; } = "SingleNumeric";

    /// <summary>Format of trained data file, e.g. "ML.NET.zip".</summary>
    public string TrainedDataFormat { get; set; } = "ML.NET.zip";

    /// <summary>Source of this kind: BuiltIn, Installed, Local.</summary>
    public string Source { get; set; } = "BuiltIn";
}
