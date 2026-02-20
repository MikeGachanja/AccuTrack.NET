namespace Runtime.Modules.Models;

/// <summary>
/// Base type for ML model runners. Implementations should expose a static <c>KindId</c>
/// and static <c>TryLoad(...)</c> so MLEngine can discover and load them by configuration.
/// Any model DLL that follows this structure can be loaded and integrated at startup.
/// </summary>
public abstract class MLModelRunnerBase : IMLModelRunner
{
    public string ModelId { get; protected set; } = string.Empty;
    public string OutputTagName { get; protected set; } = string.Empty;
    public IReadOnlyList<string> InputTagNames { get; protected set; } = Array.Empty<string>();

    protected MLModelRunnerBase() { }

    public abstract float Predict(IReadOnlyList<float> inputValues);
}
