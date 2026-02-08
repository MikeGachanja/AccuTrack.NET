using System.Text.Json;
using AccuTrack.Runtime.Modules;

namespace AccuTrack.Runtime.Security;

/// <summary>
/// SecurityModule stub; IModuleInterface for consistency; minimal role check if required.
/// </summary>
public sealed class SecurityModule : IModuleInterface
{
    private JsonElement? _config;
    private bool _running;
    private string _status = "Stopped";

    public string ModuleName => "Security";
    public string DisplayName => "Security Module";
    public string Version => "1.0";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public bool IsRunning => _running;
    public string Status => _status;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Initialized;
    public event EventHandler? Started;
    public event EventHandler? Stopped;

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

    public bool HasRole(string user, string role) => true; // stub

    private void SetStatus(string s)
    {
        _status = s;
        StatusChanged?.Invoke(this, s);
    }
}
