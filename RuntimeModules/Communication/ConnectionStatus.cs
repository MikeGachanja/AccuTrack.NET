namespace Runtime.Modules.Communication;

/// <summary>Status of a single connection (Modbus, OPC UA, etc.).</summary>
public sealed class ConnectionStatus
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Connected { get; set; }
    public string Status { get; set; } = "";
    public bool Running { get; set; }
}
