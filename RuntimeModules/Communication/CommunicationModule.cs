using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Runtime.Modules.ExecutionEngine;

namespace Runtime.Modules.Communication;

/// <summary>Communication module. GetConnectionStatuses for status bar; Modbus/OPC UA client/server.</summary>
public sealed class CommunicationModule : ModuleBase, ICommunication
{
    private readonly ConcurrentDictionary<string, ConnectionStatus> _statuses = new();
    private readonly List<IConnectionStub> _stubs = new();
    private Action<string, object?>? _tagUpdateCallback;

    public override string ModuleName => "CommunicationModule";
    public override string DisplayName => "Communication Module";

    public override bool Initialize(JsonObject? config = null)
    {
        _statuses.Clear();
        _stubs.Clear();
        if (config != null)
        {
            if (config["communication_modules"] is JsonArray arr)
            {
                foreach (var node in arr)
                {
                    if (node is not JsonObject obj) continue;
                    AddConnection(obj, obj["type"]?.GetValue<string>() ?? "Modbus");
                }
            }
            if (config["modules"] is JsonArray arr2)
            {
                foreach (var node in arr2)
                {
                    if (node is not JsonObject obj) continue;
                    AddConnection(obj, obj["type"]?.GetValue<string>() ?? "Modbus");
                }
            }
        }
        SetStatus("Initialized");
        RaiseInitialized();
        return true;
    }

    private void AddConnection(JsonObject node, string type)
    {
        var name = node["name"]?.GetValue<string>() ?? node["config"]?["name"]?.GetValue<string>() ?? "";
        if (string.IsNullOrEmpty(name)) return;
        var status = new ConnectionStatus
        {
            Name = name,
            Type = type,
            Connected = false,
            Status = "Not Initialized",
            Running = false
        };
        _statuses[name] = status;
        var config = node["config"] as JsonObject ?? node;
        var typeNorm = type.Trim();
        var mode = config["mode"]?.GetValue<string>() ?? node["mode"]?.GetValue<string>() ?? "client";
        
        if (typeNorm.Equals("OPC", StringComparison.OrdinalIgnoreCase) ||
            typeNorm.Equals("OpcUa", StringComparison.OrdinalIgnoreCase) ||
            typeNorm.Equals("OPCUA", StringComparison.OrdinalIgnoreCase) ||
            typeNorm.Equals("OPC UA", StringComparison.OrdinalIgnoreCase))
        {
            if (mode.Equals("server", StringComparison.OrdinalIgnoreCase))
            {
                var opcServer = new OpcUaServerConnection(status, node);
                _stubs.Add(opcServer);
            }
            else
            {
                var opcClient = new OpcUaClientConnection(status, node, _tagUpdateCallback);
                _stubs.Add(opcClient);
            }
        }
        else
        {
            _stubs.Add(new ModbusClientStub(status, config));
        }
    }

    public override bool Start()
    {
        foreach (var stub in _stubs)
            stub.Start();
        SetRunning(true);
        return true;
    }

    public override void Stop()
    {
        foreach (var stub in _stubs)
            stub.Stop();
        SetRunning(false);
    }

    public IReadOnlyDictionary<string, ConnectionStatus> GetConnectionStatuses()
    {
        return new Dictionary<string, ConnectionStatus>(_statuses);
    }

    /// <summary>Set callback for OPC UA tag value updates (tagName, value). Called when subscription data is received.</summary>
    public void SetTagUpdateCallback(Action<string, object?>? callback)
    {
        _tagUpdateCallback = callback;
        foreach (var stub in _stubs)
        {
            if (stub is OpcUaClientConnection opc)
                opc.SetTagValueCallback(callback);
        }
    }

    /// <summary>Get the first OPC UA client connection (for testing/debugging).</summary>
    public OpcUaClientConnection? GetOpcUaClient()
    {
        return _stubs.OfType<OpcUaClientConnection>().FirstOrDefault();
    }

    /// <summary>Get the first OPC UA server connection (for testing/debugging).</summary>
    public OpcUaServerConnection? GetOpcUaServer()
    {
        return _stubs.OfType<OpcUaServerConnection>().FirstOrDefault();
    }
}
