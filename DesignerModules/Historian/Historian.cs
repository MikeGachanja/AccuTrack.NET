using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Designer.Modules.Historian;

/// <summary>
/// Represents historian configuration for a SCADA project.
/// </summary>
public class Historian
{
    public List<HistorianTag> Tags { get; set; } = new List<HistorianTag>();
    public int LoggingIntervalSeconds { get; set; } = 60; // Default 1 minute
    public string DatabaseType { get; set; } = "SQLite"; // SQLite (default) or PostgreSQL
    public int MaxStorageSizeMB { get; set; } = 1000; // Maximum storage size in MB
    public string RetentionPolicy { get; set; } = "Days"; // Days, Size, Both
    public int RetentionDays { get; set; } = 30; // Keep data for 30 days
    public bool Enabled { get; set; } = true;
    
    // PostgreSQL connection settings (only used when DatabaseType is PostgreSQL)
    public string? PostgresHost { get; set; }
    public int PostgresPort { get; set; } = 5432;
    public string? PostgresDatabase { get; set; }
    public string? PostgresUsername { get; set; }
    public string? PostgresPassword { get; set; }

    /// <summary>
    /// Converts historian to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        var obj = new JObject();
        var tagsArray = new JArray();

        foreach (var tag in Tags)
        {
            tagsArray.Add(tag.ToJson());
        }

        obj["tags"] = tagsArray;
        obj["loggingIntervalSeconds"] = LoggingIntervalSeconds;
        obj["databaseType"] = DatabaseType;
        obj["maxStorageSizeMB"] = MaxStorageSizeMB;
        obj["retentionPolicy"] = RetentionPolicy;
        obj["retentionDays"] = RetentionDays;
        obj["enabled"] = Enabled;
        
        // Include PostgreSQL settings if DatabaseType is PostgreSQL
        if (DatabaseType == "PostgreSQL")
        {
            if (!string.IsNullOrEmpty(PostgresHost))
                obj["postgresHost"] = PostgresHost;
            obj["postgresPort"] = PostgresPort;
            if (!string.IsNullOrEmpty(PostgresDatabase))
                obj["postgresDatabase"] = PostgresDatabase;
            if (!string.IsNullOrEmpty(PostgresUsername))
                obj["postgresUsername"] = PostgresUsername;
            if (!string.IsNullOrEmpty(PostgresPassword))
                obj["postgresPassword"] = PostgresPassword;
        }

        return obj;
    }

    /// <summary>
    /// Creates historian from JSON object.
    /// </summary>
    public static Historian FromJson(JObject json)
    {
        var historian = new Historian
        {
            LoggingIntervalSeconds = json["loggingIntervalSeconds"]?.ToObject<int>() ?? 60,
            DatabaseType = json["databaseType"]?.ToString() ?? "SQLite",
            MaxStorageSizeMB = json["maxStorageSizeMB"]?.ToObject<int>() ?? 1000,
            RetentionPolicy = json["retentionPolicy"]?.ToString() ?? "Days",
            RetentionDays = json["retentionDays"]?.ToObject<int>() ?? 30,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true
        };
        
        // Load PostgreSQL settings if DatabaseType is PostgreSQL
        if (historian.DatabaseType == "PostgreSQL")
        {
            historian.PostgresHost = json["postgresHost"]?.ToString();
            historian.PostgresPort = json["postgresPort"]?.ToObject<int>() ?? 5432;
            historian.PostgresDatabase = json["postgresDatabase"]?.ToString();
            historian.PostgresUsername = json["postgresUsername"]?.ToString();
            historian.PostgresPassword = json["postgresPassword"]?.ToString();
        }
        
        // Backward compatibility: if old storageType/storagePath exist, migrate
        if (json["storageType"] != null)
        {
            var oldStorageType = json["storageType"]?.ToString() ?? "Database";
            if (oldStorageType == "Database" || oldStorageType == "File")
            {
                historian.DatabaseType = "SQLite";
            }
        }

        var tagsArray = json["tags"] as JArray;
        if (tagsArray != null)
        {
            foreach (var item in tagsArray)
            {
                if (item is JObject tagObj)
                {
                    historian.Tags.Add(HistorianTag.FromJson(tagObj));
                }
            }
        }

        return historian;
    }
}

/// <summary>
/// Represents a tag configured for historical logging.
/// </summary>
public class HistorianTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TagName { get; set; } = string.Empty;
    public string TagTableName { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int LoggingIntervalSeconds { get; set; } = 0; // 0 = use global interval
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Converts historian tag to JSON object.
    /// </summary>
    public JObject ToJson()
    {
        return new JObject
        {
            ["id"] = Id.ToString(),
            ["tagName"] = TagName,
            ["tagTableName"] = TagTableName,
            ["enabled"] = Enabled,
            ["loggingIntervalSeconds"] = LoggingIntervalSeconds,
            ["description"] = Description
        };
    }

    /// <summary>
    /// Creates historian tag from JSON object.
    /// </summary>
    public static HistorianTag FromJson(JObject json)
    {
        var tag = new HistorianTag
        {
            TagName = json["tagName"]?.ToString() ?? string.Empty,
            TagTableName = json["tagTableName"]?.ToString() ?? string.Empty,
            Enabled = json["enabled"]?.ToObject<bool>() ?? true,
            LoggingIntervalSeconds = json["loggingIntervalSeconds"]?.ToObject<int>() ?? 0,
            Description = json["description"]?.ToString() ?? string.Empty
        };

        if (Guid.TryParse(json["id"]?.ToString(), out Guid id))
        {
            tag.Id = id;
        }

        return tag;
    }
}
