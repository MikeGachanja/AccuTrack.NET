using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AccuTrack.Discovery;

/// <summary>
/// Discovers AccuTrack Runtime devices on the network via UDP broadcast.
/// Sends "DISCOVER" to port 9876; Runtime responds with "DeviceName|IP|port".
/// </summary>
public sealed class DeviceDiscovery : IDisposable
{
    public const int DiscoveryPort = 9876;
    /// <summary>Port used by Runtime for TCP project transfer (matches ProjectTransferClient and Qt Runtime).</summary>
    public const int TransferPort = 8888;
    private const int BroadcastIntervalMs = 2000;
    private const int DeviceTimeoutMs = 10000;

    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private Task? _broadcastTask;
    private Task? _timeoutTask;
    private readonly Dictionary<string, DeviceInfo> _devices = new();
    private readonly object _lock = new();

    public bool IsScanning { get; private set; }
    public IReadOnlyList<DeviceInfo> DiscoveredDevices
    {
        get { lock (_lock) return _devices.Values.ToList(); }
    }

    public event EventHandler<DeviceInfo>? DeviceFound;
    public event EventHandler<string>? DeviceLost;
    public event EventHandler<DeviceInfo>? DeviceUpdated;

    public void StartScanning()
    {
        if (IsScanning) return;
        try
        {
            _udp = new UdpClient();
            _udp.EnableBroadcast = true;
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            _receiveTask = Task.Run(() => ReceiveLoopAsync(ct));
            _broadcastTask = Task.Run(() => BroadcastLoopAsync(ct));
            _timeoutTask = Task.Run(() => TimeoutLoopAsync(ct));
            IsScanning = true;
        }
        catch (Exception)
        {
            _udp?.Close();
            _udp = null;
            throw;
        }
    }

    public void StopScanning()
    {
        if (!IsScanning) return;
        _cts?.Cancel();
        _udp?.Close();
        _udp = null;
        try
        {
            _receiveTask?.GetAwaiter().GetResult();
            _broadcastTask?.GetAwaiter().GetResult();
            _timeoutTask?.GetAwaiter().GetResult();
        }
        catch { /* ignore */ }
        lock (_lock) _devices.Clear();
        IsScanning = false;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        if (_udp == null) return;
        while (!ct.IsCancellationRequested && _udp != null)
        {
            try
            {
                var result = await _udp.ReceiveAsync(ct).ConfigureAwait(false);
                var msg = Encoding.UTF8.GetString(result.Buffer);
                if (string.IsNullOrWhiteSpace(msg)) continue;
                // Runtime sends "DeviceName|IP|port"
                var parts = msg.Split('|');
                if (parts.Length < 2) continue;
                var deviceName = parts[0].Trim();
                var ip = parts.Length >= 2 ? parts[1].Trim() : result.RemoteEndPoint?.Address?.ToString() ?? "";
                if (string.IsNullOrEmpty(ip) || ip == "127.0.0.1") ip = result.RemoteEndPoint?.Address?.ToString() ?? ip;
                var port = parts.Length >= 3 && int.TryParse(parts[2], out var p) ? p : DiscoveryPort;
                var device = new DeviceInfo(deviceName, ip, port, DateTime.UtcNow.Ticks);
                AddOrUpdateDevice(device);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception) { /* ignore */ }
        }
    }

    private void AddOrUpdateDevice(DeviceInfo device)
    {
        lock (_lock)
        {
            var isNew = !_devices.ContainsKey(device.Ip);
            _devices[device.Ip] = device;
            if (isNew)
                DeviceFound?.Invoke(this, device);
            else
                DeviceUpdated?.Invoke(this, device);
        }
    }

    private void RemoveDevice(string ip)
    {
        lock (_lock)
        {
            if (_devices.Remove(ip))
                DeviceLost?.Invoke(this, ip);
        }
    }

    private async Task BroadcastLoopAsync(CancellationToken ct)
    {
        var buffer = Encoding.UTF8.GetBytes("DISCOVER");
        while (!ct.IsCancellationRequested && _udp != null)
        {
            try
            {
                foreach (var broadcast in GetBroadcastAddresses())
                {
                    try
                    {
                        _udp.Send(buffer, buffer.Length, new IPEndPoint(broadcast, DiscoveryPort));
                    }
                    catch { /* ignore per-interface */ }
                }
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception) { /* ignore */ }
            await Task.Delay(BroadcastIntervalMs, ct).ConfigureAwait(false);
        }
    }

    private static List<IPAddress> GetBroadcastAddresses()
    {
        var list = new List<IPAddress>();
        try
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up ||
                    ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    continue;
                foreach (var uip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (uip.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    var mask = uip.IPv4Mask;
                    if (mask == null) continue;
                    var broadcast = GetBroadcast(uip.Address, mask);
                    if (broadcast != null) list.Add(broadcast);
                }
            }
        }
        catch { /* ignore */ }
        if (list.Count == 0)
            list.Add(IPAddress.Broadcast);
        return list;
    }

    private static IPAddress? GetBroadcast(IPAddress address, IPAddress mask)
    {
        var addrBytes = address.GetAddressBytes();
        var maskBytes = mask.GetAddressBytes();
        if (addrBytes.Length != 4 || maskBytes.Length != 4) return null;
        var broadcast = new byte[4];
        for (int i = 0; i < 4; i++)
            broadcast[i] = (byte)(addrBytes[i] | ~maskBytes[i]);
        return new IPAddress(broadcast);
    }

    private async Task TimeoutLoopAsync(CancellationToken ct)
    {
        var timeoutTicks = TimeSpan.FromMilliseconds(DeviceTimeoutMs).Ticks;
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct).ConfigureAwait(false);
            var now = DateTime.UtcNow.Ticks;
            List<string>? toRemove = null;
            lock (_lock)
            {
                foreach (var kv in _devices)
                {
                    if (now - kv.Value.LastSeenTicks > timeoutTicks)
                    {
                        toRemove ??= new List<string>();
                        toRemove.Add(kv.Key);
                    }
                }
            }
            if (toRemove != null)
                foreach (var ip in toRemove)
                    RemoveDevice(ip);
        }
    }

    public void Dispose() => StopScanning();
}

public sealed record DeviceInfo(string DeviceName, string Ip, int Port, long LastSeenTicks);
