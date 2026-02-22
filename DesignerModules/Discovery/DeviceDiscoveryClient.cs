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
    private volatile UdpClient? _udpClient;
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

            // Unicast sweep: send discovery to each IP on local subnets (reliable when broadcast is blocked)
            _ = SendSubnetUnicastSweepAsync(requestBytes, timeoutSeconds, ct);

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

            // Wait for final responses, then grace period so late unicast responses (e.g. 192.168.137.186) are received
            await Task.Delay(500, ct).ConfigureAwait(false);
            await Task.Delay(2000, ct).ConfigureAwait(false); // grace period before closing socket
            StopScanning();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error sending discovery broadcast: {ex.Message}");
        }
    }

    /// <summary>
    /// Default /24 mask used when the OS does not report a subnet mask (e.g. some mobile hotspot adapters).
    /// </summary>
    private static readonly IPAddress DefaultMask24 = IPAddress.Parse("255.255.255.0");

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
                        // Use reported mask, or assume /24 when null (common on hotspot/virtual adapters)
                        var mask = unicast.IPv4Mask ?? DefaultMask24;
                        byte[] ipBytes = ip.GetAddressBytes();
                        byte[] maskBytes = mask.GetAddressBytes();
                        byte[] broadcastBytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                        var broadcastAddr = new IPAddress(broadcastBytes);
                        if (!broadcastAddresses.Contains(broadcastAddr))
                            broadcastAddresses.Add(broadcastAddr);
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

    /// <summary>
    /// Gets local IPv4 subnets (address + mask) for all up interfaces. Uses /24 when mask is null (e.g. hotspot adapters).
    /// </summary>
    private List<(IPAddress Address, IPAddress Mask)> GetLocalSubnets()
    {
        var subnets = new List<(IPAddress, IPAddress)>();
        try
        {
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in nics)
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;
                foreach (var unicast in ni.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    // Use reported mask, or assume /24 when null so we still sweep (e.g. 192.168.137.x)
                    var mask = unicast.IPv4Mask ?? DefaultMask24;
                    subnets.Add((unicast.Address, mask));
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Error getting local subnets: {ex.Message}");
        }
        return subnets;
    }

    /// <summary>
    /// Enumerates host IPs in a subnet (excludes network and broadcast). For /24 sweeps .1–.254; for larger subnets caps count.
    /// </summary>
    private static IEnumerable<IPAddress> GetHostAddressesInSubnetCapped(IPAddress address, IPAddress mask, int maxHosts = 254)
    {
        byte[] ip = address.GetAddressBytes();
        byte[] m = mask.GetAddressBytes();
        int hostOctetIndex = -1;
        for (int i = 3; i >= 0; i--)
        {
            if (m[i] != 255)
            {
                hostOctetIndex = i;
                break;
            }
        }
        if (hostOctetIndex < 0)
            yield break; // /32
        byte[] network = new byte[4];
        for (int i = 0; i < 4; i++)
            network[i] = (byte)(ip[i] & m[i]);
        int count = 0;
        if (hostOctetIndex == 3)
        {
            // Typical /24: 192.168.137.1 -> 192.168.137.254
            for (int d = 1; d <= 254 && count < maxHosts; d++, count++)
            {
                yield return new IPAddress(new byte[] { network[0], network[1], network[2], (byte)d });
            }
            yield break;
        }
        if (hostOctetIndex == 2)
        {
            for (int c = 0; c < 256 && count < maxHosts; c++)
            {
                for (int d = 1; d <= 254 && count < maxHosts; d++, count++)
                {
                    yield return new IPAddress(new byte[] { network[0], network[1], (byte)c, (byte)d });
                }
            }
            yield break;
        }
        // /8 or /16: only enumerate first 254 hosts to avoid long scan
        for (int d = 1; d <= 254 && count < maxHosts; d++, count++)
        {
            yield return new IPAddress(new byte[] { network[0], network[1], network[2], (byte)d });
        }
    }

    private async Task SendSubnetUnicastSweepAsync(byte[] requestBytes, int timeoutSeconds, CancellationToken ct)
    {
        const int concurrency = 32;
        var subnets = GetLocalSubnets();
        var semaphore = new SemaphoreSlim(concurrency);
        var tasks = new List<Task>();
        foreach (var (address, mask) in subnets)
        {
            foreach (var hostIp in GetHostAddressesInSubnetCapped(address, mask))
            {
                if (ct.IsCancellationRequested) break;
                await semaphore.WaitAsync(ct).ConfigureAwait(false);
                var target = hostIp;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var ep = new IPEndPoint(target, DiscoveryPort);
                        await _udpClient!.SendAsync(requestBytes, requestBytes.Length, ep).ConfigureAwait(false);
                    }
                    catch { /* ignore per-host errors */ }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct));
            }
        }
        try
        {
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(timeoutSeconds + 1), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (TimeoutException) { }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (_isScanning && _udpClient != null && !ct.IsCancellationRequested)
        {
            try
            {
                var client = _udpClient;
                if (client == null) break;
                var result = await client.ReceiveAsync(ct).ConfigureAwait(false);
                ProcessResponse(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionReset || ex.Message.Contains("forcibly closed", StringComparison.OrdinalIgnoreCase))
            {
                break; // Socket closed or connection reset - normal when scan stops, don't spam log
            }
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
            // Use the address we received the response from (reachable IP). Runtime often reports 127.0.0.1/127.0.1.1 in JSON.
            string ip = remoteEndPoint.Address.ToString();
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
