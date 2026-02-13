using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using Runtime.Modules.Communication;

namespace Communication.Tests;

public partial class MainForm : Form
{
    private CommunicationModule? _commModule;
    private OpcUaClientConnection? _opcClient;
    private OpcUaServerConnection? _opcServer;
    private readonly List<NodeInfo> _nodes = new();
    private readonly List<NodeInfo> _filteredNodes = new();
    private readonly Dictionary<string, object?> _nodeValues = new();
    private readonly Dictionary<string, (object? value, DateTime timestamp)> _subscribedValues = new();

    public MainForm()
    {
        InitializeComponent();
        InitializeCommunication();
    }

    private void InitializeCommunication()
    {
        try
        {
            _commModule = new CommunicationModule();
            
            // Set up tag update callback to log all updates
            _commModule.SetTagUpdateCallback((tagName, value) =>
            {
                this.Invoke(() =>
                {
                    LogMessage($"Tag Update: {tagName} = {value}");
                    _nodeValues[tagName] = value;
                    UpdateNodeValuesList();
                });
            });

            // Subscribe to module events
            _commModule.StatusChanged += (sender, status) =>
            {
                this.Invoke(() =>
                {
                    LogMessage($"Communication Module Status: {status}");
                    UpdateStatus();
                });
            };

            _commModule.ErrorOccurred += (sender, error) =>
            {
                this.Invoke(() =>
                {
                    LogMessage($"ERROR: {error}", LogLevel.Error);
                });
            };

            _commModule.Initialized += (sender, args) =>
            {
                this.Invoke(() =>
                {
                    LogMessage("Communication module initialized", LogLevel.Info);
                });
            };

            LogMessage("Communication module initialized", LogLevel.Info);
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to initialize communication module: {ex.Message}", LogLevel.Error);
        }
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        try
        {
            if (_commModule == null)
            {
                LogMessage("Communication module not initialized", LogLevel.Error);
                return;
            }

            var endpointUrl = txtEndpointUrl.Text.Trim();
            if (string.IsNullOrEmpty(endpointUrl))
            {
                LogMessage("Please enter an endpoint URL", LogLevel.Warning);
                return;
            }

            // Determine mode (client or server)
            var mode = rdoClientMode.Checked ? "client" : "server";
            
            // Create OPC configuration
            var config = new JsonObject
            {
                ["name"] = $"OPC Test {mode.ToUpper()}",
                ["type"] = "OPC UA",
                ["mode"] = mode,
                ["config"] = new JsonObject
                {
                    ["endpointUrl"] = endpointUrl
                }
            };

            // Add tag mappings if provided
            if (!string.IsNullOrEmpty(txtTagMappings.Text))
            {
                var tagMappings = new JsonArray();
                var lines = txtTagMappings.Lines.Where(l => !string.IsNullOrWhiteSpace(l));
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var mapping = new JsonObject
                        {
                            ["tagName"] = parts[0].Trim(),
                            ["address"] = parts[1].Trim()
                        };
                        tagMappings.Add(mapping);
                    }
                }
                config["tagMappings"] = tagMappings;
            }

            var moduleConfig = new JsonObject
            {
                ["communication_modules"] = new JsonArray { config }
            };

