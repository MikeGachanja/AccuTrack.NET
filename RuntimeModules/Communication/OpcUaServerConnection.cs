using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.Configuration;

namespace Runtime.Modules.Communication;

/// <summary>
/// OPC UA server connection using UA-.NETStandard. Exposes nodes and handles client connections.
/// </summary>
public sealed class OpcUaServerConnection : IConnectionStub
{
    private readonly ConnectionStatus _status;
    private readonly JsonObject? _config;
    private ApplicationConfiguration? _appConfig;
    private StandardServer? _server;
    private ServerNodeManager? _nodeManager;
    private readonly Dictionary<string, object?> _nodeValues = new();
    private volatile bool _running;
    private Task? _serverTask;

    public string Name => _status.Name;
    public string EndpointUrl { get; }

    public OpcUaServerConnection(ConnectionStatus status, JsonObject? config)
    {
        _status = status;
        _status.Type = "OPC UA Server";
        _config = config;
        var cfg = config?["config"] as JsonObject ?? config;
        EndpointUrl = cfg?["endpointUrl"]?.GetValue<string>()
            ?? cfg?["EndpointUrl"]?.GetValue<string>()
            ?? cfg?["endpoint"]?.GetValue<string>()
            ?? config?["endpoint"]?.GetValue<string>()
            ?? cfg?["url"]?.GetValue<string>()
            ?? "opc.tcp://localhost:4840";
    }

    public void Start()
    {
        _running = true;
        _status.Running = true;
        _status.Connected = false;
        _status.Status = "Starting server...";
        _serverTask = Task.Run(() => StartServerAsync());
    }

    public void Stop()
    {
        _running = false;
        try
        {
            if (_server != null)
            {
                _server.StopAsync().AsTask().Wait(5000);
                _server.Dispose();
            }
            _server = null;
            _nodeManager = null;
        }
        catch { /* ignore */ }
        _status.Connected = false;
        _status.Running = false;
        _status.Status = "Stopped";
    }

    private async Task StartServerAsync()
    {
        try
        {
            _appConfig = await CreateApplicationConfigurationAsync().ConfigureAwait(false);
            
            _status.Status = "Initializing server...";
            
            // Create server instance - using a simple server implementation
            var testServer = new TestServer();
            testServer.SetNodeManagerReferenceSetter((nodeManager) => _nodeManager = nodeManager);
            _server = testServer;

            // Start server - this will call CreateMasterNodeManager which creates the node manager
            await _server.StartAsync(_appConfig).ConfigureAwait(false);
            
            _status.Connected = true;
            _status.Running = true;
            _status.Status = $"Server running on {EndpointUrl}";
            
            // Keep server running
            while (_running)
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
        catch (Opc.Ua.ServiceResultException sre)
        {
            _status.Connected = false;
            _status.Running = false;
            _status.Status = $"OPC UA Error: {sre.Message} (StatusCode: 0x{(uint)sre.StatusCode:X8})";
            if (sre.InnerException != null)
            {
                _status.Status += $" Inner: {sre.InnerException.Message}";
            }
        }
        catch (Exception ex)
        {
            _status.Connected = false;
            _status.Running = false;
            var errorMsg = $"Error: {ex.GetType().Name}: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMsg += $" Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
            }
            _status.Status = errorMsg;
        }
    }

    private async Task<ApplicationConfiguration> CreateApplicationConfigurationAsync()
    {
        // Parse port from endpoint URL
        var uri = new Uri(EndpointUrl);
        var port = uri.Port > 0 ? uri.Port : 4840;

        // Create certificate store directory if it doesn't exist
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var certStorePath = Path.Combine(appDataPath, "DarkStar", "OPC", "CertificateStores", "Server");

        Directory.CreateDirectory(Path.Combine(certStorePath, "Trusted"));
        Directory.CreateDirectory(Path.Combine(certStorePath, "Issuers"));
        Directory.CreateDirectory(Path.Combine(certStorePath, "Rejected"));
        Directory.CreateDirectory(Path.Combine(certStorePath, "Own"));

        var config = new ApplicationConfiguration
        {
            ApplicationName = "DarkStar OPC UA Server",
            ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:DarkStar:OPCServer",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = Path.Combine(certStorePath, "Own"),
                    SubjectName = "CN=DarkStar OPC UA Server"
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
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new StringCollection { EndpointUrl },
                SecurityPolicies = new ServerSecurityPolicyCollection
                {
                    new ServerSecurityPolicy
                    {
                        SecurityMode = MessageSecurityMode.None,
                        SecurityPolicyUri = "http://opcfoundation.org/UA/SecurityPolicy#None"
                    }
                },
                UserTokenPolicies = new UserTokenPolicyCollection
                {
                    new UserTokenPolicy(UserTokenType.Anonymous)
                },
                DiagnosticsEnabled = false,
                MaxSessionCount = 100,
                MaxSubscriptionCount = 1000,
                MaxMessageQueueSize = 100
            },
            DisableHiResClock = true
        };
        
