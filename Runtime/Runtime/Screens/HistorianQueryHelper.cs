using AccuTrack.Runtime.Historian;

namespace AccuTrack.Runtime.Screens;

/// <summary>
/// Query historian DB for trend data; expose to Avalonia TrendView via interface or view model.
/// </summary>
public sealed class HistorianQueryHelper
{
    private Func<string, DateTime, DateTime, Task<IReadOnlyList<HistorianDataPoint>>>? _queryFunc;

    public void SetQueryProvider(Func<string, DateTime, DateTime, Task<IReadOnlyList<HistorianDataPoint>>>? provider)
    {
        _queryFunc = provider;
    }

    public async Task<IReadOnlyList<HistorianDataPoint>> QueryAsync(string tagName, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        if (_queryFunc != null)
            return await _queryFunc(tagName, start, end).ConfigureAwait(false);
        return Array.Empty<HistorianDataPoint>();
    }
}
