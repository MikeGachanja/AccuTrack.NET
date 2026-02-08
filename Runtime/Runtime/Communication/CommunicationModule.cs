using System.Collections.Concurrent;
using System.Text.Json;
using AccuTrack.Runtime.Abstractions;
using AccuTrack.Runtime.Modules;

namespace AccuTrack.Runtime.Communication;

/// <summary>
/// Wraps SDK ICommunicationDriver instances; load from communication.json; expose connection status for UI.
/// </summary>
public sealed class CommunicationModule : IModuleInterface
{
    private readonly List<ICommunicationDriver> _drivers = new();
    private readonly object _lock = new();
    private JsonElement? _config;
    private bool _running;
    private string _status = "Stopped";

    public string ModuleName => "Communication";
    public string DisplayName => "Communication Module";
    public string Version => "1.0";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public bool IsRunning => _running;
    public string Status => _status;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Initialized;
    public event EventHandler? Started;
    public event EventHandler? Stopped;

    public IReadOnlyList<ICommunicationDriver> Drivers
    {
        get { lock (_lock) return _drivers.ToList(); }
    }

    public IReadOnlyList<ConnectionStatus> GetConnectionStatuses()
    {
        lock (_lock)
        {
            return _drivers.Select(d => new ConnectionStatus(d.Name, d.DriverType, d.IsConnected)).ToList();
        }
    }

    public void AddDriver(ICommunicationDriver driver)
    {
        lock (_lock) _drivers.Add(driver);
    }

    public void ClearDrivers()
    {
        lock (_lock)
        {
            foreach (var d in _drivers)
                try { d.Stop(); } catch { }
            _drivers.Clear();
        }
    }

    public Task InitializeAsync(JsonElement? config, CancellationToken cancellationToken = default)
    {
        _config = config;
        ClearDrivers();
        if (config != null && config.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in config.Value.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var n) ? n.GetString() : item.TryGetProperty("Name", out var n2) ? n2.GetString() : "Driver";
                var type = item.TryGetProperty("type", out var t) ? t.GetString() : item.TryGetProperty("Type", out var t2) ? t2.GetString() : "Modbus";
                // SDK would create actual driver from type; for now no-op
            }
        }
        SetStatus("Initialized");
        Initialized?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            foreach (var d in _drivers)
                try { d.Start(); } catch { }
        }
        _running = true;
        SetStatus("Running");
        Started?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            foreach (var d in _drivers)
                try { d.Stop(); } catch { }
        }
        _running = false;
        SetStatus("Stopped");
        Stopped?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ClearDrivers();
        return Task.CompletedTask;
    }

    public JsonElement? GetConfiguration() => _config;
    public bool ValidateConfiguration(JsonElement? config) => true;

    private void SetStatus(string s)
    {
        _status = s;
        StatusChanged?.Invoke(this, s);
    }
}
