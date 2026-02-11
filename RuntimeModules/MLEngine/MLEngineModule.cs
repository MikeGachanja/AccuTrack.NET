using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;

namespace Runtime.Modules.MLEngine;

/// <summary>ML engine module stub for runtime.</summary>
public sealed class MLEngineModule : ModuleBase, IMLEngine
{
    public bool IsInitialized => Status == "Initialized" || IsRunning;

    public override string ModuleName => "MLEngine";
    public override string DisplayName => "ML Engine";

    public override bool Initialize(JsonObject? config = null)
    {
        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    public override bool Start()
    {
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        SetRunning(false);
    }
}
