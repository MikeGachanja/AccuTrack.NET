namespace Runtime.Modules.Models;

/// <summary>Runs inference for a single loaded ML model instance. Implement this in model DLLs for discovery and loading by MLEngine.</summary>
public interface IMLModelRunner
{
    string ModelId { get; }
    string OutputTagName { get; }
    IReadOnlyList<string> InputTagNames { get; }
    float Predict(IReadOnlyList<float> inputValues);
}
