namespace AccuTrack.SDK.Modbus;

/// <summary>Connection state for Modbus (UI status, events).</summary>
public enum ModbusConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Error
}
