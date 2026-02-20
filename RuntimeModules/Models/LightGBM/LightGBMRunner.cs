namespace Runtime.Modules.Models;

/// <summary>Placeholder LightGBM runner. Replace with real implementation when ready.</summary>
public sealed class LightGBMRunner : MLModelRunnerBase
{
    public const string KindId = "LightGBM";

    private LightGBMRunner() { }

    public override float Predict(IReadOnlyList<float> inputValues) => 0f;

    public static LightGBMRunner? TryLoad(string modelFilePath, string modelId, string outputTagName, IReadOnlyList<string> inputTagNames)
    {
        return null;
    }
}
