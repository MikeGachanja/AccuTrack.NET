namespace Runtime.Modules.Communication;

/// <summary>Communication module interface (Modbus, OPC UA).</summary>
public interface ICommunication
{
    /// <summary>Get connection statuses for all configured connections (for status bar).</summary>
    IReadOnlyDictionary<string, ConnectionStatus> GetConnectionStatuses();
}
