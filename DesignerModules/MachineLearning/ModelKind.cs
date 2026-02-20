using System.Collections.Generic;

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

    /// <summary>Category for filtering, e.g. "QualityAssurance", "PredictiveMaintenance", "AnomalyDetection".</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Number of numeric inputs, or -1 for flexible (from tag count).</summary>
    public int InputSchema { get; set; } = -1;

    /// <summary>Output schema: "SingleNumeric", "SingleLabel", etc.</summary>
    public string OutputSchema { get; set; } = "SingleNumeric";

    /// <summary>Format of trained data file, e.g. "ML.NET.zip".</summary>
    public string TrainedDataFormat { get; set; } = "ML.NET.zip";

    /// <summary>Source of this kind: BuiltIn, Installed, Local.</summary>
    public string Source { get; set; } = "BuiltIn";

    /// <summary>Optional parameter schema for hyperparameters and model-specific settings (Tab 3).</summary>
    public List<ModelKindParameterDef> ParameterSchema { get; set; } = new List<ModelKindParameterDef>();
}

/// <summary>
/// Definition of a single parameter in a model kind's parameter schema (e.g. data cleaning method, window size).
/// </summary>
public class ModelKindParameterDef
{
    /// <summary>Parameter key stored in MLModel.Parameters.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display label for UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Type: "choice", "number", "integer", "string", "boolean".</summary>
    public string Type { get; set; } = "string";

    /// <summary>For "choice" type: option values.</summary>
    public List<string> Options { get; set; } = new List<string>();

    /// <summary>Default value (string, number, or boolean).</summary>
    public object? Default { get; set; }

    /// <summary>Minimum value for number/integer (optional).</summary>
    public double? Min { get; set; }

    /// <summary>Maximum value for number/integer (optional).</summary>
    public double? Max { get; set; }
}
