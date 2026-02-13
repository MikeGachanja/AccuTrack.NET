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
    private readonly ConcurrentDictionary<string, int> _tagLoggingIntervals = new(); // Per-tag logging intervals
    private int _loggingIntervalSeconds = 60;
    private readonly object _dbLock = new();

    public string DatabasePath => _databasePath;
    public bool IsRunning => _running;

    /// <summary>Get list of enabled tag names.</summary>
    public IReadOnlySet<string> GetEnabledTags() => _enabledTags;

    public void SetTagManager(TagManager? tagManager)
    {
        _tagManager = tagManager;
    }

    public bool Initialize(object? config)
    {
        if (config is JsonObject obj)
            {
            // Get database type from config
            var databaseType = obj["databaseType"]?.GetValue<string>() ?? "SQLite";
            _loggingIntervalSeconds = obj["loggingIntervalSeconds"]?.GetValue<int>() ?? 60;
            
            // Database path is set by HistorianModule based on project path
            // For SQLite, it will be set to database/historian.db
            // For PostgreSQL, connection string will be built from config
            var storagePath = obj["storagePath"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrEmpty(storagePath))
            {
                _databasePath = storagePath;
            }
            // If no storagePath in config, HistorianModule will set it automatically

            // Load enabled tags from config
            if (obj["tags"] is JsonArray tagsArray)
            {
                _enabledTags.Clear();
                _tagLoggingIntervals.Clear();
                foreach (var tagNode in tagsArray)
                {
                    if (tagNode is JsonObject tagObj)
                    {
                        var tagName = tagObj["tagName"]?.GetValue<string>();
                        var enabled = tagObj["enabled"]?.GetValue<bool>() ?? true;
                        var tagLoggingInterval = tagObj["loggingIntervalSeconds"]?.GetValue<int>() ?? _loggingIntervalSeconds;
                        
                        if (!string.IsNullOrEmpty(tagName))
                        {
                            if (enabled)
                            {
                                _enabledTags.Add(tagName);
                            }
                            // Store per-tag logging interval (use tag-specific or fall back to global)
                            _tagLoggingIntervals[tagName] = tagLoggingInterval;
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
                
                // Save tag configurations to database
                if (config is JsonObject jsonConfig && jsonConfig["tags"] is JsonArray tagsArray)
                {
                    SaveTagConfigurations(tagsArray);
                }
                
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
                    id TEXT PRIMARY KEY,
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

    /// <summary>Save tag configurations to historian_tags table.</summary>
    private void SaveTagConfigurations(JsonArray tagsArray)
    {
        if (string.IsNullOrEmpty(_databasePath))
            return;

        lock (_dbLock)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                foreach (var tagNode in tagsArray)
                {
                    if (tagNode is JsonObject tagObj)
                    {
                        var id = tagObj["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString();
                        var tagName = tagObj["tagName"]?.GetValue<string>();
                        var tagTableName = tagObj["tagTableName"]?.GetValue<string>();
                        var enabled = tagObj["enabled"]?.GetValue<bool>() ?? true;
                        var loggingIntervalSeconds = tagObj["loggingIntervalSeconds"]?.GetValue<int>() ?? _loggingIntervalSeconds;
                        var description = tagObj["description"]?.GetValue<string>();

                        if (!string.IsNullOrEmpty(tagName))
                        {
                            // Use INSERT OR REPLACE to update existing tags
                            var insertOrReplace = @"
                                INSERT OR REPLACE INTO historian_tags 
                                (id, tag_name, tag_table_name, enabled, logging_interval_seconds, description, created_at)
                                VALUES (@id, @tagName, @tagTableName, @enabled, @loggingIntervalSeconds, @description, 
                                        COALESCE((SELECT created_at FROM historian_tags WHERE tag_name = @tagName), CURRENT_TIMESTAMP))";

                            using var command = new SqliteCommand(insertOrReplace, connection);
                            command.Parameters.AddWithValue("@id", id);
                            command.Parameters.AddWithValue("@tagName", tagName);
                            command.Parameters.AddWithValue("@tagTableName", tagTableName ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@enabled", enabled ? 1 : 0);
                            command.Parameters.AddWithValue("@loggingIntervalSeconds", loggingIntervalSeconds);
                            command.Parameters.AddWithValue("@description", description ?? (object)DBNull.Value);

                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistorianManager] Failed to save tag configurations: {ex.Message}");
            }
        }
    }

    /// <summary>Load tag configurations from historian_tags table.</summary>
    public void LoadTagConfigurations()
    {
        if (string.IsNullOrEmpty(_databasePath) || !File.Exists(_databasePath))
            return;

        lock (_dbLock)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();

                var query = @"
                    SELECT tag_name, enabled, logging_interval_seconds 
                    FROM historian_tags";

                using var command = new SqliteCommand(query, connection);
                using var reader = command.ExecuteReader();

                _enabledTags.Clear();
                _tagLoggingIntervals.Clear();

                while (reader.Read())
                {
                    var tagName = reader.GetString(0);
                    var enabled = reader.GetInt32(1) == 1;
                    var loggingIntervalSeconds = reader.GetInt32(2);

                    if (enabled)
                    {
                        _enabledTags.Add(tagName);
                    }
                    _tagLoggingIntervals[tagName] = loggingIntervalSeconds;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistorianManager] Failed to load tag configurations: {ex.Message}");
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

        // Get per-tag logging interval or use global default
        var tagLoggingInterval = _tagLoggingIntervals.TryGetValue(tagName, out var interval) 
            ? interval 
            : _loggingIntervalSeconds;

        // Check logging interval
        if (_lastRecordedTime.TryGetValue(tagName, out var lastTime))
        {
            var elapsed = (DateTime.UtcNow - lastTime).TotalSeconds;
            if (elapsed < tagLoggingInterval)
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
