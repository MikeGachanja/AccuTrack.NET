namespace Runtime.Modules.MLEngine;

/// <summary>Runs inference for a single loaded ML model instance.</summary>
public interface IMLModelRunner
{
    string ModelId { get; }
    string OutputTagName { get; }
    IReadOnlyList<string> InputTagNames { get; }
    float Predict(IReadOnlyList<float> inputValues);
}
