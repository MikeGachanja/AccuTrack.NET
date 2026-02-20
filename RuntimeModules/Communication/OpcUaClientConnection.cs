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
    private readonly Dictionary<string, MonitoredItem> _subscribedNodes = new(); // NodeId -> MonitoredItem
    private Action<string, object?>? _onTagValue;
    private Action<string, object?, DateTime>? _onSubscribedValue; // NodeId, Value, Timestamp
    private volatile bool _running;
    private readonly List<(string NodeId, string DisplayName)> _allNodes = new();
    private readonly object _nodesLock = new();
    private Func<List<(string tagName, string address)>>? _getTagsFunc;

    public string Name => _status.Name;
    public string EndpointUrl { get; }

    public void SetTagProvider(Func<List<(string tagName, string address)>>? getTagsFunc)
    {
        _getTagsFunc = getTagsFunc;
    }

    public OpcUaClientConnection(ConnectionStatus status, JsonObject? config, Action<string, object?>? onTagValue = null)
    {
        _status = status;
        _status.Type = "OPC UA";
        _config = config;
        _onTagValue = onTagValue;
        // Prefer config, then settings (Designer uses "settings"), then node itself
        var cfg = config?["config"] as JsonObject ?? config?["settings"] as JsonObject ?? config;
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
        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Start: Starting OPC UA client connection '{Name}' to endpoint '{EndpointUrl}'");
        _running = true;
        _status.Running = true;
        _status.Connected = false;
        _status.Status = "Connecting...";
        Task.Run(() => ConnectAndSubscribeAsync());
    }

    public void Stop()
    {
        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Stop: Stopping OPC UA client connection '{Name}'");
        _running = false;
        try
        {
            if (_subscription != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Stop: Deleting subscription (ID: {_subscription.Id})");
                _subscription.Delete(true);
                _subscription = null;
            }
            
            if (_session != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Stop: Closing session (ID: {_session.SessionId})");
                _session.Close();
                _session.Dispose();
                _session = null;
            }
            
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Stop: Successfully stopped OPC UA client connection");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Stop: Error during stop: {ex.GetType().Name} - {ex.Message}");
        }
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
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Discovering endpoints from '{EndpointUrl}'");
            var endpointConfiguration = EndpointConfiguration.Create(_appConfig);
            
            // Use DiscoveryClient to discover endpoints
            EndpointDescriptionCollection? endpoints = null;
            using (var discoveryClient = DiscoveryClient.Create(new Uri(EndpointUrl), endpointConfiguration))
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Calling GetEndpointsAsync...");
                endpoints = await discoveryClient.GetEndpointsAsync(null).ConfigureAwait(false);
            }
            
            if (endpoints == null || endpoints.Count == 0)
            {
                _status.Status = "Error: Could not discover endpoints from server";
                _status.Connected = false;
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: ERROR - No endpoints discovered from server");
                return;
            }
            
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Discovered {endpoints.Count} endpoint(s)");
            for (int i = 0; i < endpoints.Count; i++)
            {
                var ep = endpoints[i];
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Endpoint {i + 1}: SecurityMode={ep.SecurityMode}, SecurityPolicyUri={ep.SecurityPolicyUri}, EndpointUrl={ep.EndpointUrl}");
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
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Found anonymous endpoint: {endpoint.EndpointUrl}");
                            break;
                        }
                    }
                }
                if (anonymousEndpoint != null) break;
            }

            // If no anonymous endpoint found, try the first endpoint with None security mode
            if (anonymousEndpoint == null)
            {
                anonymousEndpoint = endpoints.FirstOrDefault(e => e.SecurityMode == MessageSecurityMode.None);
                if (anonymousEndpoint != null)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Using endpoint with None security mode: {anonymousEndpoint.EndpointUrl}");
                }
                else
                {
                    anonymousEndpoint = endpoints[0];
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Using first available endpoint: {anonymousEndpoint.EndpointUrl}");
                }
            }

            var configuredEndpoint = new ConfiguredEndpoint(null, anonymousEndpoint, endpointConfiguration);

            _status.Status = "Connecting to server...";
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Creating session to endpoint '{anonymousEndpoint.EndpointUrl}'...");
            _session = await Session.Create(
                _appConfig,
                configuredEndpoint,
                false,
                _appConfig.ApplicationName,
                60000,
                new UserIdentity(), // Anonymous identity (empty constructor)
                null).ConfigureAwait(false);
            
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Session created successfully");

            // Set up KeepAlive handler to maintain connection and process subscriptions
            _session.KeepAlive += (session, e) =>
            {
                if (e.CurrentState == ServerState.Unknown || e.CurrentState == ServerState.Shutdown)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] KeepAlive: Server state changed to {e.CurrentState}, connection may be lost");
                    _status.Connected = false;
                    _status.Status = $"Disconnected (Server state: {e.CurrentState})";
                }
                else if (e.CurrentState == ServerState.Running)
                {
                    _status.Connected = true;
                }
            };

            _status.Connected = true;
            _status.Status = "Connected";
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully connected to OPC UA server at {EndpointUrl}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Session ID: {_session.SessionId}, Session Name: {_session.SessionName}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Endpoint URL: {_session.Endpoint?.EndpointUrl ?? EndpointUrl}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Server Description: {_session.Endpoint?.Server?.ApplicationName?.Text ?? "Unknown"}");

            // Fetch all nodes from the server
            _status.Status = "Browsing nodes...";
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Starting to browse all nodes from server...");
            try
            {
                var allNodes = BrowseAllNodes();
                lock (_nodesLock)
                {
                    _allNodes.Clear();
                    _allNodes.AddRange(allNodes);
                }
                _status.Status = $"Connected ({_allNodes.Count} nodes discovered)";
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully browsed {_allNodes.Count} nodes from server");
                
                // Log first 20 nodes for debugging (to help identify available NodeIds)
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Sample of available nodes (first 20):");
                foreach (var node in _allNodes.Take(20))
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection]   - NodeId: {node.NodeId}, DisplayName: {node.DisplayName}");
                }
                if (_allNodes.Count > 20)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection]   ... and {_allNodes.Count - 20} more nodes");
                }
            }
            catch (Exception ex)
            {
                _status.Status = $"Connected (node browsing failed: {ex.Message})";
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Node browsing failed: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: StackTrace: {ex.StackTrace}");
            }

            // Subscribe to tag-mapped nodes (only those that exist in Tags)
            var tagMappings = GetTagMappings();
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Found {tagMappings.Count} tag mappings to subscribe to");
            
            if (tagMappings.Count > 0 && _onTagValue != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Creating subscription with PublishingInterval=1000ms");
                _subscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingInterval = 1000,
                    PublishingEnabled = true
                };
                
                int subscribedCount = 0;
                int failedCount = 0;
                
                // Group by tag name to avoid duplicate subscriptions
                var uniqueTagMappings = tagMappings
                    .GroupBy(m => m.tagName)
                    .Select(g => g.First())
                    .ToList();
                
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Processing {uniqueTagMappings.Count} unique tag mappings (from {tagMappings.Count} total)");
                
                foreach (var (tagName, nodeIdStr) in uniqueTagMappings)
                {
                    if (string.IsNullOrWhiteSpace(nodeIdStr))
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Skipping tag '{tagName}' - empty nodeId");
                        continue;
                    }
                    
                    // Since tag name == tag address, try tag name first, then nodeIdStr
                    // Try multiple resolution strategies
                    NodeId? nodeId = null;
                    string? resolvedNodeIdStr = null;
                    
                    // Strategy 1: Try tag name directly as NodeId (since tag name == tag address)
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Attempting to subscribe to tag '{tagName}' - trying tag name as NodeId first");
                    nodeId = TryResolveNodeId(tagName);
                    if (nodeId != null && !nodeId.IsNullNodeId)
                    {
                        resolvedNodeIdStr = tagName;
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully resolved tag name '{tagName}' as NodeId: {nodeId}");
                    }
                    else
                    {
                        // Strategy 2: Try the provided nodeIdStr
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Tag name resolution failed, trying provided NodeId '{nodeIdStr}'");
                        nodeId = TryResolveNodeId(nodeIdStr);
                        if (nodeId != null && !nodeId.IsNullNodeId)
                        {
                            resolvedNodeIdStr = nodeIdStr;
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully resolved NodeId '{nodeIdStr}' to {nodeId}");
                        }
                    }
                    
                    if (nodeId == null || nodeId.IsNullNodeId)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Failed to resolve NodeId for tag '{tagName}' (tried '{tagName}' and '{nodeIdStr}')");
                        failedCount++;
                        continue;
                    }
                    
                    // Verify the node exists in browsed nodes (optional check)
                    bool nodeExists = false;
                    lock (_nodesLock)
                    {
                        nodeExists = _allNodes.Any(n => 
                            n.NodeId.Equals(resolvedNodeIdStr, StringComparison.OrdinalIgnoreCase) ||
                            n.DisplayName.Equals(tagName, StringComparison.OrdinalIgnoreCase) ||
                            n.DisplayName.Equals(resolvedNodeIdStr, StringComparison.OrdinalIgnoreCase));
                    }
                    
                    if (!nodeExists)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Node '{resolvedNodeIdStr}' (tag: '{tagName}') not found in browsed nodes, but will attempt subscription anyway");
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Node '{resolvedNodeIdStr}' (tag: '{tagName}') found in browsed nodes");
                    }
                    
                    // Verify the node exists and is readable before subscribing
                    try
                    {
                        var testRead = _session.ReadValue(nodeId);
                        if (testRead != null)
                        {
                            var statusCode = testRead.StatusCode;
                            var readValue = testRead.WrappedValue.Value;
                            var sourceTimestamp = testRead.SourceTimestamp;
                            var serverTimestamp = testRead.ServerTimestamp;
                            
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Test read for tag '{tagName}' (NodeId: {nodeId}, resolved from '{resolvedNodeIdStr}'):");
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync:   StatusCode: 0x{(uint)statusCode:X8} ({(Opc.Ua.StatusCode.IsGood(statusCode) ? "Good" : "Bad")})");
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync:   Value: '{readValue}' (Type: {readValue?.GetType().Name ?? "null"})");
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync:   SourceTimestamp: {sourceTimestamp}");
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync:   ServerTimestamp: {serverTimestamp}");
                            
                            if (Opc.Ua.StatusCode.IsGood(statusCode))
                            {
                                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Verified node '{nodeId}' (resolved from '{resolvedNodeIdStr}') is readable. Current value: {readValue}");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: WARNING - Node '{nodeId}' (resolved from '{resolvedNodeIdStr}') is not readable. StatusCode: 0x{(uint)statusCode:X8}. Subscription may fail.");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: WARNING - Test read returned null for node '{nodeId}' (resolved from '{resolvedNodeIdStr}')");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: WARNING - Failed to verify node '{nodeId}' (resolved from '{resolvedNodeIdStr}') before subscribing: {ex.GetType().Name} - {ex.Message}");
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Exception StackTrace: {ex.StackTrace}");
                    }
                    
                    try
                    {
                        var item = new MonitoredItem(_subscription.DefaultItem)
                        {
                            StartNodeId = nodeId,
                            AttributeId = Attributes.Value,
                            SamplingInterval = 1000,
                            DisplayName = tagName
                        };
                        
                        // Capture both tagName and resolvedNodeIdStr for the notification handler
                        var capturedTagName = tagName;
                        var capturedNodeIdStr = resolvedNodeIdStr ?? nodeIdStr;
                        
                        item.Notification += (monitoredItem, e) =>
                        {
                            // Process notification on background thread to avoid blocking the OPC UA notification handler
                            Task.Run(() =>
                            {
                                try
                                {
                                    if (e.NotificationValue is MonitoredItemNotification n && n.Value?.Value != null)
                                    {
                                        var value = n.Value.Value;
                                        var statusCode = n.Value.StatusCode;
                                        var sourceTimestamp = n.Value.SourceTimestamp;
                                        
                                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification (Background Thread): Tag '{capturedTagName}' (NodeId: {monitoredItem.StartNodeId}) changed to '{value}' (Type: {value.GetType().Name}, StatusCode: 0x{(uint)statusCode:X8}, Timestamp: {sourceTimestamp})");
                                        
                                        if (Opc.Ua.StatusCode.IsGood(statusCode))
                                        {
                                            // Try to resolve tag by name first, then by address/NodeId
                                            // Since tag name == tag address, either should work
                                            string? resolvedTagName = capturedTagName;
                                            
                                            // If tag name doesn't work, try finding by NodeId/address
                                            if (_getTagsFunc != null)
                                            {
                                                try
                                                {
                                                    var tags = _getTagsFunc();
                                                    // Check if tag name exists
                                                    bool tagExists = tags.Any(t => t.tagName.Equals(capturedTagName, StringComparison.OrdinalIgnoreCase));
                                                    
                                                    if (!tagExists)
                                                    {
                                                        // Try to find by address/NodeId
                                                        var matchingTag = tags.FirstOrDefault(t => 
                                                            (!string.IsNullOrEmpty(t.address) && t.address.Equals(capturedNodeIdStr, StringComparison.OrdinalIgnoreCase)) ||
                                                            t.tagName.Equals(capturedNodeIdStr, StringComparison.OrdinalIgnoreCase));
                                                        
                                                        if (!string.IsNullOrEmpty(matchingTag.tagName))
                                                        {
                                                            resolvedTagName = matchingTag.tagName;
                                                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Resolved tag by address/NodeId '{capturedNodeIdStr}' to tag name '{resolvedTagName}'");
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Error resolving tag: {ex.Message}");
                                                }
                                            }
                                            
                                            // Invoke callback with resolved tag name on background thread
                                            if (!string.IsNullOrEmpty(resolvedTagName))
                                            {
                                                try
                                                {
                                                    _onTagValue?.Invoke(resolvedTagName, value);
                                                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Successfully invoked callback for tag '{resolvedTagName}' with value '{value}'");
                                                }
                                                catch (Exception ex)
                                                {
                                                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Error invoking callback for tag '{resolvedTagName}': {ex.GetType().Name} - {ex.Message}");
                                                }
                                            }
                                            else
                                            {
                                                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Could not resolve tag name for NodeId '{capturedNodeIdStr}', tag name '{capturedTagName}'");
                                            }
                                        }
                                        else
                                        {
                                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Tag '{capturedTagName}' has bad status code 0x{(uint)statusCode:X8}, not invoking callback");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Tag '{capturedTagName}' received notification but value is null or invalid");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: Exception processing notification for tag '{capturedTagName}': {ex.GetType().Name} - {ex.Message}");
                                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] Subscription Notification: StackTrace: {ex.StackTrace}");
                                }
                            });
                        };
                        
                        _subscription.AddItem(item);
                        _monitoredItems.Add(item);
                        subscribedCount++;
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully added subscription for tag '{tagName}'");
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Failed to subscribe to tag '{tagName}' with NodeId '{nodeIdStr}': {ex.GetType().Name} - {ex.Message}");
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: StackTrace: {ex.StackTrace}");
                    }
                }
                
                if (subscribedCount > 0)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Adding subscription to session ({subscribedCount} items, {failedCount} failed)");
                    _session.AddSubscription(_subscription);
                    _subscription.Create();
                    
                    // Log subscription creation details
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Subscription created successfully. Subscription ID: {_subscription.Id}, PublishingInterval: {_subscription.PublishingInterval}ms");
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Created {_monitoredItems.Count} monitored items. Status will be determined by notification callbacks.");
                    
                    // Log each monitored item for debugging
                    foreach (var item in _monitoredItems)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: MonitoredItem '{item.DisplayName}' (NodeId: {item.StartNodeId}, ClientHandle: {item.ClientHandle})");
                    }
                    
                    _status.Status = $"Connected ({_allNodes.Count} nodes, {subscribedCount} tags subscribed)";
                    
                    // After setting up subscriptions, do an initial scan to read all mapped tag values
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Starting initial scan of all mapped tag values...");
                    ScanAndReadAllTagValues(uniqueTagMappings);
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: No tags successfully subscribed ({failedCount} failed)");
                    _status.Status = $"Connected ({_allNodes.Count} nodes discovered, {failedCount} subscription failures)";
                }
            }
            else
            {
                if (tagMappings.Count == 0)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: No tag mappings found in configuration");
                }
                else if (_onTagValue != null)
                {
                    // Even if subscriptions failed, try to read initial values
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Subscriptions not set up, but scanning initial tag values anyway...");
                    var uniqueTagMappings = tagMappings
                        .GroupBy(m => m.tagName)
                        .Select(g => g.First())
                        .ToList();
                    ScanAndReadAllTagValues(uniqueTagMappings);
                }
                if (_onTagValue == null)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Tag update callback is null - subscriptions disabled");
                }
                _status.Status = $"Connected ({_allNodes.Count} nodes discovered)";
            }
            
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Connection setup complete. Connected: {_status.Connected}, Status: {_status.Status}");
        }
        catch (Exception ex)
        {
            _status.Connected = false;
            _status.Status = "Error: " + ex.Message;
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Connection failed with exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
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
        var list = new List<(string tagName, string nodeId)>();
        
        // First, try to get tag mappings from config (legacy/explicit mappings)
        if (_config?["tagMappings"] is JsonArray configArr)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: Found {configArr.Count} tag mappings in config");
            foreach (var node in configArr)
            {
                if (node is not JsonObject obj) continue;
                var tagName = obj["tagName"]?.GetValue<string>() ?? "";
                var address = obj["address"]?.GetValue<string>() ?? obj["nodeId"]?.GetValue<string>() ?? "";
                if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(address))
                {
                    list.Add((tagName, address));
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: Added mapping from config - Tag: '{tagName}', Address: '{address}'");
                }
            }
        }
        
        // Also get tags from TagProvider function (tags loaded from JSON)
        if (_getTagsFunc != null)
        {
            try
            {
                var tags = _getTagsFunc();
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: TagProvider returned {tags.Count} tags");
                
                if (tags.Count == 0)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: WARNING - TagProvider returned 0 tags. Tags may not be loaded yet.");
                }
                
                foreach (var (tagName, address) in tags)
                {
                    // Skip if already in list (from config)
                    if (list.Any(m => m.tagName == tagName))
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: Tag '{tagName}' already mapped from config, skipping");
                        continue;
                    }
                    
                    // Since tag name == tag address, try both:
                    // 1. First try tag name as NodeId (most common case)
                    // 2. Then try address as NodeId
                    // 3. Fall back to tag name if address is empty
                    var nodeId = !string.IsNullOrEmpty(tagName) ? tagName : (!string.IsNullOrEmpty(address) ? address : tagName);
                    
                    // Also add address-based mapping if address is different from tag name
                    if (!string.IsNullOrEmpty(address) && !address.Equals(tagName, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add((tagName, address)); // Add address-based mapping
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: Added mapping from TagProvider - Tag: '{tagName}', NodeId: '{address}' (by address)");
                    }
                    
                    // Always add tag name-based mapping (tag name == tag address)
                    list.Add((tagName, nodeId));
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: Added mapping from TagProvider - Tag: '{tagName}', NodeId: '{nodeId}' (by tag name)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: Error getting tags from TagProvider: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: StackTrace: {ex.StackTrace}");
            }
        }
        else
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: TagProvider is null - cannot get tags from TagProvider");
        }
        
        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] GetTagMappings: Total mappings: {list.Count}");
        return list;
    }

    /// <summary>Scan and read all mapped tag values to update global tag values immediately.</summary>
    private void ScanAndReadAllTagValues(List<(string tagName, string nodeId)> tagMappings)
    {
        if (_session == null || !_session.Connected || _onTagValue == null)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Cannot scan - session is null or not connected, or callback is null");
            return;
        }

        if (tagMappings.Count == 0)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: No tag mappings to scan");
            return;
        }

        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Starting scan of {tagMappings.Count} mapped tags...");
        
        int successCount = 0;
        int failedCount = 0;

        // Use batch read for efficiency if possible, otherwise read individually
        try
        {
            var nodeIdsToRead = new List<NodeId>();
            var tagNamesForNodes = new List<string>();
            
            // First, resolve all node IDs
            foreach (var (tagName, nodeIdStr) in tagMappings)
            {
                if (string.IsNullOrWhiteSpace(nodeIdStr))
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Skipping tag '{tagName}' - nodeId is empty");
                    failedCount++;
                    continue;
                }

                try
                {
                    var resolvedNodeId = TryResolveNodeId(nodeIdStr);
                    if (resolvedNodeId == null || resolvedNodeId.IsNullNodeId)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Could not resolve NodeId for tag '{tagName}' (nodeId: '{nodeIdStr}')");
                        failedCount++;
                        continue;
                    }

                    nodeIdsToRead.Add(resolvedNodeId);
                    tagNamesForNodes.Add(tagName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Error resolving NodeId for tag '{tagName}' (nodeId: '{nodeIdStr}'): {ex.Message}");
                    failedCount++;
                }
            }

            if (nodeIdsToRead.Count == 0)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: No valid node IDs to read");
                return;
            }

            // Read values individually (can be optimized to batch read later if needed)
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Reading {nodeIdsToRead.Count} nodes...");
            
            for (int i = 0; i < nodeIdsToRead.Count; i++)
            {
                var nodeId = nodeIdsToRead[i];
                var tagName = tagNamesForNodes[i];
                
                try
                {
                    var dataValue = _session.ReadValue(nodeId);
                    if (dataValue != null && Opc.Ua.StatusCode.IsGood(dataValue.StatusCode))
                    {
                        var value = dataValue.WrappedValue.Value;
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Successfully read tag '{tagName}' (NodeId: {nodeId}) = '{value}'");
                        
                        try
                        {
                            _onTagValue(tagName, value);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Error invoking callback for tag '{tagName}': {ex.Message}");
                            failedCount++;
                        }
                    }
                    else
                    {
                        var statusCode = dataValue?.StatusCode ?? StatusCodes.Bad;
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Failed to read tag '{tagName}' (NodeId: {nodeId}) - StatusCode: 0x{(uint)statusCode:X8}");
                        failedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Exception reading tag '{tagName}' (NodeId: {nodeId}): {ex.Message}");
                    failedCount++;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Exception during batch read, falling back to individual reads: {ex.Message}");
            
            // Fallback to individual reads
            foreach (var (tagName, nodeIdStr) in tagMappings)
            {
                if (string.IsNullOrWhiteSpace(nodeIdStr)) continue;
                
                try
                {
                    if (ReadTag(nodeIdStr, out var value))
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Successfully read tag '{tagName}' = '{value}'");
                        _onTagValue(tagName, value);
                        successCount++;
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Failed to read tag '{tagName}' (nodeId: '{nodeIdStr}')");
                        failedCount++;
                    }
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Exception reading tag '{tagName}': {ex2.Message}");
                    failedCount++;
                }
            }
        }

        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ScanAndReadAllTagValues: Scan complete - {successCount} succeeded, {failedCount} failed out of {tagMappings.Count} total tags");
    }

    /// <summary>Find tag name by NodeId/address - used for resolving notifications that come with NodeId instead of tag name.</summary>
    private string? FindTagNameByNodeId(string nodeId)
    {
        if (_getTagsFunc == null) return null;
        
        try
        {
            var tags = _getTagsFunc();
            foreach (var (tagName, address) in tags)
            {
                // Match by address (NodeId)
                if (!string.IsNullOrEmpty(address) && address.Equals(nodeId, StringComparison.OrdinalIgnoreCase))
                {
                    return tagName;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] FindTagNameByNodeId: Error finding tag for NodeId '{nodeId}': {ex.Message}");
        }
        
        return null;
    }

    public bool ReadTag(string nodeIdOrTagName, out object? value)
    {
        value = null;
        if (_session == null || !_session.Connected)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag failed: Session is null or not connected. NodeId: {nodeIdOrTagName}");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(nodeIdOrTagName))
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag failed: NodeId is null or empty");
            return false;
        }
        
        try
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag: Attempting to read NodeId='{nodeIdOrTagName}'");
            
            NodeId nodeId = TryResolveNodeId(nodeIdOrTagName);
            if (nodeId == null || nodeId.IsNullNodeId)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag: Could not resolve NodeId for '{nodeIdOrTagName}'. Returning false.");
                return false;
            }
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag: NodeId resolved: {nodeId}");
            
            var dataValue = _session.ReadValue(nodeId);
            if (dataValue != null)
            {
                var statusCode = dataValue.StatusCode;
                var readValue = dataValue.WrappedValue.Value;
                var sourceTimestamp = dataValue.SourceTimestamp;
                var serverTimestamp = dataValue.ServerTimestamp;
                
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag: Read result for NodeId '{nodeIdOrTagName}' (resolved: {nodeId}):");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag:   StatusCode: 0x{(uint)statusCode:X8} ({(Opc.Ua.StatusCode.IsGood(statusCode) ? "Good" : "Bad")})");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag:   Value: '{readValue}' (Type: {readValue?.GetType().Name ?? "null"})");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag:   SourceTimestamp: {sourceTimestamp}");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag:   ServerTimestamp: {serverTimestamp}");
                
                if (Opc.Ua.StatusCode.IsGood(statusCode))
                {
                    value = readValue;
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag: Successfully read value '{value}' from NodeId '{nodeIdOrTagName}'");
                    return true;
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag: Read failed with StatusCode: 0x{(uint)statusCode:X8} for NodeId '{nodeIdOrTagName}'");
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag: Read returned null DataValue for NodeId '{nodeIdOrTagName}'");
            }
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException ParamName: {ex.ParamName}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag Exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag Exception StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] ReadTag Exception InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
        }
        return false;
    }

    public bool WriteTag(string nodeIdOrTagName, object? value)
    {
        if (_session == null || !_session.Connected)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag failed: Session is null or not connected. NodeId: {nodeIdOrTagName}, Value: {value}");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(nodeIdOrTagName))
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag failed: NodeId is null or empty. Value: {value}");
            return false;
        }
        
        try
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Attempting to write NodeId='{nodeIdOrTagName}', Value='{value}' (Type: {value?.GetType().Name ?? "null"})");
            
            NodeId nodeId = TryResolveNodeId(nodeIdOrTagName);
            if (nodeId == null || nodeId.IsNullNodeId)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Could not resolve NodeId for '{nodeIdOrTagName}'. Returning false.");
                return false;
            }
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: NodeId resolved: {nodeId}");
            
            Variant variant;
            if (value != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Creating Variant from value type {value.GetType().Name}");
                
                // Convert value to a type that OPC UA supports
                object? convertedValue = value;
                
                // Handle common type conversions - preserve boolean types explicitly
                if (value is bool boolValue)
                {
                    convertedValue = boolValue;
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Preserving boolean value: {boolValue}");
                }
                else if (value is string strValue)
                {
                    convertedValue = strValue;
                }
                else if (value is int intValue)
                {
                    convertedValue = intValue;
                }
                else if (value is long longValue)
                {
                    // OPC UA might prefer Int32, try to convert if safe
                    if (longValue >= int.MinValue && longValue <= int.MaxValue)
                    {
                        convertedValue = (int)longValue;
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Converted long to int: {longValue} -> {convertedValue}");
                    }
                    else
                    {
                        convertedValue = longValue;
                    }
                }
                else if (value is float floatValue)
                {
                    convertedValue = floatValue;
                }
                else if (value is double doubleValue)
                {
                    convertedValue = doubleValue;
                }
                else if (value is decimal decimalValue)
                {
                    // Convert decimal to double
                    convertedValue = (double)decimalValue;
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Converted decimal to double: {decimalValue} -> {convertedValue}");
                }
                else if (value is DateTime dateTimeValue)
                {
                    convertedValue = dateTimeValue;
                }
                else
                {
                    // Try to convert to string as fallback
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Unsupported type {value.GetType().Name}, converting to string");
                    convertedValue = value.ToString();
                }
                
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Creating Variant with converted value type {convertedValue?.GetType().Name}");
                variant = new Variant(convertedValue);
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Variant created successfully. Type: {variant.TypeInfo?.BuiltInType}");
            }
            else
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Creating default Variant for null value");
                variant = default;
            }
            
            var dataValue = new DataValue(variant);
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: DataValue created successfully");
            
            var writeValue = new WriteValue 
            { 
                NodeId = nodeId, 
                AttributeId = Attributes.Value, 
                Value = dataValue 
            };
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: WriteValue created successfully");
            
            var writeValues = new WriteValueCollection { writeValue };
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Calling session.Write...");
            
            _session.Write(null, writeValues, out var results, out var diagnosticInfos);
            
            if (results != null && results.Count > 0)
            {
                var statusCode = results[0];
                if (Opc.Ua.StatusCode.IsGood(statusCode))
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Successfully wrote value '{value}' to NodeId '{nodeIdOrTagName}'");
                    return true;
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Write failed with StatusCode: 0x{(uint)statusCode:X8} for NodeId '{nodeIdOrTagName}'");
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag: Write returned no results");
            }
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException ParamName: {ex.ParamName}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException StackTrace: {ex.StackTrace}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException Details - NodeId: '{nodeIdOrTagName}', Value: '{value}', ValueType: {value?.GetType().Name ?? "null"}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException InnerException StackTrace: {ex.InnerException.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag Exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag Exception StackTrace: {ex.StackTrace}");
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag Exception Details - NodeId: '{nodeIdOrTagName}', Value: '{value}', ValueType: {value?.GetType().Name ?? "null"}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag Exception InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] WriteTag Exception InnerException StackTrace: {ex.InnerException.StackTrace}");
            }
        }
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

    /// <summary>Get all browsed nodes from the server.</summary>
    public IReadOnlyList<(string NodeId, string DisplayName)> GetAllNodes()
    {
        lock (_nodesLock)
        {
            return _allNodes.ToList();
        }
    }

    /// <summary>Try to resolve a NodeId from a string identifier. Tries multiple formats and also searches browsed nodes.</summary>
    private NodeId? TryResolveNodeId(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        // Strategy 1: Try parsing as-is (for fully qualified NodeIds like "ns=2;s=MyTag" or "ns=2;i=123")
        try
        {
            var nodeId = new NodeId(identifier);
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Successfully parsed as NodeId: {nodeId}");
            return nodeId;
        }
        catch (ArgumentException)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to parse '{identifier}' as NodeId, trying alternatives...");
        }

        // Strategy 2: Search browsed nodes by DisplayName (case-insensitive)
        lock (_nodesLock)
        {
            var matchingNode = _allNodes.FirstOrDefault(n => 
                n.DisplayName.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(matchingNode.NodeId))
            {
                try
                {
                    var nodeId = new NodeId(matchingNode.NodeId);
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found node by DisplayName '{identifier}' -> NodeId: {nodeId}");
                    return nodeId;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to parse found NodeId '{matchingNode.NodeId}': {ex.Message}");
                }
            }
            
            // Strategy 2b: Search browsed nodes by DisplayName containing the identifier (for partial matches)
            // This helps with cases like "mFIOPause" matching "iFIOPaused" or "FIOPause"
            // Try matching the core name (without prefix like 'm' or 'i')
            string coreIdentifier = identifier;
            if (coreIdentifier.Length > 1 && char.IsLetter(coreIdentifier[0]) && char.IsUpper(coreIdentifier[1]))
            {
                coreIdentifier = coreIdentifier.Substring(1); // Remove first character if it's a prefix
            }
            
            matchingNode = _allNodes.FirstOrDefault(n => 
            {
                string nodeCoreName = n.DisplayName;
                if (nodeCoreName.Length > 1 && char.IsLetter(nodeCoreName[0]) && char.IsUpper(nodeCoreName[1]))
                {
                    nodeCoreName = nodeCoreName.Substring(1); // Remove prefix
                }
                
                return nodeCoreName.Equals(coreIdentifier, StringComparison.OrdinalIgnoreCase) ||
                       n.DisplayName.Contains(coreIdentifier, StringComparison.OrdinalIgnoreCase) ||
                       coreIdentifier.Contains(nodeCoreName, StringComparison.OrdinalIgnoreCase) ||
                       n.DisplayName.EndsWith(coreIdentifier, StringComparison.OrdinalIgnoreCase) ||
                       coreIdentifier.EndsWith(nodeCoreName, StringComparison.OrdinalIgnoreCase);
            });
            
            if (!string.IsNullOrEmpty(matchingNode.NodeId))
            {
                try
                {
                    var nodeId = new NodeId(matchingNode.NodeId);
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found node by DisplayName partial match '{identifier}' (core: '{coreIdentifier}') -> NodeId: {nodeId} (DisplayName: {matchingNode.DisplayName})");
                    return nodeId;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to parse found NodeId '{matchingNode.NodeId}': {ex.Message}");
                }
            }
            
            // Strategy 2c: Search browsed nodes by NodeId string containing the identifier
            matchingNode = _allNodes.FirstOrDefault(n => 
                n.NodeId.Contains(identifier, StringComparison.OrdinalIgnoreCase) ||
                n.NodeId.Contains(coreIdentifier, StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(matchingNode.NodeId))
            {
                try
                {
                    var nodeId = new NodeId(matchingNode.NodeId);
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found node by NodeId partial match '{identifier}' -> NodeId: {nodeId} (DisplayName: {matchingNode.DisplayName})");
                    return nodeId;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to parse found NodeId '{matchingNode.NodeId}': {ex.Message}");
                }
            }
        }

        // Strategy 2d: Try as string NodeId in namespace 0 (default namespace) - only if no browsed node match found
        try
        {
            var nodeId = new NodeId(identifier, 0);
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Created string NodeId in namespace 0: {nodeId} (no browsed node match found)");
            return nodeId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to create string NodeId in namespace 0: {ex.Message}");
        }

        // Strategy 4: Check if identifier matches a tag name or address from TagProvider
        if (_getTagsFunc != null)
        {
            try
            {
                var tags = _getTagsFunc();
                var matchingTag = tags.FirstOrDefault(t => 
                    t.tagName.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(t.address) && t.address.Equals(identifier, StringComparison.OrdinalIgnoreCase)));
                
                if (!string.IsNullOrEmpty(matchingTag.tagName))
                {
                    // Use the tag's address if available, otherwise use the tag name
                    var nodeIdStr = !string.IsNullOrEmpty(matchingTag.address) ? matchingTag.address : matchingTag.tagName;
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching tag '{matchingTag.tagName}' with address '{matchingTag.address}', using NodeId: '{nodeIdStr}'");
                    
                    // First try to find matching node in browsed nodes by DisplayName or NodeId
                    lock (_nodesLock)
                    {
                        // Try exact matches first
                        var matchingNode = _allNodes.FirstOrDefault(n => 
                            n.DisplayName.Equals(nodeIdStr, StringComparison.OrdinalIgnoreCase) ||
                            n.NodeId.Equals(nodeIdStr, StringComparison.OrdinalIgnoreCase) ||
                            n.DisplayName.Equals(matchingTag.tagName, StringComparison.OrdinalIgnoreCase));
                        
                        // If no exact match, try core name matching (ignoring prefixes)
                        if (string.IsNullOrEmpty(matchingNode.NodeId))
                        {
                            // Extract core name from tag name
                            string tagCoreName = matchingTag.tagName;
                            if (tagCoreName.Length > 1 && char.IsLetter(tagCoreName[0]) && char.IsUpper(tagCoreName[1]))
                            {
                                tagCoreName = tagCoreName.Substring(1);
                            }
                            
                            matchingNode = _allNodes.FirstOrDefault(n =>
                            {
                                // Try matching DisplayName core name
                                string nodeCoreName = n.DisplayName;
                                if (nodeCoreName.Length > 1 && char.IsLetter(nodeCoreName[0]) && char.IsUpper(nodeCoreName[1]))
                                {
                                    nodeCoreName = nodeCoreName.Substring(1);
                                }
                                
                                return nodeCoreName.Equals(tagCoreName, StringComparison.OrdinalIgnoreCase) ||
                                       n.DisplayName.EndsWith(tagCoreName, StringComparison.OrdinalIgnoreCase) ||
                                       tagCoreName.EndsWith(nodeCoreName, StringComparison.OrdinalIgnoreCase);
                            });
                            
                            if (!string.IsNullOrEmpty(matchingNode.NodeId))
                            {
                                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching browsed node by core name - Tag: '{matchingTag.tagName}' (core: '{tagCoreName}'), DisplayName: '{matchingNode.DisplayName}', NodeId: '{matchingNode.NodeId}'");
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(matchingNode.NodeId))
                        {
                            try
                            {
                                var nodeId = new NodeId(matchingNode.NodeId);
                                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching browsed node '{matchingNode.DisplayName}' with NodeId '{matchingNode.NodeId}'");
                                return nodeId;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching node but failed to parse NodeId '{matchingNode.NodeId}': {ex.Message}");
                            }
                        }
                    }
                    
                    try
                    {
                        var nodeId = new NodeId(nodeIdStr);
                        return nodeId;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to parse tag address '{nodeIdStr}' as NodeId: {ex.Message}");
                    }
                    
                    // Try as string NodeId in namespace 0
                    try
                    {
                        var nodeId = new NodeId(nodeIdStr, 0);
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Created NodeId from tag address in namespace 0: {nodeId}");
                        return nodeId;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to create NodeId from tag address in namespace 0: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Error checking TagProvider: {ex.Message}");
            }
        }

        // Strategy 4: Search browsed nodes by DisplayName or NodeId string
        lock (_nodesLock)
        {
            // Try exact match first
            var matchingNode = _allNodes.FirstOrDefault(n => 
                n.DisplayName.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
                n.NodeId.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            
            // If no exact match, try partial matches
            if (string.IsNullOrEmpty(matchingNode.NodeId))
            {
                matchingNode = _allNodes.FirstOrDefault(n => 
                    n.DisplayName.Contains(identifier, StringComparison.OrdinalIgnoreCase) ||
                    n.NodeId.Contains(identifier, StringComparison.OrdinalIgnoreCase) ||
                    n.NodeId.EndsWith(identifier, StringComparison.OrdinalIgnoreCase) ||
                    identifier.EndsWith(n.DisplayName, StringComparison.OrdinalIgnoreCase));
            }
            
            // If still no match, try matching by core name (ignoring common prefixes like 'm', 'i', etc.)
            if (string.IsNullOrEmpty(matchingNode.NodeId))
            {
                // Extract core name by removing common single-character prefixes
                string coreName = identifier;
                if (coreName.Length > 1 && char.IsLetter(coreName[0]) && char.IsUpper(coreName[1]))
                {
                    coreName = coreName.Substring(1); // Remove first character if it's a single-letter prefix
                }
                
                matchingNode = _allNodes.FirstOrDefault(n =>
                {
                    // Try matching DisplayName core name
                    string nodeCoreName = n.DisplayName;
                    if (nodeCoreName.Length > 1 && char.IsLetter(nodeCoreName[0]) && char.IsUpper(nodeCoreName[1]))
                    {
                        nodeCoreName = nodeCoreName.Substring(1);
                    }
                    
                    return nodeCoreName.Equals(coreName, StringComparison.OrdinalIgnoreCase) ||
                           n.DisplayName.EndsWith(coreName, StringComparison.OrdinalIgnoreCase) ||
                           coreName.EndsWith(nodeCoreName, StringComparison.OrdinalIgnoreCase);
                });
                
                if (!string.IsNullOrEmpty(matchingNode.NodeId))
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching browsed node by core name - Identifier: '{identifier}' (core: '{coreName}'), DisplayName: '{matchingNode.DisplayName}', NodeId: '{matchingNode.NodeId}'");
                }
            }
            
            if (!string.IsNullOrEmpty(matchingNode.NodeId))
            {
                try
                {
                    var nodeId = new NodeId(matchingNode.NodeId);
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching browsed node - DisplayName: '{matchingNode.DisplayName}', NodeId: '{matchingNode.NodeId}' -> {nodeId}");
                    return nodeId;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching node but failed to parse NodeId '{matchingNode.NodeId}': {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: No matching browsed node found for identifier '{identifier}'. Searched {_allNodes.Count} nodes.");
            }
        }

        System.Diagnostics.Trace.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Could not resolve '{identifier}' using any method");
        return null;
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

    /// <summary>Set callback for subscribed node value updates (nodeId, value, timestamp).</summary>
    public void SetSubscribedValueCallback(Action<string, object?, DateTime>? callback)
    {
        _onSubscribedValue = callback;
    }

    /// <summary>Subscribe to a node to receive value updates.</summary>
    public bool SubscribeNode(string nodeId, string? displayName = null)
    {
        if (_session == null || !_session.Connected)
        {
            return false;
        }

        var nodeIdStr = nodeId;
        if (_subscribedNodes.ContainsKey(nodeIdStr))
        {
            return true; // Already subscribed
        }

        try
        {
            var resolvedNodeId = TryResolveNodeId(nodeId);
            if (resolvedNodeId == null || resolvedNodeId.IsNullNodeId)
            {
                return false;
            }

            // Ensure we have a subscription
            if (_subscription == null)
            {
                _subscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingInterval = 1000,
                    PublishingEnabled = true
                };
                _session.AddSubscription(_subscription);
                _subscription.Create();
            }

            var item = new MonitoredItem(_subscription.DefaultItem)
            {
                StartNodeId = resolvedNodeId,
                AttributeId = Attributes.Value,
                SamplingInterval = 1000,
                DisplayName = displayName ?? nodeId
            };

            var capturedNodeId = nodeIdStr;
            item.Notification += (monitoredItem, e) =>
            {
                if (e.NotificationValue is MonitoredItemNotification notification && notification.Value?.Value != null)
                {
                    var value = notification.Value.Value;
                    var timestamp = notification.Value.SourceTimestamp != DateTime.MinValue 
                        ? notification.Value.SourceTimestamp 
                        : DateTime.UtcNow;
                    
                    _onSubscribedValue?.Invoke(capturedNodeId, value, timestamp);
                }
            };

            _subscription.AddItem(item);
            _monitoredItems.Add(item);
            _subscribedNodes[nodeIdStr] = item;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Unsubscribe from a node.</summary>
    public bool UnsubscribeNode(string nodeId)
    {
        if (!_subscribedNodes.TryGetValue(nodeId, out var item))
        {
            return false;
        }

        try
        {
            _subscription?.RemoveItem(item);
            _monitoredItems.Remove(item);
            _subscribedNodes.Remove(nodeId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Get list of currently subscribed node IDs.</summary>
    public List<string> GetSubscribedNodes()
    {
        return _subscribedNodes.Keys.ToList();
    }

    /// <summary>Check if a node is currently subscribed.</summary>
    public bool IsSubscribed(string nodeId)
    {
        return _subscribedNodes.ContainsKey(nodeId);
    }
}
