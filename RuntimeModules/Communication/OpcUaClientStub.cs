using System.Text.Json.Nodes;

namespace Runtime.Modules.Communication;

/// <summary>Stub OPC UA client. Parses endpoint config; no real connection. For pipeline/config readiness.</summary>
public sealed class OpcUaClientStub : IConnectionStub
{
    private ConnectionStatus _status;

    public string Name => _status.Name;
    public string EndpointUrl { get; }

    public OpcUaClientStub(ConnectionStatus status, JsonObject? config)
    {
        _status = status;
        _status.Type = "OPC UA";
        EndpointUrl = config?["endpointUrl"]?.GetValue<string>()
            ?? config?["EndpointUrl"]?.GetValue<string>()
            ?? config?["url"]?.GetValue<string>()
            ?? "opc.tcp://localhost:4840";
    }

    public void Start()
    {
        _status.Running = true;
        _status.Connected = false;
        _status.Status = $"Stub ({EndpointUrl})";
    }

    public void Stop()
    {
        _status.Running = false;
        _status.Connected = false;
        _status.Status = "Stopped";
    }
}
