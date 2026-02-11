namespace Runtime.Modules.Historian;

/// <summary>Historian module interface. Provides tag history storage and query for TrendView and reporting.</summary>
public interface IHistorian
{
    /// <summary>Underlying historian manager (record, query tag values).</summary>
    HistorianManager HistorianManager { get; }
}
