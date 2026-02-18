namespace Runtime.Modules.MLEngine;

/// <summary>Machine learning engine interface for runtime.</summary>
public interface IMLEngine
{
    bool IsInitialized { get; }
    /// <summary>Run inference once for the given model (e.g. from scheduler).</summary>
    void RunModelOnce(string modelId);
}
