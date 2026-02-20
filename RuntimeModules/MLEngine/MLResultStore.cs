using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Runtime.Modules.MLEngine;

/// <summary>
/// Writes ML prediction results to a separate SQLite database (ml.db) so historian and ML writes do not contend.
/// Path: {projectPath}/database/ml.db.
/// </summary>
public sealed class MLResultStore
{
    private string _databasePath = "";
    private readonly object _dbLock = new();

    public string DatabasePath => _databasePath;

    /// <summary>Set the database path (e.g. projectPath/database/ml.db). Call before RecordResult.</summary>
    public void SetDatabasePath(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            _databasePath = "";
            return;
        }
        var databaseDir = Path.Combine(projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "database");
        _databasePath = Path.Combine(databaseDir, "ml.db");
    }

    /// <summary>Ensure the database and table exist. Call once after SetDatabasePath.</summary>
    public void EnsureInitialized()
    {
        if (string.IsNullOrEmpty(_databasePath)) return;
        lock (_dbLock)
        {
            var dir = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            connection.Open();

            const string createTable = @"
                CREATE TABLE IF NOT EXISTS ml_results (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    model_id TEXT NOT NULL,
                    output_tag_name TEXT NOT NULL,
                    timestamp TEXT NOT NULL,
                    value TEXT,
                    quality INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                )";
            using (var cmd = new SqliteCommand(createTable, connection))
                cmd.ExecuteNonQuery();

            using (var cmd = new SqliteCommand("CREATE INDEX IF NOT EXISTS idx_ml_results_model_timestamp ON ml_results(model_id, timestamp)", connection))
                cmd.ExecuteNonQuery();
            using (var cmd = new SqliteCommand("CREATE INDEX IF NOT EXISTS idx_ml_results_output_tag ON ml_results(output_tag_name)", connection))
                cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Record one prediction result. Thread-safe; uses a dedicated lock (separate from historian).</summary>
    public void RecordResult(string modelId, string outputTagName, object? value, int quality)
    {
        if (string.IsNullOrEmpty(_databasePath)) return;
        if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(outputTagName)) return;

        lock (_dbLock)
        {
            try
            {
                using var connection = new SqliteConnection($"Data Source={_databasePath}");
                connection.Open();
                const string insert = @"
                    INSERT INTO ml_results (model_id, output_tag_name, timestamp, value, quality)
                    VALUES (@modelId, @outputTagName, @timestamp, @value, @quality)";
                using var command = new SqliteCommand(insert, connection);
                command.Parameters.AddWithValue("@modelId", modelId);
                command.Parameters.AddWithValue("@outputTagName", outputTagName);
                command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                command.Parameters.AddWithValue("@value", value?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@quality", quality);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MLResultStore] Record error: {ex.Message}");
            }
        }
    }
}
