using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;

namespace Runtime.Modules.Security;

/// <summary>Security module stub for runtime.</summary>
public sealed class SecurityModule : ModuleBase, ISecurity
{
    public bool IsInitialized => Status == "Initialized" || IsRunning;

    public override string ModuleName => "SecurityModule";
    public override string DisplayName => "Security Module";

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
