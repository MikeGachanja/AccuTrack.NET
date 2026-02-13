using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            
            // Discover endpoints from the server
            _status.Status = "Discovering endpoints...";
            var endpointConfiguration = EndpointConfiguration.Create(_appConfig);
            
            // Use DiscoveryClient to discover endpoints
            EndpointDescriptionCollection? endpoints = null;
            using (var discoveryClient = DiscoveryClient.Create(new Uri(EndpointUrl), endpointConfiguration))
            {
                endpoints = await discoveryClient.GetEndpointsAsync(null).ConfigureAwait(false);
            }
            
            if (endpoints == null || endpoints.Count == 0)
            {
                _status.Status = "Error: Could not discover endpoints from server";
                _status.Connected = false;
                return;
            }

            // Find an endpoint that supports anonymous authentication
            EndpointDescription? anonymousEndpoint = null;
            foreach (var endpoint in endpoints)
            {
                if (endpoint.UserIdentityTokens != null)
                {
                    foreach (var token in endpoint.UserIdentityTokens)
                    {
                        if (token.TokenType == UserTokenType.Anonymous)
                        {
                            anonymousEndpoint = endpoint;
                            break;
                        }
                    }
                }
                if (anonymousEndpoint != null) break;
            }

            // If no anonymous endpoint found, try the first endpoint with None security mode
            if (anonymousEndpoint == null)
            {
                anonymousEndpoint = endpoints.FirstOrDefault(e => e.SecurityMode == MessageSecurityMode.None) 
                    ?? endpoints[0];
            }

            var configuredEndpoint = new ConfiguredEndpoint(null, anonymousEndpoint, endpointConfiguration);

            _status.Status = "Connecting to server...";
            _session = await Session.Create(
                _appConfig,
                configuredEndpoint,
                false,
                _appConfig.ApplicationName,
                60000,
                new UserIdentity(), // Anonymous identity (empty constructor)
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
        // Create certificate store directory if it doesn't exist
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var certStorePath = Path.Combine(appDataPath, "DarkStar", "OPC", "CertificateStores");
        
        // Ensure directories exist
        Directory.CreateDirectory(Path.Combine(certStorePath, "Trusted"));
        Directory.CreateDirectory(Path.Combine(certStorePath, "Issuers"));
        Directory.CreateDirectory(Path.Combine(certStorePath, "Rejected"));
        Directory.CreateDirectory(Path.Combine(certStorePath, "Own"));

        var config = new ApplicationConfiguration
        {
            ApplicationName = "DarkStar Runtime",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(certStorePath, "Own"),
                    SubjectName = "CN=DarkStar Runtime Client"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(certStorePath, "Trusted")
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(certStorePath, "Issuers")
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(certStorePath, "Rejected")
                },
                AutoAcceptUntrustedCertificates = true,
                RejectSHA1SignedCertificates = false
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

    /// <summary>Browse nodes starting from the specified node ID. Returns list of (NodeId, DisplayName) pairs.</summary>
    public List<(string NodeId, string DisplayName)> BrowseNodes(string? startNodeId = null)
    {
        var result = new List<(string, string)>();
        if (_session == null || !_session.Connected) return result;

        try
        {
            var nodeToBrowse = startNodeId != null ? new NodeId(startNodeId) : ObjectIds.ObjectsFolder;
            var browseDescription = new BrowseDescription
            {
                NodeId = nodeToBrowse,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.All
            };

            var browseDescriptionCollection = new BrowseDescriptionCollection { browseDescription };
            _session.Browse(null, null, 0, browseDescriptionCollection, out var results, out var diagnosticInfos);

            if (results != null && results.Count > 0)
            {
                foreach (var reference in results[0].References ?? new ReferenceDescriptionCollection())
                {
                    var nodeId = reference.NodeId.ToString();
                    var displayName = reference.DisplayName?.Text ?? nodeId;
                    result.Add((nodeId, displayName));
                }
            }
        }
        catch (Exception ex)
        {
            // Log error if needed
        }

        return result;
    }

    /// <summary>Recursively browse all nodes starting from the specified node ID.</summary>
    public List<(string NodeId, string DisplayName)> BrowseAllNodes(string? startNodeId = null)
    {
        var result = new List<(string, string)>();
        if (_session == null || !_session.Connected) return result;

        try
        {
            var nodeToBrowse = startNodeId != null ? new NodeId(startNodeId) : ObjectIds.ObjectsFolder;
            BrowseNodeRecursive(nodeToBrowse, result, new HashSet<string>());
        }
        catch { }

        return result;
    }

    private void BrowseNodeRecursive(NodeId nodeId, List<(string, string)> result, HashSet<string> visited)
    {
        if (_session == null || !_session.Connected) return;

        var nodeIdStr = nodeId.ToString();
        if (visited.Contains(nodeIdStr)) return;
        visited.Add(nodeIdStr);

        try
        {
            var browseDescription = new BrowseDescription
            {
                NodeId = nodeId,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.All
            };

            var browseDescriptionCollection = new BrowseDescriptionCollection { browseDescription };
            BrowseResultCollection? results = null;
            DiagnosticInfoCollection? diagnosticInfos = null;
            _session.Browse(null, null, 0, browseDescriptionCollection, out results, out diagnosticInfos);

            if (results != null && results.Count > 0 && results[0].References != null)
            {
                foreach (var reference in results[0].References)
                {
                    var childNodeIdStr = reference.NodeId.ToString();
                    var displayName = reference.DisplayName?.Text ?? childNodeIdStr;
                    
                    // Add to results
                    result.Add((childNodeIdStr, displayName));
                    
                    // Recursively browse children - convert ExpandedNodeId to NodeId
                    try
                    {
                        var childNodeId = ExpandedNodeId.ToNodeId(reference.NodeId, _session.NamespaceUris);
                        if (childNodeId != null && !childNodeId.IsNullNodeId)
                        {
                            BrowseNodeRecursive(childNodeId, result, visited);
                        }
                    }
                    catch { /* Skip if can't browse child */ }
                }
            }
        }
        catch { /* Skip if can't browse this node */ }
    }
}
