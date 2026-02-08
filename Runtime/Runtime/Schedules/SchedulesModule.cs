using System.Text.Json;
using AccuTrack.Runtime.Modules;

namespace AccuTrack.Runtime.Schedules;

/// <summary>
/// SchedulesModule implementing IModuleInterface; parse schedule config; timer-based execution; call Script module. Config: schedules.json.
/// </summary>
public sealed class SchedulesModule : IModuleInterface
{
    private JsonElement? _config;
    private bool _running;
    private string _status = "Stopped";
    private Func<string, object?[]?, Task>? _runScript;

    public string ModuleName => "Schedules";
    public string DisplayName => "Schedules Module";
    public string Version => "1.0";
    public IReadOnlyList<string> Dependencies => new[] { "Script" };
    public bool IsRunning => _running;
    public string Status => _status;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Initialized;
    public event EventHandler? Started;
    public event EventHandler? Stopped;

    public void SetScriptRunner(Func<string, object?[]?, Task>? runScript) => _runScript = runScript;

    public Task InitializeAsync(JsonElement? config, CancellationToken cancellationToken = default)
    {
        _config = config;
        SetStatus("Initialized");
        Initialized?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _running = true;
        SetStatus("Running");
        Started?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _running = false;
        SetStatus("Stopped");
        Stopped?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public JsonElement? GetConfiguration() => _config;
    public bool ValidateConfiguration(JsonElement? config) => true;

    private void SetStatus(string s)
    {
        _status = s;
        StatusChanged?.Invoke(this, s);
    }
}
