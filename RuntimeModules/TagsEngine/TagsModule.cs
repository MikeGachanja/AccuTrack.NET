using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;

namespace Runtime.Modules.TagsEngine;

/// <summary>Tags module: implements IModuleInterface, exposes TagManager and TagIOHandler.</summary>
public sealed class TagsModule : ModuleBase, ITagsEngine
{
    private readonly TagManager _tagManager = new();
    private readonly TagIOHandler _tagIOHandler;

    public TagManager TagManager => _tagManager;
    public TagIOHandler TagIOHandler => _tagIOHandler;

    public override string ModuleName => "TagsModule";
    public override string DisplayName => "Tags Module";
    public override IReadOnlyList<string> Dependencies => new[] { "EventDispatcher" }; // Communication optional, wired by host

    public TagsModule()
    {
        _tagIOHandler = new TagIOHandler(_tagManager);
    }

    public void SetCommunicationModule(object? commModule)
    {
        _tagIOHandler.SetCommunicationModule(commModule);
    }

    public override bool Initialize(JsonObject? config = null)
    {
        if (config != null)
        {
            // Config may be path or embedded tags
            if (config["tags"] is JsonArray arr)
            {
                foreach (var node in arr)
                {
                    if (node is not JsonObject obj) continue;
                    var name = obj["name"]?.GetValue<string>() ?? "";
                    var address = obj["address"]?.GetValue<string>() ?? "";
                    var description = obj["description"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrEmpty(name))
                        _tagManager.RegisterTag(name, address, description);
                }
            }
        }
        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        _tagIOHandler.Start();
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        _tagIOHandler.Stop();
        SetRunning(false);
    }
}
