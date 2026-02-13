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
        var projectPath = ExecutionEngine.Instance.ProjectPath;
        if (!string.IsNullOrEmpty(projectPath))
        {
            // Create data subdirectory in project path for historian database
            var dataPath = Path.Combine(projectPath, "data");
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            
            // Set database path to historian.db in the data folder
            var dbPath = Path.Combine(dataPath, "historian.db");
            
            // Update config with database path if not already set
            if (config == null)
            {
                config = new JsonObject();
            }
            
            var storageType = config["storageType"]?.GetValue<string>() ?? "Database";
            if (storageType == "Database" || storageType == "File" || string.IsNullOrEmpty(config["storagePath"]?.GetValue<string>()))
            {
                config["storagePath"] = dbPath;
            }
        }

        _historianManager.Initialize(config);
        
        var tagManager = ExecutionEngine.Instance.ModuleManager.GetModule("TagsModule") as TagsModule;
        if (tagManager != null)
        {
            _historianManager.SetTagManager(tagManager.TagManager);
            
            // Subscribe only to tags configured in historian
            if (config != null && config["tags"] is JsonArray tagsArray)
            {
                _subscribedTags.Clear();
                foreach (var tagNode in tagsArray)
                {
                    if (tagNode is JsonObject tagObj)
                    {
                        var tagName = tagObj["tagName"]?.GetValue<string>();
                        var enabled = tagObj["enabled"]?.GetValue<bool>() ?? true;
                        
                        if (!string.IsNullOrEmpty(tagName) && enabled)
                        {
                            // Check if tag exists in TagManager
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