            if (_commModule.Initialize(moduleConfig))
            {
                LogMessage("Configuration loaded successfully", LogLevel.Info);
                
                // Get the OPC client or server based on mode
                if (rdoClientMode.Checked)
                {
                    _opcClient = _commModule.GetOpcUaClient();
                    _opcServer = null;
                    if (_opcClient == null)
                    {
                        LogMessage("Failed to get OPC client from module", LogLevel.Error);
                        return;
                    }
                    
                    // Set up subscription callback for logging
                    _opcClient.SetSubscribedValueCallback((nodeId, value, timestamp) =>
                    {
                        this.Invoke(() =>
                        {
                            _subscribedValues[nodeId] = (value, timestamp);
                            LogMessage($"Subscribed Node Update: {nodeId} = {value} (Timestamp: {timestamp:HH:mm:ss.fff})", LogLevel.Debug);
                            UpdateSubscribedNodesList();
                        });
                    });
                }
                else
                {
                    _opcServer = _commModule.GetOpcUaServer();
                    _opcClient = null;
                    if (_opcServer == null)
                    {
                        LogMessage("Failed to get OPC server from module", LogLevel.Error);
                        return;
                    }
                }

                if (_commModule.Start())
                {
                    LogMessage($"Communication module started ({mode} mode)", LogLevel.Info);
                    btnConnect.Enabled = false;
                    btnDisconnect.Enabled = true;
                    
                    // Start monitoring connection status
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 500;
                    var errorLogged = false;
                    timer.Tick += (s, args) =>
                    {
                        UpdateStatus();
                        var status = _commModule.GetConnectionStatuses().Values.FirstOrDefault();
                        if (status != null)
                        {
                            // Log status changes
                            if (!string.IsNullOrEmpty(status.Status) && status.Status.Contains("Error"))
                            {
                                if (!errorLogged)
                                {
                                    LogMessage($"Server Status: {status.Status}", LogLevel.Error);
                                    errorLogged = true;
                                }
                            }
                            
                            if (status.Connected == true || (rdoServerMode.Checked && status.Running == true))
                            {
                                timer.Stop();
                                timer.Dispose();
                                if (rdoServerMode.Checked)
                                {
                                    UpdateServerNodesList();
                                    LogMessage($"Server started successfully on {txtEndpointUrl.Text}", LogLevel.Info);
                                }
                                else
                                {
                                    UpdateSubscribedNodesList();
                                }
                            }
                        }
                    };
                    timer.Start();
                }
                else
                {
                    LogMessage("Failed to start communication module", LogLevel.Error);
                }
            }
            else
            {
                LogMessage("Failed to initialize communication module", LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error connecting: {ex.Message}", LogLevel.Error);
            LogMessage($"Stack trace: {ex.StackTrace}", LogLevel.Debug);
        }
    }

