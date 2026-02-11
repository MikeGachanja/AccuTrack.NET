using System.Text.Json.Nodes;

namespace Runtime.Modules.ExecutionEngine;

/// <summary>
/// Base class for runtime modules. Provides default status and event raising for <see cref="IModuleInterface"/>.
/// </summary>
public abstract class ModuleBase : IModuleInterface
{
    private string _status = "Stopped";
    private bool _running;

    public abstract string ModuleName { get; }
    public virtual string DisplayName => ModuleName;
    public virtual string Version => "1.0.0";
    public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();

    public abstract bool Initialize(JsonObject? config = null);
    public abstract bool Start();
    public abstract void Stop();
    public virtual void Shutdown() { }

    public bool IsRunning => _running;
    public string Status => _status;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? Initialized;
    public event EventHandler? Started;
    public event EventHandler? Stopped;

    protected void SetRunning(bool value)
    {
        if (_running == value) return;
        _running = value;
        SetStatus(value ? "Running" : "Stopped");
        if (value) Started?.Invoke(this, EventArgs.Empty);
        else Stopped?.Invoke(this, EventArgs.Empty);
    }

    protected void SetStatus(string status)
    {
        if (_status == status) return;
        _status = status ?? "";
        StatusChanged?.Invoke(this, _status);
    }

    protected void RaiseError(string message)
    {
        ErrorOccurred?.Invoke(this, message);
    }

    protected void RaiseInitialized()
    {
        Initialized?.Invoke(this, EventArgs.Empty);
    }
}
