using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.Historian;

/// <summary>Historian manager: SQLite database storage, tag subscription, query API.</summary>
public sealed class HistorianManager
{
    private string _databasePath = "";
    private volatile bool _running;
    private TagManager? _tagManager;
    private readonly ConcurrentDictionary<string, DateTime> _lastRecordedTime = new();
    private readonly HashSet<string> _enabledTags = new();
    private int _loggingIntervalSeconds = 60;
    private readonly object _dbLock = new();

    public string DatabasePath => _databasePath;
    public bool IsRunning => _running;

    public void SetTagManager(TagManager? tagManager)
    {
        _tagManager = tagManager;
    }

    public bool Initialize(object? config)
    {
        if (config is JsonObject obj)
        {
            // Get database path from config or use default
            var storagePath = obj["storagePath"]?.GetValue<string>() ?? "";
            var storageType = obj["storageType"]?.GetValue<string>() ?? "Database";
            _loggingIntervalSeconds = obj["loggingIntervalSeconds"]?.GetValue<int>() ?? 60;
            
            // If storage type is Database and we have a project path, use it
            if (storageType == "Database" || storageType == "File")
            {
                // The database path should be set by HistorianModule based on project path
                // For now, we'll use the storagePath if provided, otherwise default to "historian.db"
                _databasePath = storagePath;
            }
            else
            {
                _databasePath = storagePath;
            }

            // Load enabled tags from config
            if (obj["tags"] is JsonArray tagsArray)
            {
                _enabledTags.Clear();
                foreach (var tagNode in tagsArray)
                {
                    if (tagNode is JsonObject tagObj)
                    {
                        var tagName = tagObj["tagName"]?.GetValue<string>();
                        var enabled = tagObj["enabled"]?.GetValue<bool>() ?? true;
                        if (!string.IsNullOrEmpty(tagName) && enabled)
                        {
                            _enabledTags.Add(tagName);
                        }
                    }
                }
            }
        }

        // Initialize database if path is set
        if (!string.IsNullOrEmpty(_databasePath))
        {
            try
            {
                InitializeDatabase();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistorianManager] Failed to initialize database: {ex.Message}");
                return false;
            }
        }

        return true;
    }

    private void InitializeDatabase()
    {
        if (string.IsNullOrEmpty(_databasePath))
            return;

        // Ensure directory exists
        var dir = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        lock (_dbLock)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            // Create tags table if it doesn't exist
            var createTagsTable = @"
                CREATE TABLE IF NOT EXISTS historian_tags (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tag_name TEXT NOT NULL,
                    tag_table_name TEXT,
                    enabled INTEGER NOT NULL DEFAULT 1,
                    logging_interval_seconds INTEGER NOT NULL DEFAULT 0,
                    description TEXT,
                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(tag_name)
                )";

            using (var command = new SqliteCommand(createTagsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create history table if it doesn't exist
            var createHistoryTable = @"
                CREATE TABLE IF NOT EXISTS tag_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tag_name TEXT NOT NULL,
                    timestamp TEXT NOT NULL,
                    value TEXT,
                    quality INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )";

            using (var command = new SqliteCommand(createHistoryTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Create indexes for better query performance
            var createIndexes = @"
                CREATE INDEX IF NOT EXISTS idx_tag_history_tag_name ON tag_history(tag_name);
                CREATE INDEX IF NOT EXISTS idx_tag_history_timestamp ON tag_history(timestamp);
                CREATE INDEX IF NOT EXISTS idx_tag_history_tag_timestamp ON tag_history(tag_name, timestamp)";

            using (var command = new SqliteCommand(createIndexes, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }

    public void Start() => _running = true;
    public void Stop() => _running = false;

    /// <summary>Query tag values from SQLite database.</summary>
    public IReadOnlyList<(DateTime Timestamp, object? Value, int Quality)> QueryTagValues(string tagName, long startTimeMs, long endTimeMs, int maxRows = 0)
    {
        if (string.IsNullOrEmpty(_databasePath) || !File.Exists(_databasePath))
            return Array.Empty<(DateTime, object?, int)>();

        var results = new List<(DateTime, object?, int)>();
        var start = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs).UtcDateTime;
        var end = DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs).UtcDateTime;

        lock (_dbLock)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                var query = @"
                    SELECT timestamp, value, quality 
                    FROM tag_history 
                    WHERE tag_name = @tagName 
                    AND timestamp >= @startTime 
                    AND timestamp <= @endTime 
                    ORDER BY timestamp ASC";

                if (maxRows > 0)
                {
                    query += $" LIMIT {maxRows}";
                }

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@tagName", tagName);
                command.Parameters.AddWithValue("@startTime", start.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@endTime", end.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var timestampStr = reader.GetString(0);
                    if (DateTime.TryParse(reader.GetString(0), out var timestamp))
                    {
                        var valueStr = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var quality = reader.GetInt32(2);
                        
                        // Try to parse value based on type
                        object? value = valueStr;
                        if (valueStr != null)
                        {
                            if (double.TryParse(valueStr, out var dbl))
                                value = dbl;
                            else if (bool.TryParse(valueStr, out var bl))
                                value = bl;
                            else if (int.TryParse(valueStr, out var i))
                                value = i;
                        }

                        results.Add((timestamp, value, quality));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistorianManager] Query error: {ex.Message}");
            }
        }

        return results;
    }

    /// <summary>Record a tag value to SQLite database.</summary>
    public void RecordTagValue(string tagName, object? value, int quality)
    {
        if (!_running || string.IsNullOrEmpty(_databasePath))
            return;

        // Check if tag is enabled
        if (_enabledTags.Count > 0 && !_enabledTags.Contains(tagName))
            return;

        // Check logging interval
        if (_lastRecordedTime.TryGetValue(tagName, out var lastTime))
        {
            var elapsed = (DateTime.UtcNow - lastTime).TotalSeconds;
            if (elapsed < _loggingIntervalSeconds)
                return;
        }

        lock (_dbLock)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                var insert = @"
                    INSERT INTO tag_history (tag_name, timestamp, value, quality)
                    VALUES (@tagName, @timestamp, @value, @quality)";

                using var command = new SqliteCommand(insert, connection);
                command.Parameters.AddWithValue("@tagName", tagName);
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@value", value?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@quality", quality);

                command.ExecuteNonQuery();
                _lastRecordedTime[tagName] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistorianManager] Record error: {ex.Message}");
            }
        }
    }
}
