namespace AccuTrack.SDK.Historian;

/// <summary>
/// Historian placeholder/namespace. Real logic (data_collector, historian_database, retention)
/// lives in Runtime (Plan 2). SDK holds shared types (e.g. HistorianConfig DTO) if needed.
/// </summary>
public class Historian : IHistorian
{
    public void WriteSample(string tagId, object? value, DateTime timestamp)
    {
        // Placeholder; Runtime implements
    }

    public object? Query(string? tagId, DateTime? start, DateTime? end)
    {
        // Placeholder; Runtime implements
        return null;
    }
}
