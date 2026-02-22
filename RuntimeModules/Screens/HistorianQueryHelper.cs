using System;
using System.Collections.Generic;
using System.IO;

namespace Runtime.Modules.Screens;

/// <summary>Query historian for tag values (for TrendView etc.). Can delegate to Historian module.</summary>
public sealed class HistorianQueryHelper
{
    private string _databasePath = "";
    private bool _databaseAvailable;
    private object? _historianManager;

    public void SetDatabasePath(string path)
    {
        _databasePath = path ?? "";
        _databaseAvailable = !string.IsNullOrEmpty(_databasePath) && File.Exists(_databasePath);
    }

    public void SetHistorianManager(object? manager) => _historianManager = manager;

    public bool IsDatabaseAvailable() => _databaseAvailable;

    /// <summary>Query tag values. Returns list of (timestamp ms, value, quality).</summary>
    public IReadOnlyList<(long TimestampMs, object? Value, int Quality)> QueryTagValues(string tagName, long startTimeMs, long endTimeMs, int maxRows = 0)
    {
        if (_historianManager != null)
        {
            var method = _historianManager.GetType().GetMethod("QueryTagValues",
                new[] { typeof(string), typeof(long), typeof(long), typeof(int) });
            if (method != null)
            {
                try
                {
                    var result = method.Invoke(_historianManager, new object[] { tagName, startTimeMs, endTimeMs, maxRows });
                    if (result is IReadOnlyList<(DateTime, object?, int)> list)
                    {
                        var outList = new List<(long, object?, int)>();
                        foreach (var item in list)
                            outList.Add((new DateTimeOffset(item.Item1).ToUnixTimeMilliseconds(), item.Item2, item.Item3));
                        return outList;
                    }
                }
                catch { }
            }
        }
        return Array.Empty<(long, object?, int)>();
    }
}
