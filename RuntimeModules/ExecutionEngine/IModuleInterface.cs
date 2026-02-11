using System.Text.Json.Nodes;

namespace Runtime.Modules.ExecutionEngine;

/// <summary>
/// Base interface for all runtime modules. Modules implement this to participate
/// in the execution engine's lifecycle (initialize, start, stop, shutdown).
/// </summary>
public interface IModuleInterface
{
    /// <summary>Unique module identifier.</summary>
    string ModuleName { get; }

    /// <summary>Display name for UI.</summary>
    string DisplayName { get; }

    /// <summary>Module version.</summary>
    string Version { get; }

    /// <summary>Module names that must be started before this module.</summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>Initialize the module with optional JSON config. Called once before Start.</summary>
    bool Initialize(JsonObject? config = null);

    /// <summary>Start the module. Called after dependencies are started.</summary>
    bool Start();

    /// <summary>Stop the module gracefully.</summary>
    void Stop();

    /// <summary>Clean up resources after Stop.</summary>
    void Shutdown();

    /// <summary>Whether the module is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Current status description (e.g. "Running", "Stopped", "Error").</summary>
    string Status { get; }

    /// <summary>Raised when status changes.</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Raised when an error occurs.</summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>Raised when initialization completes.</summary>
    event EventHandler? Initialized;

    /// <summary>Raised when start completes.</summary>
    event EventHandler? Started;

    /// <summary>Raised when stop completes.</summary>
    event EventHandler? Stopped;
}
