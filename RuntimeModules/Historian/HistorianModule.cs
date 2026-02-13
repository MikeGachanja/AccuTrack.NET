using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.Historian;

/// <summary>Historian module: implements IModuleInterface, wraps HistorianManager.</summary>
public sealed class HistorianModule : ModuleBase, IHistorian
{
    private readonly HistorianManager _historianManager = new();
    private readonly HashSet<string> _subscribedTags = new();

    public HistorianManager HistorianManager => _historianManager;

    public override string ModuleName => "HistorianModule";
    public override string DisplayName => "Historian Module";
    public override IReadOnlyList<string> Dependencies => new[] { "EventDispatcher", "TagsModule" };

    public override bool Initialize(JsonObject? config = null)
    {
        // Set database path based on project path
        string? projectPath = null;
        try
        {
            projectPath = Runtime.Modules.ExecutionEngine.ExecutionEngine.Instance.ProjectPath;
        }
        catch
        {
            // ExecutionEngine.Instance might not be available yet, try to get from config
            projectPath = config?["projectPath"]?.GetValue<string>();
        }
        
        if (!string.IsNullOrEmpty(projectPath))
        {
            // Create database subdirectory in project path for historian database
            var databasePath = Path.Combine(projectPath, "database");
            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
            }
            
            // Set database path to historian.db in the database folder (ensuring it's named "historian")
            var dbPath = Path.Combine(databasePath, "historian.db");
            
            // Update config with database path if not already set
            if (config == null)
            {
                config = new JsonObject();
            }
            
            var databaseType = config["databaseType"]?.GetValue<string>() ?? "SQLite";
            
            // For SQLite, always set the database path to ensure it's in the database folder
            if (databaseType == "SQLite")
            {
                // Always use the project-specific database folder path
                config["storagePath"] = dbPath;
            }
            // For PostgreSQL, the connection will be handled separately
        }

        _historianManager.Initialize(config);
        
        // Load tag configurations from database (in case they were saved previously)
        _historianManager.LoadTagConfigurations();
        
        TagsModule? tagManager = null;
        try
        {
            tagManager = Runtime.Modules.ExecutionEngine.ExecutionEngine.Instance.ModuleManager.GetModule("TagsModule") as TagsModule;
        }
        catch
        {
            // ExecutionEngine.Instance might not be available yet
        }
        if (tagManager != null)
        {
            _historianManager.SetTagManager(tagManager.TagManager);
            
            // Subscribe to tags based on loaded configurations (from database)
            // This ensures we use the persisted configurations
            _subscribedTags.Clear();
            
            // Get enabled tags from HistorianManager (loaded from database)
            var enabledTags = _historianManager.GetEnabledTags();
            
            if (enabledTags.Count > 0)
            {
                // Subscribe only to enabled tags
                var allTags = tagManager.TagManager.GetTagNames();
                foreach (var tagName in enabledTags)
                {
                    if (allTags.Contains(tagName) && !_subscribedTags.Contains(tagName))
                    {
                        tagManager.TagManager.Subscribe(tagName, (n, v, q) =>
                            _historianManager.RecordTagValue(n, v, (int)q));
                        _subscribedTags.Add(tagName);
                    }
                }
            }
            else if (config != null && config["tags"] is JsonArray tagsArray && tagsArray.Count > 0)
            {
                // Fallback to JSON config if database is empty
                foreach (var tagNode in tagsArray)
                {
                    if (tagNode is JsonObject tagObj)
                    {
                        var tagName = tagObj["tagName"]?.GetValue<string>();
                        var enabled = tagObj["enabled"]?.GetValue<bool>() ?? true;
                        
                        if (!string.IsNullOrEmpty(tagName) && enabled)
                        {
                            var allTags = tagManager.TagManager.GetTagNames();
                            if (allTags.Contains(tagName) && !_subscribedTags.Contains(tagName))
                            {
                                tagManager.TagManager.Subscribe(tagName, (n, v, q) =>
                                    _historianManager.RecordTagValue(n, v, (int)q));
                                _subscribedTags.Add(tagName);
                            }
                        }
                    }
                }
            }
            else
            {
                // If no tags configured, subscribe to all tags (backward compatibility)
                foreach (var name in tagManager.TagManager.GetTagNames())
                {
                    if (!_subscribedTags.Contains(name))
                    {
                        tagManager.TagManager.Subscribe(name, (n, v, q) =>
                            _historianManager.RecordTagValue(n, v, (int)q));
                        _subscribedTags.Add(name);
                    }
                }
            }
        }
        
        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        _historianManager.Start();
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        _historianManager.Stop();
        SetRunning(false);
    }
}
