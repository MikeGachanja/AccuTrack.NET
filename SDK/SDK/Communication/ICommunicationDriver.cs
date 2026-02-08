namespace AccuTrack.SDK.Communication;

/// <summary>
/// SDK communication driver contract (Plan 3). Runtime wraps instances per communication.json.
/// </summary>
public interface ICommunicationDriver
{
    string Name { get; }
    string DriverType { get; }
    bool IsConnected { get; }
    void Start();
    void Stop();
    Task<object?> ReadAsync(string address, CancellationToken cancellationToken = default);
    Task WriteAsync(string address, object value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Connection status for UI (status bar).
/// </summary>
public record ConnectionStatus(string Name, string DriverType, bool IsConnected, string? Message = null);
