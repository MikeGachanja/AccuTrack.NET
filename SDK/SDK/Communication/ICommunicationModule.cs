using System.Text.Json;

namespace AccuTrack.SDK.Communication;

/// <summary>
/// Communication Abstraction Layer (CAL) interface that all protocol modules (Modbus, OPC, S7) must implement.
/// </summary>
public interface ICommunicationModule
{
    /// <summary>Module type name (e.g. "Modbus", "OPC", "S7").</summary>
    string ModuleType { get; }

    /// <summary>Configure the module from JSON. Returns true if valid and applied.</summary>
    bool Configure(JsonElement config);

    /// <summary>Current configuration as JSON object (same shape as Designer/Runtime expect).</summary>
    JsonElement GetConfiguration();

    /// <summary>Validate configuration without applying. Returns true if valid.</summary>
    bool ValidateConfiguration(JsonElement config);

    /// <summary>Start the module. Returns true if started successfully.</summary>
    bool Start();

    /// <summary>Stop the module.</summary>
    void Stop();

    /// <summary>Whether the module is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Display name of the module.</summary>
    string Name { get; set; }

    /// <summary>Raised when status changes (e.g. "Connected", "Disconnected", "Error").</summary>
    event EventHandler<string>? StatusChanged;

    /// <summary>Raised when an error occurs.</summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>Optional: raised when data is received (for push-style protocols).</summary>
    event EventHandler<JsonElement>? DataReceived;
}