    private void btnDisconnect_Click(object sender, EventArgs e)
    {
        try
        {
            _commModule?.Stop();
            _opcClient = null;
            _opcServer = null;
            _nodes.Clear();
            _filteredNodes.Clear();
            _nodeValues.Clear();
            _subscribedValues.Clear();
            UpdateNodeValuesList();
            UpdateBrowsedNodesList();
            UpdateSubscribedNodesList();
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            var mode = rdoClientMode.Checked ? "client" : "server";
            LogMessage($"Stopped OPC UA {mode}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            LogMessage($"Error stopping: {ex.Message}", LogLevel.Error);
        }
    }

    private void btnReadNode_Click(object sender, EventArgs e)
    {
        try
        {
            var nodeId = txtNodeId.Text.Trim();
            if (string.IsNullOrEmpty(nodeId))
            {
                LogMessage("Please enter a Node ID", LogLevel.Warning);
                return;
            }

            if (_opcClient == null)
            {
                LogMessage("Not connected to OPC server", LogLevel.Warning);
                return;
            }

            if (_opcClient.ReadTag(nodeId, out var value))
            {
                LogMessage($"Read Node {nodeId}: {value}", LogLevel.Info);
                _nodeValues[nodeId] = value;
                UpdateNodeValuesList();
            }
            else
            {
                LogMessage($"Failed to read node {nodeId}", LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error reading node: {ex.Message}", LogLevel.Error);
        }
    }

    private void btnWriteNode_Click(object sender, EventArgs e)
    {
        try
        {
            var nodeId = txtWriteNodeId.Text.Trim();
            var value = txtWriteValue.Text.Trim();
            
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(value))
            {
                LogMessage("Please enter both Node ID and Value", LogLevel.Warning);
                return;
            }

            if (_opcClient == null)
            {
                LogMessage("Not connected to OPC server", LogLevel.Warning);
                return;
            }

            // Try to parse value based on selected data type
            object? writeValue = value;
            if (cmbDataType.SelectedIndex > 0)
            {
                writeValue = ParseValue(value, cmbDataType.SelectedItem?.ToString() ?? "String");
            }

            if (_opcClient.WriteTag(nodeId, writeValue))
            {
                LogMessage($"Write Node {nodeId}: {writeValue}", LogLevel.Info);
            }
            else
            {
                LogMessage($"Failed to write node {nodeId}", LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error writing node: {ex.Message}", LogLevel.Error);
        }
    }

    private object? ParseValue(string value, string dataType)
    {
        return dataType switch
        {
            "Boolean" => bool.TryParse(value, out var b) ? b : value == "1" || value.ToLower() == "true",
            "Int32" => int.TryParse(value, out var i) ? i : null,
            "Double" => double.TryParse(value, out var d) ? d : null,
            "String" => value,
            _ => value
        };
    }

    private void UpdateStatus()
    {
        if (_commModule == null) return;

        var statuses = _commModule.GetConnectionStatuses();
        if (statuses.Count > 0)
        {
            var status = statuses.Values.First();
            lblStatus.Text = $"Status: {status.Status}";
            lblConnected.Text = $"Connected: {status.Connected}";
            lblRunning.Text = $"Running: {status.Running}";
        }
    }

    private void UpdateNodeValuesList()
    {
        lstNodeValues.Items.Clear();
        foreach (var kvp in _nodeValues)
        {
            lstNodeValues.Items.Add($"{kvp.Key} = {kvp.Value}");
        }
    }

    private void LogMessage(string message, LogLevel level = LogLevel.Info)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var levelStr = level switch
        {
            LogLevel.Error => "ERROR",
            LogLevel.Warning => "WARN",
            LogLevel.Debug => "DEBUG",
            _ => "INFO"
        };

        var logEntry = $"[{timestamp}] [{levelStr}] {message}";
        
        txtConsole.AppendText(logEntry + Environment.NewLine);
        txtConsole.SelectionStart = txtConsole.Text.Length;
        txtConsole.ScrollToCaret();
    }

    private void btnClearConsole_Click(object sender, EventArgs e)
    {
        txtConsole.Clear();
    }

    private void btnBrowseNodes_Click(object sender, EventArgs e)
    {
        try
        {
            if (_opcClient == null)
            {
                LogMessage("Not connected as OPC client. Browsing requires client mode.", LogLevel.Warning);
                return;
            }

            LogMessage("Browsing nodes from server...", LogLevel.Info);
            btnBrowseNodes.Enabled = false;
            btnBrowseNodes.Text = "Browsing...";

            // Run browsing in background
            Task.Run(() =>
            {
                try
                {
                    var startNodeId = txtBrowseStartNode.Text.Trim();
                    var nodes = string.IsNullOrEmpty(startNodeId) 
                        ? _opcClient.BrowseAllNodes() 
                        : _opcClient.BrowseAllNodes(startNodeId);

                    this.Invoke(() =>
                    {
                        _nodes.Clear();
                        foreach (var node in nodes)
                        {
                            _nodes.Add(new NodeInfo
                            {
                                NodeId = node.NodeId,
                                DisplayName = node.DisplayName,
                                Timestamp = DateTime.Now
                            });
                        }

                        LogMessage($"Found {_nodes.Count} nodes", LogLevel.Info);
                        ApplyNodeFilter();
                        btnBrowseNodes.Enabled = true;
                        btnBrowseNodes.Text = "Browse All Nodes";
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke(() =>
                    {
                        LogMessage($"Error browsing nodes: {ex.Message}", LogLevel.Error);
                        btnBrowseNodes.Enabled = true;
                        btnBrowseNodes.Text = "Browse All Nodes";
                    });
                }
            });
        }
        catch (Exception ex)
        {
            LogMessage($"Error browsing nodes: {ex.Message}", LogLevel.Error);
            btnBrowseNodes.Enabled = true;
            btnBrowseNodes.Text = "Browse All Nodes";
        }
    }

    private void txtNodeFilter_TextChanged(object sender, EventArgs e)
    {
        ApplyNodeFilter();
    }

    private void ApplyNodeFilter()
    {
        var filter = txtNodeFilter.Text.Trim().ToLower();
        _filteredNodes.Clear();

        if (string.IsNullOrEmpty(filter))
        {
            _filteredNodes.AddRange(_nodes);
        }
        else
        {
            _filteredNodes.AddRange(_nodes.Where(n =>
                n.NodeId.ToLower().Contains(filter) ||
                n.DisplayName.ToLower().Contains(filter)));
        }

        UpdateBrowsedNodesList();
    }

    private void UpdateBrowsedNodesList()
    {
        lstBrowsedNodes.Items.Clear();
        foreach (var node in _filteredNodes)
        {
            lstBrowsedNodes.Items.Add($"{node.DisplayName} ({node.NodeId})");
        }
        lblNodeCount.Text = $"Nodes: {_filteredNodes.Count} / {_nodes.Count}";
    }

    private void lstBrowsedNodes_DoubleClick(object sender, EventArgs e)
    {
        if (lstBrowsedNodes.SelectedIndex >= 0 && lstBrowsedNodes.SelectedIndex < _filteredNodes.Count)
        {
            var node = _filteredNodes[lstBrowsedNodes.SelectedIndex];
            txtNodeId.Text = node.NodeId;
            txtWriteNodeId.Text = node.NodeId;
        }
    }

    private void rdoClientMode_CheckedChanged(object sender, EventArgs e)
    {
        UpdateModeUI();
    }

    private void rdoServerMode_CheckedChanged(object sender, EventArgs e)
    {
        UpdateModeUI();
    }

    private void UpdateModeUI()
    {
        var isClientMode = rdoClientMode.Checked;
        grpNodeOperations.Visible = isClientMode;
        grpNodeValues.Visible = isClientMode;
        grpNodeBrowser.Visible = isClientMode;
        grpSubscriptions.Visible = isClientMode;
        grpServerNodes.Visible = !isClientMode;
        grpTagMappings.Visible = isClientMode;
    }

    private void btnAddServerNode_Click(object sender, EventArgs e)
    {
        try
        {
            if (_opcServer == null)
            {
                LogMessage("Server not running", LogLevel.Warning);
                return;
            }

            var nodeId = txtServerNodeId.Text.Trim();
            var valueStr = txtServerNodeValue.Text.Trim();

            if (string.IsNullOrEmpty(nodeId))
            {
                LogMessage("Please enter a Node ID", LogLevel.Warning);
                return;
            }

            // Try to parse value
            object? value = valueStr;
            if (int.TryParse(valueStr, out var intVal))
                value = intVal;
            else if (double.TryParse(valueStr, out var doubleVal))
                value = doubleVal;
            else if (bool.TryParse(valueStr, out var boolVal))
                value = boolVal;

            if (_opcServer.SetNodeValue(nodeId, value))
            {
                LogMessage($"Server node {nodeId} = {value}", LogLevel.Info);
                UpdateServerNodesList();
            }
            else
            {
                LogMessage($"Failed to set server node {nodeId}", LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error adding server node: {ex.Message}", LogLevel.Error);
        }
    }

    private void UpdateServerNodesList()
    {
        if (_opcServer == null) return;

        lstServerNodes.Items.Clear();
        var nodeIds = _opcServer.GetNodeIds();
        foreach (var nodeId in nodeIds)
        {
            if (_opcServer.GetNodeValue(nodeId, out var value))
            {
                lstServerNodes.Items.Add($"{nodeId} = {value}");
            }
        }
    }

    private void btnSubscribeNode_Click(object sender, EventArgs e)
    {
        if (_opcClient == null)
        {
            LogMessage("OPC client not available. Please connect first.", LogLevel.Warning);
            return;
        }

        var nodeId = txtSubscribeNodeId.Text.Trim();
        if (string.IsNullOrEmpty(nodeId))
        {
            LogMessage("Please enter a node ID to subscribe", LogLevel.Warning);
            return;
        }

        try
        {
            if (_opcClient.IsSubscribed(nodeId))
            {
                LogMessage($"Node '{nodeId}' is already subscribed", LogLevel.Info);
                return;
            }

            if (_opcClient.SubscribeNode(nodeId))
            {
                LogMessage($"Successfully subscribed to node: {nodeId}", LogLevel.Info);
                txtSubscribeNodeId.Clear();
                UpdateSubscribedNodesList();
            }
            else
            {
                LogMessage($"Failed to subscribe to node: {nodeId}", LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error subscribing to node: {ex.Message}", LogLevel.Error);
        }
    }

    private void btnUnsubscribeNode_Click(object sender, EventArgs e)
    {
        if (_opcClient == null)
        {
            LogMessage("OPC client not available. Please connect first.", LogLevel.Warning);
            return;
        }

        var nodeId = txtSubscribeNodeId.Text.Trim();
        if (string.IsNullOrEmpty(nodeId))
        {
            // Try to get from selected item in list
            if (lstSubscribedNodes.SelectedItem != null)
            {
                var selected = lstSubscribedNodes.SelectedItem.ToString();
                if (selected != null && selected.Contains(" = "))
                {
                    nodeId = selected.Split(new[] { " = " }, StringSplitOptions.None)[0];
                }
            }
        }

        if (string.IsNullOrEmpty(nodeId))
        {
            LogMessage("Please enter or select a node ID to unsubscribe", LogLevel.Warning);
            return;
        }

        try
        {
            if (_opcClient.UnsubscribeNode(nodeId))
            {
                LogMessage($"Successfully unsubscribed from node: {nodeId}", LogLevel.Info);
                _subscribedValues.Remove(nodeId);
                txtSubscribeNodeId.Clear();
                UpdateSubscribedNodesList();
            }
            else
            {
                LogMessage($"Failed to unsubscribe from node: {nodeId} (may not be subscribed)", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error unsubscribing from node: {ex.Message}", LogLevel.Error);
        }
    }

    private void UpdateSubscribedNodesList()
    {
        lstSubscribedNodes.Items.Clear();
        
        if (_opcClient == null)
        {
            lblSubscribedCount.Text = "Subscribed: 0";
            return;
        }

        var subscribedNodes = _opcClient.GetSubscribedNodes();
        lblSubscribedCount.Text = $"Subscribed: {subscribedNodes.Count}";

        foreach (var nodeId in subscribedNodes)
        {
            if (_subscribedValues.TryGetValue(nodeId, out var valueInfo))
            {
                var timestamp = valueInfo.timestamp.ToString("HH:mm:ss.fff");
                lstSubscribedNodes.Items.Add($"{nodeId} = {valueInfo.value} [{timestamp}]");
            }
            else
            {
                lstSubscribedNodes.Items.Add($"{nodeId} = (no value yet)");
            }
        }
    }

    private void lstSubscribedNodes_DoubleClick(object sender, EventArgs e)
    {
        if (lstSubscribedNodes.SelectedItem != null)
        {
            var selected = lstSubscribedNodes.SelectedItem.ToString();
            if (selected != null && selected.Contains(" = "))
            {
                var nodeId = selected.Split(new[] { " = " }, StringSplitOptions.None)[0];
                txtSubscribeNodeId.Text = nodeId;
            }
        }
    }
}

public enum LogLevel
{
    Info,
    Warning,
    Error,
    Debug
}

public class NodeInfo
{
    public string NodeId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public object? Value { get; set; }
    public string DataType { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