            await config.ValidateAsync(ApplicationType.Server).ConfigureAwait(false);
            
            // The OPC UA stack will automatically create certificates if needed
            // No manual certificate creation required
            
        return config;
    }

    /// <summary>Add or update a node value in the server.</summary>
    public bool SetNodeValue(string nodeId, object? value)
    {
        if (_nodeManager == null) return false;
        try
        {
            _nodeValues[nodeId] = value;
            _nodeManager.UpdateNodeValue(nodeId, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Get a node value from the server.</summary>
    public bool GetNodeValue(string nodeId, out object? value)
    {
        value = null;
        if (_nodeManager == null) return false;
        return _nodeValues.TryGetValue(nodeId, out value);
    }

    /// <summary>Get all node IDs exposed by this server.</summary>
    public List<string> GetNodeIds()
    {
        return _nodeValues.Keys.ToList();
    }
}

/// <summary>Simple test server implementation.</summary>
internal class TestServer : StandardServer
{
    private ServerNodeManager? _nodeManager;
    private Action<ServerNodeManager>? _nodeManagerSetter;

    public void SetNodeManagerReferenceSetter(Action<ServerNodeManager> setter)
    {
        _nodeManagerSetter = setter;
    }

    public ServerNodeManager? GetNodeManager()
    {
        return _nodeManager;
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        // Create node manager here where we have access to IServerInternal
        _nodeManager = new ServerNodeManager(server, configuration);
        _nodeManagerSetter?.Invoke(_nodeManager);
        var nodeManagers = new List<INodeManager> { _nodeManager };
        return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
    }

    protected override ServerProperties LoadServerProperties()
    {
        var properties = new ServerProperties
        {
            ManufacturerName = "DarkStar",
            ProductName = "DarkStar OPC UA Server",
            ProductUri = "http://DarkStar/OPC/Server",
            SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
            BuildNumber = Utils.GetAssemblyBuildNumber(),
            BuildDate = Utils.GetAssemblyTimestamp()
        };
        return properties;
    }
}

/// <summary>Simple node manager for the OPC UA server.</summary>
internal class ServerNodeManager : CustomNodeManager2
{
    public ServerNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration, "http://DarkStar/OPC/Server")
    {
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (Lock)
        {
            LoadPredefinedNodes(SystemContext, externalReferences);
        }
    }

    protected override void AddPredefinedNode(ISystemContext context, NodeState node)
    {
        base.AddPredefinedNode(context, node);
    }

    public void UpdateNodeValue(string nodeIdStr, object? value)
    {
        try
        {
            var nodeId = new NodeId(nodeIdStr);
            lock (Lock)
            {
                if (PredefinedNodes.TryGetValue(nodeId, out var node))
                {
                    if (node is BaseVariableState variable)
                    {
                        variable.Value = value;
                        variable.Timestamp = DateTime.UtcNow;
                        variable.ClearChangeMasks(SystemContext, false);
                    }
                }
                else
                {
                    // Create new node if it doesn't exist
                    // Get the Objects folder as parent (should already exist in address space)
                    NodeState? parentNode = null;
                    if (PredefinedNodes.TryGetValue(ObjectIds.ObjectsFolder, out var existingParent))
                    {
                        parentNode = existingParent;
                    }

                    // Create variable node - pass null as parent and set it up manually
                    var newNode = new BaseDataVariableState(parentNode)
                    {
                        NodeId = nodeId,
                        BrowseName = new QualifiedName(nodeIdStr, NamespaceIndex),
                        DisplayName = new LocalizedText("en", nodeIdStr),
                        TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                        ReferenceTypeId = ReferenceTypeIds.Organizes,
                        Value = value,
                        DataType = value?.GetType() == typeof(int) ? DataTypeIds.Int32 :
                                   value?.GetType() == typeof(double) ? DataTypeIds.Double :
                                   value?.GetType() == typeof(bool) ? DataTypeIds.Boolean :
                                   DataTypeIds.String,
                        AccessLevel = AccessLevels.CurrentReadOrWrite,
                        UserAccessLevel = AccessLevels.CurrentReadOrWrite
                    };
                    
                    // Add reference to parent if parent exists
                    if (parentNode != null)
                    {
                        parentNode.AddReference(ReferenceTypeIds.Organizes, false, newNode.NodeId);
                        newNode.AddReference(ReferenceTypeIds.Organizes, true, parentNode.NodeId);
                    }
                    
                    AddPredefinedNode(SystemContext, newNode);
                }
            }
        }
        catch { }
    }
}
