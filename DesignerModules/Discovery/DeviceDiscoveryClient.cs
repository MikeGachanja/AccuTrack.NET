using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Designer.Modules.Discovery;

/// <summary>
/// UDP client that discovers Runtime devices on the network by broadcasting discovery requests.
/// </summary>
public class DeviceDiscoveryClient
{
    private const int DiscoveryPort = 8889;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private volatile bool _isScanning;
    private readonly Dictionary<string, DiscoveredDevice> _discoveredDevices = new();
    private readonly object _lock = new();

    public event EventHandler<DiscoveredDevice>? DeviceFound;
    public event EventHandler<DiscoveredDevice>? DeviceUpdated;
    public event EventHandler<string>? DeviceLost;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsScanning => _isScanning;
    public IReadOnlyDictionary<string, DiscoveredDevice> DiscoveredDevices
    {
        get
        {
            lock (_lock)
            {
                return new Dictionary<string, DiscoveredDevice>(_discoveredDevices);
            }
        }
    }

    /// <summary>
    /// Starts scanning for devices on the network.
    /// </summary>
    public bool StartScanning(int timeoutSeconds = 5)
    {
        if (_isScanning)
            return true;

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            _udpClient.EnableBroadcast = true;

            _cancellationTokenSource = new CancellationTokenSource();
            _isScanning = true;

            // Start listening for responses
            _ = ReceiveLoopAsync(_cancellationTokenSource.Token);

            // Send discovery broadcast on all network interfaces
            _ = SendDiscoveryBroadcastAsync(timeoutSeconds, _cancellationTokenSource.Token);

            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Failed to start scanning: {ex.Message}");
            _isScanning = false;
            return false;
        }
    }

    /// <summary>
    /// Probes a specific IP address directly (useful for devices on different subnets).
    /// </summary>
    public async Task<bool> ProbeDeviceAsync(string ipAddress, int port = DiscoveryPort, int timeoutMs = 2000)
    {
        if (_udpClient == null || !_isScanning)
        {
            // Create a temporary UDP client for probing
            using var probeClient = new UdpClient();
            probeClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            probeClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            probeClient.EnableBroadcast = true;

            return await ProbeDeviceWithClientAsync(probeClient, ipAddress, port, timeoutMs).ConfigureAwait(false);
        }
        else
        {
            return await ProbeDeviceWithClientAsync(_udpClient, ipAddress, port, timeoutMs).ConfigureAwait(false);
        }
    }

    private async Task<bool> ProbeDeviceWithClientAsync(UdpClient client, string ipAddress, int port, int timeoutMs)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress? targetIp))
            {
                ErrorOccurred?.Invoke(this, $"Invalid IP address: {ipAddress}");
                return false;
            }

            var discoveryRequest = new Dictionary<string, object>
            {
                ["type"] = "discovery",
                ["source"] = "designer"
            };
            string json = JsonSerializer.Serialize(discoveryRequest);
            byte[] requestBytes = Encoding.UTF8.GetBytes(json);

            var targetEndPoint = new IPEndPoint(targetIp, port);
            await client.SendAsync(requestBytes, requestBytes.Length, targetEndPoint).ConfigureAwait(false);

            // Wait for response with timeout
            var cts = new CancellationTokenSource(timeoutMs);
            try
            {
                var result = await client.ReceiveAsync(cts.Token).ConfigureAwait(false);
                ProcessResponse(result.Buffer, result.RemoteEndPoint);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false; // Timeout
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error probing device {ipAddress}:{port}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stops scanning for devices.
    /// </summary>
    public void StopScanning()
    {
        if (!_isScanning)
            return;

        _isScanning = false;
        _cancellationTokenSource?.Cancel();
        
        try
        {
            _udpClient?.Close();
        }
        catch { }
        
        _udpClient = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    /// <summary>
    /// Clears all discovered devices.
    /// </summary>
    public void ClearDevices()
    {
        lock (_lock)
        {
            _discoveredDevices.Clear();
        }
    }

    private async Task SendDiscoveryBroadcastAsync(int timeoutSeconds, CancellationToken ct)
    {
        var discoveryRequest = new Dictionary<string, object>
        {
            ["type"] = "discovery",
            ["source"] = "designer"
        };
        string json = JsonSerializer.Serialize(discoveryRequest);
        byte[] requestBytes = Encoding.UTF8.GetBytes(json);

        try
        {
            // Get all network interfaces and send broadcasts on each subnet
            var broadcastAddresses = GetBroadcastAddresses();
            
            // Also send to localhost
            var localhostEndPoint = new IPEndPoint(IPAddress.Loopback, DiscoveryPort);
            
            // Send initial broadcasts
            foreach (var broadcastAddr in broadcastAddresses)
            {
                try
                {
                    var broadcastEndPoint = new IPEndPoint(broadcastAddr, DiscoveryPort);
                    await _udpClient!.SendAsync(requestBytes, requestBytes.Length, broadcastEndPoint).ConfigureAwait(false);
                }
                catch { /* Ignore errors on specific interfaces */ }
            }
            await _udpClient!.SendAsync(requestBytes, requestBytes.Length, localhostEndPoint).ConfigureAwait(false);

            // Send periodic broadcasts during scan period
            int intervalMs = 1000; // Send every second
            int elapsed = 0;
            while (elapsed < timeoutSeconds * 1000 && !ct.IsCancellationRequested)
            {
                await Task.Delay(intervalMs, ct).ConfigureAwait(false);
                elapsed += intervalMs;
                
                if (!ct.IsCancellationRequested)
                {
                    foreach (var broadcastAddr in broadcastAddresses)
                    {
                        try
                        {
                            var broadcastEndPoint = new IPEndPoint(broadcastAddr, DiscoveryPort);
                            await _udpClient.SendAsync(requestBytes, requestBytes.Length, broadcastEndPoint).ConfigureAwait(false);
                        }
                        catch { /* Ignore errors on specific interfaces */ }
                    }
                    await _udpClient.SendAsync(requestBytes, requestBytes.Length, localhostEndPoint).ConfigureAwait(false);
                }
            }

            // Wait a bit more for final responses
            await Task.Delay(500, ct).ConfigureAwait(false);
            StopScanning();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error sending discovery broadcast: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets broadcast addresses for all network interfaces.
    /// </summary>
    private List<IPAddress> GetBroadcastAddresses()
    {
        var broadcastAddresses = new List<IPAddress>();
        
        try
        {
            // Add standard broadcast
            broadcastAddresses.Add(IPAddress.Broadcast);
            
            // Get broadcast addresses for each network interface
            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in networkInterfaces)
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;
                    
                var ipProps = ni.GetIPProperties();
                foreach (var unicast in ipProps.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                        
                    try
                    {
                        var ip = unicast.Address;
                        var mask = unicast.IPv4Mask;
                        
                        if (mask != null)
                        {
                            // Calculate broadcast address: IP | ~Mask
                            byte[] ipBytes = ip.GetAddressBytes();
                            byte[] maskBytes = mask.GetAddressBytes();
                            byte[] broadcastBytes = new byte[4];
                            
                            for (int i = 0; i < 4; i++)
                            {
                                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                            }
                            
                            var broadcastAddr = new IPAddress(broadcastBytes);
                            if (!broadcastAddresses.Contains(broadcastAddr))
                            {
                                broadcastAddresses.Add(broadcastAddr);
                            }
                        }
                    }
                    catch { /* Skip invalid addresses */ }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error getting broadcast addresses: {ex.Message}");
        }
        
        return broadcastAddresses;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (_isScanning && _udpClient != null && !ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct).ConfigureAwait(false);
                ProcessResponse(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, $"Error receiving discovery response: {ex.Message}");
            }
        }
    }

    private void ProcessResponse(byte[] data, IPEndPoint remoteEndPoint)
    {
        try
        {
            string json = Encoding.UTF8.GetString(data);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string type = root.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
            if (type != "response")
                return;

            string deviceName = root.TryGetProperty("deviceName", out var dn) ? dn.GetString() ?? "" : "";
            string ip = root.TryGetProperty("ip", out var ipProp) ? ipProp.GetString() ?? "" : remoteEndPoint.Address.ToString();
            int port = root.TryGetProperty("port", out var p) ? p.GetInt32() : 8888;
            string version = root.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
            string deviceType = root.TryGetProperty("deviceType", out var dt) ? dt.GetString() ?? "" : "";

            if (string.IsNullOrEmpty(deviceName))
                deviceName = $"Device-{ip}";

            var device = new DiscoveredDevice
            {
                DeviceName = deviceName,
                IpAddress = ip,
                Port = port,
                Version = version,
                DeviceType = deviceType,
                LastSeen = DateTime.Now
            };

            string deviceKey = $"{ip}:{port}";
            bool isNew = false;

            lock (_lock)
            {
                if (!_discoveredDevices.ContainsKey(deviceKey))
                {
                    _discoveredDevices[deviceKey] = device;
                    isNew = true;
                }
                else
                {
                    _discoveredDevices[deviceKey].LastSeen = DateTime.Now;
                    _discoveredDevices[deviceKey].DeviceName = deviceName;
                    _discoveredDevices[deviceKey].Version = version;
                    _discoveredDevices[deviceKey].DeviceType = deviceType;
                }
            }

            if (isNew)
                DeviceFound?.Invoke(this, device);
            else
                DeviceUpdated?.Invoke(this, device);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error processing discovery response: {ex.Message}");
        }
    }
}

/// <summary>
/// Represents a discovered Runtime device.
/// </summary>
public class DiscoveredDevice
{
    public string DeviceName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int Port { get; set; } = 8888;
    public string Version { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public DateTime LastSeen { get; set; } = DateTime.Now;

    public override string ToString()
    {
        return $"{DeviceName} ({IpAddress}:{Port})";
    }
}
