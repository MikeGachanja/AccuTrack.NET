using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Runtime.Modules.Discovery;

/// <summary>UDP responder for designer discovery: listens on 8889, responds with device name and TCP port.</summary>
public sealed class DeviceDiscoveryResponder
{
    private const int DiscoveryPort = 8889;
    private const int TcpPortDefault = 8888;
    private UdpClient? _udp;
    private volatile bool _running;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _running;
    public string DeviceName { get; set; } = "Runtime-Device";
    public int TcpPort { get; set; } = TcpPortDefault;

    public bool Start()
    {
        if (_running) return true;
        try
        {
            _udp = new UdpClient(DiscoveryPort);
            _running = true;
            _cts = new CancellationTokenSource();
            _ = ReceiveLoopAsync(_cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Stop()
    {
        _running = false;
        _cts?.Cancel();
        try { _udp?.Close(); } catch { }
        _udp = null;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (_running && _udp != null && !ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(ct).ConfigureAwait(false);
                var data = Encoding.UTF8.GetString(result.Buffer);
                JsonDocument? doc = null;
                try { doc = JsonDocument.Parse(data); } catch { }
                if (doc == null) continue;
                using (doc)
                {
                    var root = doc.RootElement;
                    var type = root.TryGetProperty("type", out var t) ? t.GetString() : "";
                    var source = root.TryGetProperty("source", out var s) ? s.GetString() : "";
                    if (type == "discovery" && source == "designer")
                        SendResponse(result.RemoteEndPoint.Address, result.RemoteEndPoint.Port);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }

    private void SendResponse(IPAddress targetAddress, int targetPort)
    {
        if (_udp == null) return;
        var localIp = "127.0.0.1";
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                    break;
                }
            }
        }
        catch { }

        var response = new Dictionary<string, object>
        {
            ["type"] = "response",
            ["deviceName"] = DeviceName,
            ["ip"] = localIp,
            ["port"] = TcpPort,
            ["version"] = "1.0",
            ["deviceType"] = "AccuTrack-HMI"
        };
        var json = JsonSerializer.Serialize(response);
        var bytes = Encoding.UTF8.GetBytes(json);
        try
        {
            _udp.Send(bytes, bytes.Length, new IPEndPoint(targetAddress, targetPort));
        }
        catch { }
    }
}
