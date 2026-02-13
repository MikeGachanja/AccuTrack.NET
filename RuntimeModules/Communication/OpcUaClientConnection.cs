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
        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Start: Starting OPC UA client connection '{Name}' to endpoint '{EndpointUrl}'");
        _running = true;
        _status.Running = true;
        _status.Connected = false;
        _status.Status = "Connecting...";
        Task.Run(() => ConnectAndSubscribeAsync());
    }

    public void Stop()
    {
        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Stop: Stopping OPC UA client connection '{Name}'");
        _running = false;
        try
        {
            if (_subscription != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Stop: Deleting subscription (ID: {_subscription.Id})");
                _subscription.Delete(true);
                _subscription = null;
            }
            
            if (_session != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Stop: Closing session (ID: {_session.SessionId})");
                _session.Close();
                _session.Dispose();
                _session = null;
            }
            
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Stop: Successfully stopped OPC UA client connection");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Stop: Error during stop: {ex.GetType().Name} - {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Discovering endpoints from '{EndpointUrl}'");
            var endpointConfiguration = EndpointConfiguration.Create(_appConfig);
            
            // Use DiscoveryClient to discover endpoints
            EndpointDescriptionCollection? endpoints = null;
            using (var discoveryClient = DiscoveryClient.Create(new Uri(EndpointUrl), endpointConfiguration))
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Calling GetEndpointsAsync...");
                endpoints = await discoveryClient.GetEndpointsAsync(null).ConfigureAwait(false);
            }
            
            if (endpoints == null || endpoints.Count == 0)
            {
                _status.Status = "Error: Could not discover endpoints from server";
                _status.Connected = false;
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: ERROR - No endpoints discovered from server");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Discovered {endpoints.Count} endpoint(s)");
            for (int i = 0; i < endpoints.Count; i++)
            {
                var ep = endpoints[i];
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Endpoint {i + 1}: SecurityMode={ep.SecurityMode}, SecurityPolicyUri={ep.SecurityPolicyUri}, EndpointUrl={ep.EndpointUrl}");
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
                            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Found anonymous endpoint: {endpoint.EndpointUrl}");
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
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Using endpoint with None security mode: {anonymousEndpoint.EndpointUrl}");
                }
                else
                {
                    anonymousEndpoint = endpoints[0];
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Using first available endpoint: {anonymousEndpoint.EndpointUrl}");
                }
            }

            var configuredEndpoint = new ConfiguredEndpoint(null, anonymousEndpoint, endpointConfiguration);

            _status.Status = "Connecting to server...";
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Creating session to endpoint '{anonymousEndpoint.EndpointUrl}'...");
            _session = await Session.Create(
                _appConfig,
                configuredEndpoint,
                false,
                _appConfig.ApplicationName,
                60000,
                new UserIdentity(), // Anonymous identity (empty constructor)
                null).ConfigureAwait(false);
            
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Session created successfully");

            _status.Connected = true;
            _status.Status = "Connected";
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully connected to OPC UA server at {EndpointUrl}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Session ID: {_session.SessionId}, Session Name: {_session.SessionName}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Endpoint URL: {_session.Endpoint?.EndpointUrl ?? EndpointUrl}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Server Description: {_session.Endpoint?.Server?.ApplicationName?.Text ?? "Unknown"}");

            // Fetch all nodes from the server
            _status.Status = "Browsing nodes...";
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Starting to browse all nodes from server...");
            try
            {
                var allNodes = BrowseAllNodes();
                lock (_nodesLock)
                {
                    _allNodes.Clear();
                    _allNodes.AddRange(allNodes);
                }
                _status.Status = $"Connected ({_allNodes.Count} nodes discovered)";
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully browsed {_allNodes.Count} nodes from server");
            }
            catch (Exception ex)
            {
                _status.Status = $"Connected (node browsing failed: {ex.Message})";
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Node browsing failed: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: StackTrace: {ex.StackTrace}");
            }

            // Subscribe to tag-mapped nodes (only those that exist in Tags)
            var tagMappings = GetTagMappings();
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Found {tagMappings.Count} tag mappings to subscribe to");
            
            if (tagMappings.Count > 0 && _onTagValue != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Creating subscription with PublishingInterval=1000ms");
                _subscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingInterval = 1000,
                    PublishingEnabled = true
                };
                
                int subscribedCount = 0;
                int failedCount = 0;
                
                foreach (var (tagName, nodeIdStr) in tagMappings)
                {
                    if (string.IsNullOrWhiteSpace(nodeIdStr))
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Skipping tag '{tagName}' - empty nodeId");
                        continue;
                    }
                    
                    // Verify the node exists in browsed nodes (optional check)
                    bool nodeExists = false;
                    lock (_nodesLock)
                    {
                        nodeExists = _allNodes.Any(n => n.NodeId == nodeIdStr);
                    }
                    
                    if (!nodeExists)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Node '{nodeIdStr}' not found in browsed nodes, but will attempt subscription anyway");
                    }
                    
                    // Subscribe even if node not found in browse (might be a valid node)
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Attempting to subscribe to tag '{tagName}' with NodeId '{nodeIdStr}'");
                        
                        // Try to resolve NodeId
                        var nodeId = TryResolveNodeId(nodeIdStr);
                        if (nodeId == null || nodeId.IsNullNodeId)
                        {
                            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Failed to resolve NodeId '{nodeIdStr}' for tag '{tagName}'");
                            failedCount++;
                            continue;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Resolved NodeId '{nodeIdStr}' to {nodeId}");
                        
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
                            {
                                var value = n.Value.Value;
                                var statusCode = n.Value.StatusCode;
                                var sourceTimestamp = n.Value.SourceTimestamp;
                                
                                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Subscription Notification: Tag '{tagName}' changed to '{value}' (Type: {value.GetType().Name}, StatusCode: 0x{statusCode:X8}, Timestamp: {sourceTimestamp})");
                                
                                if (Opc.Ua.StatusCode.IsGood(statusCode))
                                {
                                    _onTagValue?.Invoke(tagName, value);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Subscription Notification: Tag '{tagName}' has bad status code 0x{statusCode:X8}, not invoking callback");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] Subscription Notification: Tag '{tagName}' received notification but value is null or invalid");
                            }
                        };
                        
                        _subscription.AddItem(item);
                        _monitoredItems.Add(item);
                        subscribedCount++;
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Successfully added subscription for tag '{tagName}'");
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Failed to subscribe to tag '{tagName}' with NodeId '{nodeIdStr}': {ex.GetType().Name} - {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: StackTrace: {ex.StackTrace}");
                    }
                }
                
                if (subscribedCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Adding subscription to session ({subscribedCount} items, {failedCount} failed)");
                    _session.AddSubscription(_subscription);
                    _subscription.Create();
                    
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Subscription created successfully. Subscription ID: {_subscription.Id}, PublishingInterval: {_subscription.PublishingInterval}ms");
                    _status.Status = $"Connected ({_allNodes.Count} nodes, {subscribedCount} tags subscribed)";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: No tags successfully subscribed ({failedCount} failed)");
                    _status.Status = $"Connected ({_allNodes.Count} nodes discovered, {failedCount} subscription failures)";
                }
            }
            else
            {
                if (tagMappings.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: No tag mappings found in configuration");
                }
                if (_onTagValue == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Tag update callback is null - subscriptions disabled");
                }
                _status.Status = $"Connected ({_allNodes.Count} nodes discovered)";
            }
            
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Connection setup complete. Connected: {_status.Connected}, Status: {_status.Status}");
        }
        catch (Exception ex)
        {
            _status.Connected = false;
            _status.Status = "Error: " + ex.Message;
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: Connection failed with exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ConnectAndSubscribeAsync: InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
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
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: Found {configArr.Count} tag mappings in config");
            foreach (var node in configArr)
            {
                if (node is not JsonObject obj) continue;
                var tagName = obj["tagName"]?.GetValue<string>() ?? "";
                var address = obj["address"]?.GetValue<string>() ?? obj["nodeId"]?.GetValue<string>() ?? "";
                if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(address))
                {
                    list.Add((tagName, address));
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: Added mapping from config - Tag: '{tagName}', Address: '{address}'");
                }
            }
        }
        
        // Also get tags from TagProvider function (tags loaded from JSON)
        if (_getTagsFunc != null)
        {
            try
            {
                var tags = _getTagsFunc();
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: TagProvider returned {tags.Count} tags");
                
                if (tags.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: WARNING - TagProvider returned 0 tags. Tags may not be loaded yet.");
                }
                
                foreach (var (tagName, address) in tags)
                {
                    // Skip if already in list (from config)
                    if (list.Any(m => m.tagName == tagName))
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: Tag '{tagName}' already mapped from config, skipping");
                        continue;
                    }
                    
                    // Use tag address if available, otherwise use tag name as NodeId
                    var nodeId = !string.IsNullOrEmpty(address) ? address : tagName;
                    list.Add((tagName, nodeId));
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: Added mapping from TagProvider - Tag: '{tagName}', NodeId: '{nodeId}' (Address: '{address}')");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: Error getting tags from TagProvider: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: StackTrace: {ex.StackTrace}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: TagProvider is null - cannot get tags from TagProvider");
        }
        
        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] GetTagMappings: Total mappings: {list.Count}");
        return list;
    }

    public bool ReadTag(string nodeIdOrTagName, out object? value)
    {
        value = null;
        if (_session == null || !_session.Connected)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag failed: Session is null or not connected. NodeId: {nodeIdOrTagName}");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(nodeIdOrTagName))
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag failed: NodeId is null or empty");
            return false;
        }
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag: Attempting to read NodeId='{nodeIdOrTagName}'");
            
            NodeId nodeId = TryResolveNodeId(nodeIdOrTagName);
            if (nodeId == null || nodeId.IsNullNodeId)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag: Could not resolve NodeId for '{nodeIdOrTagName}'. Returning false.");
                return false;
            }
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag: NodeId resolved: {nodeId}");
            
            var dataValue = _session.ReadValue(nodeId);
            if (dataValue != null && Opc.Ua.StatusCode.IsGood(dataValue.StatusCode))
            {
                value = dataValue.WrappedValue.Value;
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag: Successfully read value '{value}' (Type: {value?.GetType().Name}) from NodeId '{nodeIdOrTagName}'");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag: Read failed with StatusCode: {dataValue?.StatusCode:X8} for NodeId '{nodeIdOrTagName}'");
            }
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException ParamName: {ex.ParamName}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag ArgumentException InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag Exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag Exception StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] ReadTag Exception InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
        }
        return false;
    }

    public bool WriteTag(string nodeIdOrTagName, object? value)
    {
        if (_session == null || !_session.Connected)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag failed: Session is null or not connected. NodeId: {nodeIdOrTagName}, Value: {value}");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(nodeIdOrTagName))
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag failed: NodeId is null or empty. Value: {value}");
            return false;
        }
        
        try
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Attempting to write NodeId='{nodeIdOrTagName}', Value='{value}' (Type: {value?.GetType().Name ?? "null"})");
            
            NodeId nodeId = TryResolveNodeId(nodeIdOrTagName);
            if (nodeId == null || nodeId.IsNullNodeId)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Could not resolve NodeId for '{nodeIdOrTagName}'. Returning false.");
                return false;
            }
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: NodeId resolved: {nodeId}");
            
            Variant variant;
            if (value != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Creating Variant from value type {value.GetType().Name}");
                
                // Convert value to a type that OPC UA supports
                object? convertedValue = value;
                
                // Handle common type conversions - preserve boolean types explicitly
                if (value is bool boolValue)
                {
                    convertedValue = boolValue;
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Preserving boolean value: {boolValue}");
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
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Converted long to int: {longValue} -> {convertedValue}");
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
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Converted decimal to double: {decimalValue} -> {convertedValue}");
                }
                else if (value is DateTime dateTimeValue)
                {
                    convertedValue = dateTimeValue;
                }
                else
                {
                    // Try to convert to string as fallback
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Unsupported type {value.GetType().Name}, converting to string");
                    convertedValue = value.ToString();
                }
                
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Creating Variant with converted value type {convertedValue?.GetType().Name}");
                variant = new Variant(convertedValue);
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Variant created successfully. Type: {variant.TypeInfo?.BuiltInType}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Creating default Variant for null value");
                variant = default;
            }
            
            var dataValue = new DataValue(variant);
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: DataValue created successfully");
            
            var writeValue = new WriteValue 
            { 
                NodeId = nodeId, 
                AttributeId = Attributes.Value, 
                Value = dataValue 
            };
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: WriteValue created successfully");
            
            var writeValues = new WriteValueCollection { writeValue };
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Calling session.Write...");
            
            _session.Write(null, writeValues, out var results, out var diagnosticInfos);
            
            if (results != null && results.Count > 0)
            {
                var statusCode = results[0];
                if (Opc.Ua.StatusCode.IsGood(statusCode))
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Successfully wrote value '{value}' to NodeId '{nodeIdOrTagName}'");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Write failed with StatusCode: 0x{statusCode:X8} for NodeId '{nodeIdOrTagName}'");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag: Write returned no results");
            }
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException ParamName: {ex.ParamName}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException StackTrace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException Details - NodeId: '{nodeIdOrTagName}', Value: '{value}', ValueType: {value?.GetType().Name ?? "null"}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag ArgumentException InnerException StackTrace: {ex.InnerException.StackTrace}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag Exception: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag Exception StackTrace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag Exception Details - NodeId: '{nodeIdOrTagName}', Value: '{value}', ValueType: {value?.GetType().Name ?? "null"}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag Exception InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] WriteTag Exception InnerException StackTrace: {ex.InnerException.StackTrace}");
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
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Successfully parsed as NodeId: {nodeId}");
            return nodeId;
        }
        catch (ArgumentException)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to parse '{identifier}' as NodeId, trying alternatives...");
        }

        // Strategy 2: Try as string NodeId in namespace 0 (default namespace)
        try
        {
            var nodeId = new NodeId(identifier, 0);
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Created string NodeId in namespace 0: {nodeId}");
            return nodeId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to create string NodeId in namespace 0: {ex.Message}");
        }

        // Strategy 3: Check if identifier matches a tag name or address from TagProvider
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
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching tag '{matchingTag.tagName}' with address '{matchingTag.address}', using NodeId: '{nodeIdStr}'");
                    
                    try
                    {
                        var nodeId = new NodeId(nodeIdStr);
                        return nodeId;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to parse tag address '{nodeIdStr}' as NodeId: {ex.Message}");
                    }
                    
                    // Try as string NodeId in namespace 0
                    try
                    {
                        var nodeId = new NodeId(nodeIdStr, 0);
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Created NodeId from tag address in namespace 0: {nodeId}");
                        return nodeId;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Failed to create NodeId from tag address in namespace 0: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Error checking TagProvider: {ex.Message}");
            }
        }

        // Strategy 4: Search browsed nodes by DisplayName or NodeId string
        lock (_nodesLock)
        {
            var matchingNode = _allNodes.FirstOrDefault(n => 
                n.DisplayName.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
                n.NodeId.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
                n.NodeId.EndsWith(identifier, StringComparison.OrdinalIgnoreCase));
            
            if (!string.IsNullOrEmpty(matchingNode.NodeId))
            {
                try
                {
                    var nodeId = new NodeId(matchingNode.NodeId);
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching node by DisplayName/NodeId: {matchingNode.DisplayName} -> {nodeId}");
                    return nodeId;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Found matching node but failed to parse NodeId '{matchingNode.NodeId}': {ex.Message}");
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"[OpcUaClientConnection] TryResolveNodeId: Could not resolve '{identifier}' using any method");
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
}
