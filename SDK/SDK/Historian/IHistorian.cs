namespace AccuTrack.SDK.Historian;

/// <summary>
/// Optional abstraction for historian service. Runtime implements data_collector, historian_database,
/// retention; SDK only defines the contract (e.g. WriteSample, Query).
/// </summary>
public interface IHistorian
{
    /// <summary>Write a single sample for a tag.</summary>
    void WriteSample(string tagId, object? value, DateTime timestamp);

    /// <summary>Query samples (e.g. by tag, time range). Implementation-defined; Runtime provides.</summary>
    object? Query(string? tagId, DateTime? start, DateTime? end);
}
