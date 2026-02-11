using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;
using Runtime.Modules.TagsEngine;

namespace Runtime.Modules.Historian;

/// <summary>Historian module: implements IModuleInterface, wraps HistorianManager.</summary>
public sealed class HistorianModule : ModuleBase, IHistorian
{
    private readonly HistorianManager _historianManager = new();

    public HistorianManager HistorianManager => _historianManager;

    public override string ModuleName => "HistorianModule";
    public override string DisplayName => "Historian Module";
    public override IReadOnlyList<string> Dependencies => new[] { "EventDispatcher", "TagsModule" };

    public override bool Initialize(JsonObject? config = null)
    {
        _historianManager.Initialize(config);
        var tagManager = Runtime.Modules.ExecutionEngine.ExecutionEngine.Instance.ModuleManager.GetModule("TagsModule") as TagsModule;
        if (tagManager != null)
        {
            _historianManager.SetTagManager(tagManager.TagManager);
            // Subscribe to tag changes to record history
            foreach (var name in tagManager.TagManager.GetTagNames())
            {
                tagManager.TagManager.Subscribe(name, (n, v, q) =>
                    _historianManager.RecordTagValue(n, v, (int)q));
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
