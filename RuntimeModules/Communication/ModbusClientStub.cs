using System.Text.Json.Nodes;

namespace Runtime.Modules.Communication;

/// <summary>Stub Modbus client. Parses TCP/RTU config; no real connection. For pipeline/config readiness.</summary>
public sealed class ModbusClientStub : IConnectionStub
{
    private ConnectionStatus _status;

    public string Name => _status.Name;
    public string Transport { get; }
    public string? Host { get; }
    public int Port { get; }
    public byte UnitId { get; }
    public string? RtuPort { get; }
    public int BaudRate { get; }

    public ModbusClientStub(ConnectionStatus status, JsonObject? config)
    {
        _status = status;
        _status.Type = "Modbus";
        Transport = config?["transport"]?.GetValue<string>() ?? config?["Transport"]?.GetValue<string>() ?? "TCP";
        Host = config?["host"]?.GetValue<string>() ?? config?["Host"]?.GetValue<string>();
        Port = config?["port"]?.GetValue<int>() ?? config?["Port"]?.GetValue<int>() ?? 502;
        UnitId = (byte)(config?["unitId"]?.GetValue<int>() ?? config?["UnitId"]?.GetValue<int>() ?? 1);
        RtuPort = config?["rtuPort"]?.GetValue<string>() ?? config?["serialPort"]?.GetValue<string>();
        BaudRate = config?["baudRate"]?.GetValue<int>() ?? config?["BaudRate"]?.GetValue<int>() ?? 9600;
    }

    public void Start()
    {
        _status.Running = true;
        _status.Connected = false;
        _status.Status = Transport == "RTU"
            ? $"Stub (RTU {RtuPort ?? "?"} @ {BaudRate})"
            : $"Stub (TCP {Host ?? "?"}:{Port})";
    }

    public void Stop()
    {
        _status.Running = false;
        _status.Connected = false;
        _status.Status = "Stopped";
    }
}
