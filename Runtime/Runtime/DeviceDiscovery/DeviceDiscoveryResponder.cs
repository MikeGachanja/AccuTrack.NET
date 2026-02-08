using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AccuTrack.Runtime.DeviceDiscovery;

/// <summary>
/// Responds to discovery requests (UDP broadcast) with device name/IP so Designer can list devices.
/// </summary>
public sealed class DeviceDiscoveryResponder
{
    private UdpClient? _udp;
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private readonly int _port;
    private readonly string _deviceName;

    public DeviceDiscoveryResponder(int port = 9876, string deviceName = "AccuTrack Runtime")
    {
        _port = port;
        _deviceName = deviceName;
    }

    public void Start()
    {
        _udp = new UdpClient(_port);
        _cts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _udp?.Close();
        _udp = null;
        _receiveTask?.GetAwaiter().GetResult();
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        if (_udp == null) return;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(ct).ConfigureAwait(false);
                var msg = Encoding.UTF8.GetString(result.Buffer);
                if (msg.Contains("DISCOVER", StringComparison.OrdinalIgnoreCase))
                {
                    var response = $"{_deviceName}|{GetLocalIP()}|{_port}";
                    var bytes = Encoding.UTF8.GetBytes(response);
                    await _udp.SendAsync(bytes, result.RemoteEndPoint).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { /* ignore */ }
        }
    }

    private static string GetLocalIP()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            var endPoint = (IPEndPoint)socket.LocalEndPoint!;
            return endPoint.Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }
}
