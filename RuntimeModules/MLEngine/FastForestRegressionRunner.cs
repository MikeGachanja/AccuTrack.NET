using Microsoft.ML;
using Microsoft.ML.Data;

namespace Runtime.Modules.MLEngine;

/// <summary>Loads and runs an ML.NET Fast Forest regression model (zip).</summary>
public sealed class FastForestRegressionRunner : IMLModelRunner
{
    private readonly MLContext _mlContext = new(0);
    private readonly Microsoft.ML.PredictionEngine<MLInput, MLOutput> _engine;

    public string ModelId { get; }
    public string OutputTagName { get; }
    public IReadOnlyList<string> InputTagNames { get; }

    private FastForestRegressionRunner(string modelId, string outputTagName, IReadOnlyList<string> inputTagNames,
        Microsoft.ML.PredictionEngine<MLInput, MLOutput> engine)
    {
        ModelId = modelId;
        OutputTagName = outputTagName;
        InputTagNames = inputTagNames;
        _engine = engine;
    }

    public float Predict(IReadOnlyList<float> inputValues)
    {
        var input = new MLInput { Features = inputValues.ToArray() };
        var output = _engine.Predict(input);
        return output.Score;
    }

    /// <summary>Create a runner from a model zip path. Returns null if load fails.</summary>
    public static FastForestRegressionRunner? TryLoad(string modelFilePath, string modelId, string outputTagName, IReadOnlyList<string> inputTagNames)
    {
        if (!File.Exists(modelFilePath))
            return null;
        try
        {
            var mlContext = new MLContext(0);
            var transformer = mlContext.Model.Load(modelFilePath, out _);
            var engine = mlContext.Model.CreatePredictionEngine<MLInput, MLOutput>(transformer);
            return new FastForestRegressionRunner(modelId, outputTagName, inputTagNames, engine);
        }
        catch
        {
            return null;
        }
    }

    public sealed class MLInput
    {
        [VectorType]
        public float[] Features { get; set; } = Array.Empty<float>();
    }

    public sealed class MLOutput
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
