using System.Text.Json;
using AccuTrack.SDK.Communication;
using Opc.Ua;
using Opc.Ua.Client;

namespace AccuTrack.SDK.OPC;

/// <summary>
/// OPC communication module: ModuleType => "OPC"; config from JSON (endpoint, type UA/DA, securityMode, securityPolicy).
/// Uses OPC Foundation OPC UA .NET client; tag address = NodeId string (e.g. ns=2;s=MyVariable).
/// </summary>
public class OPCModule : CommunicationModuleBase
{
    public override string ModuleType => "OPC";

    private string _endpoint = string.Empty;
    private string _type = "UA";
    private string _securityMode = "None";
    private string _securityPolicy = "None";
    private Session? _session;
    private readonly object _sessionLock = new();

    /// <summary>OPC UA session for read/write by NodeId. Null when not connected.</summary>
    public Session? Session
    {
        get { lock (_sessionLock) return _session; }
    }

    public override bool Configure(JsonElement config)
    {
        if (!ValidateConfiguration(config))
        {
            OnErrorOccurred("Invalid OPC configuration");
            return false;
        }

        Name = config.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
        _endpoint = config.TryGetProperty("endpoint", out var e) ? e.GetString() ?? string.Empty : string.Empty;
        // "type" is reserved for module type (Modbus/OPC/S7); use "opcType" for UA/DA (Plan 3 §9).
        _type = config.TryGetProperty("opcType", out var ot) ? ot.GetString() ?? "UA" : "UA";
        _securityMode = config.TryGetProperty("securityMode", out var sm) ? sm.GetString() ?? "None" : "None";
        _securityPolicy = config.TryGetProperty("securityPolicy", out var sp) ? sp.GetString() ?? "None" : "None";

        SetConfig(config);
        return true;
    }

    public override bool ValidateConfiguration(JsonElement config)
    {
        if (!config.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
            return false;
        var autoMode = config.TryGetProperty("autoMode", out var am) && am.GetBoolean()
            || !config.TryGetProperty("endpoint", out var ep) || ep.GetString() is null or "" or "auto";
        if (!autoMode && (!config.TryGetProperty("endpoint", out _) || config.GetProperty("endpoint").ValueKind != JsonValueKind.String))
            return false;
        return true;
    }

    public override bool Start()
    {
        if (IsRunning) return true;
        if (string.IsNullOrEmpty(_endpoint) || _endpoint == "auto")
        {
            OnErrorOccurred("OPC endpoint not configured");
            return false;
        }

        lock (_sessionLock)
        {
            try
            {
                var config = CreateMinimalApplicationConfiguration();
                var endpointDescription = CoreClientUtils.SelectEndpoint(_endpoint, useSecurity: _securityMode != "None");
                var endpointConfiguration = EndpointConfiguration.Create(config);
                var session = Session.Create(config, new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration), false, "AccuTrack OPC", 60000, null, null).GetAwaiter().GetResult();
                _session = session;
                SetRunning(true);
                OnStatusChanged("Connected");
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex.Message);
                return false;
            }
        }
    }

    public override void Stop()
    {
        if (!IsRunning) return;
        lock (_sessionLock)
        {
            try
            {
                _session?.Close();
                _session?.Dispose();
            }
            catch { }
            _session = null;
        }
        SetRunning(false);
        OnStatusChanged("Disconnected");
    }

    /// <summary>Read a node by NodeId string (e.g. "ns=2;s=MyVariable"). Returns boxed value or null on error.</summary>
    public object? ReadNode(string nodeIdString)
    {
        lock (_sessionLock)
        {
            if (_session == null || !_session.Connected)
                return null;
            try
            {
                var nodeId = NodeId.Parse(nodeIdString);
                var value = _session.ReadValue(nodeId);
                return value?.Value;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Write a node by NodeId string. Returns true on success.</summary>
    public bool WriteNode(string nodeIdString, object? value)
    {
        lock (_sessionLock)
        {
            if (_session == null || !_session.Connected)
                return false;
            try
            {
                var nodeId = NodeId.Parse(nodeIdString);
                var wv = new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(value))
                };
                _session.Write(null, new WriteValueCollection { wv }, out var results, out var diagnosticInfos);
                return results != null && results.Count > 0 && StatusCode.IsGood(results[0]);
            }
            catch
            {
                return false;
            }
        }
    }

    private static ApplicationConfiguration CreateMinimalApplicationConfiguration()
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "AccuTrack OPC Client",
            ApplicationUri = "urn:AccuTrack:SDK:OPC:Client",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier(),
                TrustedPeerCertificates = new CertificateTrustList(),
                TrustedIssuerCertificates = new CertificateTrustList(),
                RejectedCertificateStore = new CertificateTrustList(),
                AutoAcceptUntrustedCertificates = true
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 60000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
        };
        config.Validate(ApplicationType.Client).GetAwaiter().GetResult();
        return config;
    }
}
