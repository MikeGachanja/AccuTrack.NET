using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace AccuTrack.Runtime.DeviceDiscovery;

/// <summary>
/// TCP project transfer server matching Qt Runtime protocol (port 8888).
/// Designer sends: JSON "deploy" command → Runtime responds "ready" → binary file stream (pathLen, path, fileSize, bytes)… pathLen 0 = end.
/// </summary>
public sealed class ProjectTransferServer
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private readonly string _dataPath;
    private readonly int _port;
    private TcpClient? _currentClient;
    private readonly object _clientLock = new();

    public const int DefaultPort = 8888;

    public ProjectTransferServer(string dataPath, int port = DefaultPort)
    {
        _dataPath = dataPath;
        _port = port;
    }

    public event EventHandler? TransferStarted;
    public event EventHandler<(int percent, string status)>? TransferProgress;
    public event EventHandler<string>? TransferCompleted;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        IsRunning = true;
        _cts = new CancellationTokenSource();
        _acceptTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        lock (_clientLock)
        {
            _currentClient?.Close();
            _currentClient = null;
        }
        _listener?.Stop();
        _listener = null;
        try { _acceptTask?.GetAwaiter().GetResult(); } catch { }
        IsRunning = false;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _ = Task.Run(() => HandleClientAsync(client), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        lock (_clientLock)
        {
            if (_currentClient != null)
            {
                client.Close();
                return;
            }
            _currentClient = client;
        }

        try
        {
            using var stream = client.GetStream();
            var state = TransferState.WaitingForCommand;
            var buffer = new List<byte>();
            string? projectName = null;
            int expectedFileCount = 0;
            long expectedTotalSize = 0;
            int receivedFileCount = 0;
            long receivedTotalSize = 0;
            string currentFilePath = "";
            long currentFileSize = 0;
            long currentFileReceived = 0;

            while (true)
            {
                var readBuf = new byte[8192];
                var read = await stream.ReadAsync(readBuf).ConfigureAwait(false);
                if (read <= 0) break;
                buffer.AddRange(readBuf.AsSpan(0, read).ToArray());

                if (state == TransferState.WaitingForCommand)
                {
                    var newline = -1;
                    for (var i = 0; i < buffer.Count; i++)
                    {
                        if (buffer[i] == (byte)'\n') { newline = i; break; }
                    }
                    if (newline < 0 && buffer.Count > 65536)
                    {
                        ErrorOccurred?.Invoke(this, "Invalid command");
                        break;
                    }
                    if (newline < 0) continue;

                    var line = Encoding.UTF8.GetString(buffer.Take(newline).ToArray());
                    buffer.RemoveRange(0, newline + 1);
                    JsonDocument? doc = null;
                    try
                    {
                        doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;
                        var cmd = root.TryGetProperty("command", out var c) ? c.GetString() : null;
                        if (cmd == "deploy")
                        {
                            projectName = root.TryGetProperty("projectName", out var pn) ? pn.GetString() ?? "Project" : "Project";
                            expectedFileCount = root.TryGetProperty("fileCount", out var fc) ? fc.GetInt32() : 0;
                            expectedTotalSize = root.TryGetProperty("totalSize", out var ts) ? ts.GetInt64() : 0;

                            if (Directory.Exists(_dataPath))
                            {
                                try { Directory.Delete(_dataPath, true); } catch { }
                            }
                            Directory.CreateDirectory(_dataPath);

                            var response = JsonSerializer.Serialize(new { status = "ready" }) + "\n";
                            var responseBytes = Encoding.UTF8.GetBytes(response);
                            await stream.WriteAsync(responseBytes).ConfigureAwait(false);

                            state = TransferState.ReceivingFiles;
                            TransferStarted?.Invoke(this, EventArgs.Empty);
                            TransferProgress?.Invoke(this, (0, "Receiving..."));
                        }
                        else
                        {
                            var err = JsonSerializer.Serialize(new { status = "error", message = "Unknown command" }) + "\n";
                            await stream.WriteAsync(Encoding.UTF8.GetBytes(err)).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        doc?.Dispose();
                    }
                    continue;
                }

                if (state == TransferState.ReceivingFiles)
                {
                    while (buffer.Count >= 4)
                    {
                        if (string.IsNullOrEmpty(currentFilePath))
                        {
                            var pathLen = ReadInt32LittleEndian(buffer, 0);
                            if (pathLen == 0)
                            {
                                buffer.RemoveRange(0, 4);
                                var completeResponse = JsonSerializer.Serialize(new { status = "complete", projectName }) + "\n";
                                await stream.WriteAsync(Encoding.UTF8.GetBytes(completeResponse)).ConfigureAwait(false);
                                TransferProgress?.Invoke(this, (100, "Done"));
                                TransferCompleted?.Invoke(this, _dataPath);
                                return;
                            }
                            if (pathLen < 0 || pathLen > 8192)
                            {
                                ErrorOccurred?.Invoke(this, "Invalid path length in transfer");
                                return;
                            }
                            if (buffer.Count < 4 + pathLen + 4) break;
                            currentFilePath = Encoding.UTF8.GetString(buffer.GetRange(4, pathLen).ToArray());
                            currentFileSize = ReadInt32LittleEndian(buffer, 4 + pathLen);
                            currentFileReceived = 0;
                            buffer.RemoveRange(0, 4 + pathLen + 4);

                            var fullPath = Path.Combine(_dataPath, currentFilePath);
                            var dir = Path.GetDirectoryName(fullPath);
                            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                        }

                        var toRead = (int)Math.Min(currentFileSize - currentFileReceived, buffer.Count);
                        if (toRead > 0)
                        {
                            var fullPath = Path.Combine(_dataPath, currentFilePath);
                            var mode = currentFileReceived == 0 ? FileMode.Create : FileMode.Append;
                            await using (var fs = new FileStream(fullPath, mode, FileAccess.Write, FileShare.None))
                                await fs.WriteAsync(buffer.Take(toRead).ToArray()).ConfigureAwait(false);
                            buffer.RemoveRange(0, toRead);
                            currentFileReceived += toRead;
                            receivedTotalSize += toRead;
                        }

                        if (currentFileReceived >= currentFileSize)
                        {
                            receivedFileCount++;
                            currentFilePath = "";
                            currentFileSize = 0;
                            currentFileReceived = 0;
                            var progress = expectedTotalSize > 0
                                ? (int)((receivedTotalSize * 100) / expectedTotalSize)
                                : (expectedFileCount > 0 ? (receivedFileCount * 100) / expectedFileCount : 0);
                            TransferProgress?.Invoke(this, (Math.Min(100, progress), "Receiving..."));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
        finally
        {
            lock (_clientLock)
            {
                if (_currentClient == client)
                    _currentClient = null;
            }
            client.Close();
        }
    }

    private static int ReadInt32LittleEndian(List<byte> list, int start)
    {
        if (list.Count < start + 4) return 0;
        Span<byte> span = stackalloc byte[4];
        for (var i = 0; i < 4; i++) span[i] = list[start + i];
        return BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    private enum TransferState { WaitingForCommand, ReceivingFiles }
}
