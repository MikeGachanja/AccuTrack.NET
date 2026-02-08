using System.Text.Json;

namespace AccuTrack.Runtime.Modules;

/// <summary>
/// Base interface for runtime modules (mirrors Qt ModuleInterface).
/// </summary>
public interface IModuleInterface
{
    string ModuleName { get; }
    string DisplayName { get; }
    string Version { get; }
    IReadOnlyList<string> Dependencies { get; }

    Task InitializeAsync(JsonElement? config, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    bool IsRunning { get; }
    string Status { get; }
    JsonElement? GetConfiguration();
    bool ValidateConfiguration(JsonElement? config);

    event EventHandler<string>? StatusChanged;
    event EventHandler<string>? ErrorOccurred;
    event EventHandler? Initialized;
    event EventHandler? Started;
    event EventHandler? Stopped;
}
