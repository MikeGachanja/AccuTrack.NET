namespace AccuTrack.SDK.Historian;

/// <summary>Shared DTO for historian configuration (JSON); used by Designer and Runtime.</summary>
public class HistorianConfig
{
    public string Name { get; set; } = string.Empty;
    public string? DatabasePath { get; set; }
    public int RetentionDays { get; set; }
    public int FlushIntervalSeconds { get; set; }
}
