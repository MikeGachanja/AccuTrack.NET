using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace Runtime.Modules.Communication;

/// <summary>
/// OPC UA client connection using UA-.NETStandard. Connects to an endpoint,
/// optionally subscribes to tag-mapped nodes and reports value changes.
/// </summary>
public sealed class OpcUaClientConnection : IConnectionStub
{
    private readonly ConnectionStatus _status;
    private readonly JsonObject? _config;
    private ApplicationConfiguration? _appConfig;
    private Session? _session;
    private Subscription? _subscription;
    private readonly List<MonitoredItem> _monitoredItems = new();
    private Action<string, object?>? _onTagValue;
    private volatile bool _running;

    public string Name => _status.Name;
    public string EndpointUrl { get; }

    public OpcUaClientConnection(ConnectionStatus status, JsonObject? config, Action<string, object?>? onTagValue = null)
    {
        _status = status;
        _status.Type = "OPC UA";
        _config = config;
        _onTagValue = onTagValue;
        var cfg = config?["config"] as JsonObject ?? config;
        EndpointUrl = cfg?["endpointUrl"]?.GetValue<string>()
            ?? cfg?["EndpointUrl"]?.GetValue<string>()
            ?? cfg?["endpoint"]?.GetValue<string>()
            ?? config?["endpoint"]?.GetValue<string>()
            ?? cfg?["url"]?.GetValue<string>()
            ?? "opc.tcp://localhost:4840";
    }

    public void SetTagValueCallback(Action<string, object?>? callback) => _onTagValue = callback;

    public void Start()
    {
        _running = true;
        _status.Running = true;
        _status.Connected = false;
        _status.Status = "Connecting...";
        Task.Run(() => ConnectAndSubscribeAsync());
    }

    public void Stop()
    {
        _running = false;
        try
        {
            _subscription?.Delete(true);
            _subscription = null;
            _session?.Close();
            _session?.Dispose();
            _session = null;
        }
        catch { /* ignore */ }
        _status.Connected = false;
        _status.Running = false;
        _status.Status = "Stopped";
    }

    private async Task ConnectAndSubscribeAsync()
    {
        try
        {
            _appConfig = await CreateApplicationConfigurationAsync().ConfigureAwait(false);
            var endpointDescription = new EndpointDescription
            {
                EndpointUrl = EndpointUrl,
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None",
                Server = new ApplicationDescription { ApplicationName = "DarkStar Runtime Server" }
            };
            var endpointConfiguration = EndpointConfiguration.Create(_appConfig);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

            _session = await Session.Create(
                _appConfig,
                configuredEndpoint,
                false,
                _appConfig.ApplicationName,
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null).ConfigureAwait(false);

            _status.Connected = true;
            _status.Status = "Connected";

            var tagMappings = GetTagMappings();
            if (tagMappings.Count > 0 && _onTagValue != null)
            {
                _subscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingInterval = 1000,
                    PublishingEnabled = true
                };
                foreach (var (tagName, nodeIdStr) in tagMappings)
                {
                    if (string.IsNullOrWhiteSpace(nodeIdStr)) continue;
                    try
                    {
                        var nodeId = new NodeId(nodeIdStr);
                        var item = new MonitoredItem(_subscription.DefaultItem)
                        {
                            StartNodeId = nodeId,
                            AttributeId = Attributes.Value,
                            SamplingInterval = 1000,
                            DisplayName = tagName
                        };
                        item.Notification += (monitoredItem, e) =>
                        {
                            if (e.NotificationValue is MonitoredItemNotification n && n.Value?.Value != null)
                                _onTagValue?.Invoke(tagName, n.Value.Value);
                        };
                        _subscription.AddItem(item);
                        _monitoredItems.Add(item);
                    }
                    catch { /* skip invalid node */ }
                }
                _session.AddSubscription(_subscription);
                _subscription.Create();
            }
        }
        catch (Exception ex)
        {
            _status.Connected = false;
            _status.Status = "Error: " + ex.Message;
        }
    }

    private async Task<ApplicationConfiguration> CreateApplicationConfigurationAsync()
    {
        var config = new ApplicationConfiguration
        {
            ApplicationName = "DarkStar Runtime",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier(),
                TrustedPeerCertificates = new CertificateTrustList(),
                TrustedIssuerCertificates = new CertificateTrustList(),
                RejectedCertificateStore = new CertificateTrustList()
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            ClientConfiguration = new ClientConfiguration
            {
                DefaultSessionTimeout = 60000
            },
            DisableHiResClock = true
        };
        await config.Validate(ApplicationType.Client).ConfigureAwait(false);
        return config;
    }

    private List<(string tagName, string nodeId)> GetTagMappings()
    {
        var list = new List<(string, string)>();
        if (_config?["tagMappings"] is not JsonArray arr) return list;
        foreach (var node in arr)
        {
            if (node is not JsonObject obj) continue;
            var tagName = obj["tagName"]?.GetValue<string>() ?? "";
            var address = obj["address"]?.GetValue<string>() ?? obj["nodeId"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(address))
                list.Add((tagName, address));
        }
        return list;
    }

    public bool ReadTag(string nodeIdOrTagName, out object? value)
    {
        value = null;
        if (_session == null || !_session.Connected) return false;
        try
        {
            var nodeId = new NodeId(nodeIdOrTagName);
            var dataValue = _session.ReadValue(nodeId);
            if (dataValue != null && Opc.Ua.StatusCode.IsGood(dataValue.StatusCode))
            {
                value = dataValue.WrappedValue.Value;
                return true;
            }
        }
        catch { }
        return false;
    }

    public bool WriteTag(string nodeIdOrTagName, object? value)
    {
        if (_session == null || !_session.Connected) return false;
        try
        {
            var nodeId = new NodeId(nodeIdOrTagName);
            var writeValue = new WriteValue { NodeId = nodeId, AttributeId = Attributes.Value, Value = new DataValue(value != null ? new Variant(value) : default) };
            _session.Write(null, new WriteValueCollection { writeValue }, out _, out _);
            return true;
        }
        catch { }
        return false;
    }
}
