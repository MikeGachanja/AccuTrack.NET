namespace AccuTrack.Runtime.Abstractions;

/// <summary>
/// Communication driver contract. Runtime wraps instances per communication.json. Can be implemented by SDK (Plan 3).
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

public record ConnectionStatus(string Name, string DriverType, bool IsConnected, string? Message = null);
