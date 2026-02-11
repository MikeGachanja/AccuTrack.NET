using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Runtime.Modules.Discovery;

/// <summary>TCP server that receives project deploy: JSON "deploy" command then binary file stream.</summary>
public sealed class ProjectTransferServer
{
    private TcpListener? _listener;
    private readonly object _lock = new();
    private volatile bool _running;
    private const int DefaultPort = 8888;

    public bool IsRunning => _running;

    public event EventHandler<string>? TransferStarted;
    public event EventHandler<int>? TransferProgress;
    public event EventHandler<string>? TransferCompleted;
    public event EventHandler<string>? ErrorOccurred;

    public bool Start(ushort port = DefaultPort)
    {
        lock (_lock)
        {
            if (_running) return true;
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                _running = true;
                _ = AcceptLoopAsync();
                return true;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
                return false;
            }
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_running) return;
            _running = false;
            try { _listener?.Stop(); } catch { }
            _listener = null;
        }
    }

    private string GetStoragePath()
    {
        var appDir = AppContext.BaseDirectory;
        return Path.Combine(appDir, "data");
    }

    private async Task AcceptLoopAsync()
    {
        while (_running && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _ = HandleClientAsync(client);
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) { ErrorOccurred?.Invoke(this, ex.Message); }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        string? projectName = null;
        int expectedFileCount = 0;
        long expectedTotalSize = 0;
        long receivedTotalSize = 0;
        var state = TransferState.WaitingForCommand;
        var buffer = new List<byte>();
        string? currentFilePath = null;
        int currentFileSize = 0;
        int currentFileReceived = 0;

        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var readBuf = new byte[8192];
                while (_running)
                {
                    var read = await stream.ReadAsync(readBuf).ConfigureAwait(false);
                    if (read <= 0) break;
                    buffer.AddRange(readBuf.AsSpan(0, read).ToArray());

                    if (state == TransferState.WaitingForCommand)
                    {
                        int lineEnd = -1;
                        for (int i = 0; i < buffer.Count; i++) { if (buffer[i] == (byte)'\n') { lineEnd = i; break; } }
                        if (lineEnd < 0 && buffer.Count > 10000) { ErrorOccurred?.Invoke(this, "Invalid command"); break; }
                        if (lineEnd < 0) continue;

                        var line = Encoding.UTF8.GetString(buffer.Take(lineEnd).ToArray());
                        buffer.RemoveRange(0, lineEnd + 1);
                        JsonDocument? doc = null;
                        try { doc = JsonDocument.Parse(line); } catch { }
                        if (doc == null) continue;
                        using (doc)
                        {
                            var root = doc.RootElement;
                            var cmd = root.TryGetProperty("command", out var c) ? c.GetString() : "";
                            if (cmd == "deploy")
                            {
                                projectName = root.TryGetProperty("projectName", out var pn) ? pn.GetString() ?? "" : "";
                                expectedFileCount = root.TryGetProperty("fileCount", out var fc) ? fc.GetInt32() : 0;
                                expectedTotalSize = root.TryGetProperty("totalSize", out var ts) ? ts.GetInt64() : 0;
                                var storagePath = GetStoragePath();
                                if (Directory.Exists(storagePath))
                                    try { Directory.Delete(storagePath, true); } catch { }
                                Directory.CreateDirectory(storagePath);
                                var response = "{\"status\":\"ready\"}\n";
                                await stream.WriteAsync(Encoding.UTF8.GetBytes(response)).ConfigureAwait(false);
                                state = TransferState.ReceivingFiles;
                                TransferStarted?.Invoke(this, projectName ?? "");
                                TransferProgress?.Invoke(this, 0);
                            }
                            else
                            {
                                await stream.WriteAsync(Encoding.UTF8.GetBytes("{\"status\":\"error\",\"message\":\"Unknown command\"}\n")).ConfigureAwait(false);
                            }
                        }
                        continue;
                    }

                    if (state == TransferState.ReceivingFiles)
                    {
                        while (buffer.Count >= 4)
                        {
                            if (currentFilePath == null)
                            {
                                int pathLen = BitConverter.ToInt32(buffer.Take(4).ToArray(), 0);
                                if (pathLen == 0)
                                {
                                    buffer.RemoveRange(0, 4);
                                    var projectPath = GetStoragePath();
                                    System.Diagnostics.Debug.WriteLine($"Transfer complete. Received {receivedTotalSize} bytes. Writing completion response.");
                                    await stream.WriteAsync(Encoding.UTF8.GetBytes("{\"status\":\"complete\",\"projectName\":\"" + (projectName ?? "") + "\"}\n")).ConfigureAwait(false);
                                    TransferCompleted?.Invoke(this, projectPath);
                                    return;
                                }
                                if (buffer.Count < 4 + pathLen + 4) break;
                                currentFilePath = Encoding.UTF8.GetString(buffer.Skip(4).Take(pathLen).ToArray());
                                currentFileSize = BitConverter.ToInt32(buffer.Skip(4 + pathLen).Take(4).ToArray(), 0);
                                currentFileReceived = 0;
                                buffer.RemoveRange(0, 4 + pathLen + 4);
                                var fullPath = Path.Combine(GetStoragePath(), currentFilePath);
                                var dir = Path.GetDirectoryName(fullPath);
                                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                                {
                                    Directory.CreateDirectory(dir);
                                }
                                System.Diagnostics.Debug.WriteLine($"Receiving file: {currentFilePath} ({currentFileSize} bytes)");
                            }

                            int toRead = Math.Min(currentFileSize - currentFileReceived, buffer.Count);
                            if (toRead <= 0) 
                            { 
                                if (currentFileReceived >= currentFileSize)
                                {
                                    // File complete
                                    System.Diagnostics.Debug.WriteLine($"File received: {currentFilePath} ({currentFileReceived} bytes)");
                                    int progress = expectedTotalSize > 0
                                        ? (int)((receivedTotalSize * 100) / expectedTotalSize)
                                        : (expectedFileCount > 0 ? (receivedTotalSize > 0 ? 50 : 0) : 100);
                                    TransferProgress?.Invoke(this, Math.Min(100, progress));
                                    currentFilePath = null;
                                }
                                continue; 
                            }
                            
                            var fullPath2 = Path.Combine(GetStoragePath(), currentFilePath!);
                            try
                            {
                                using (var fs = new FileStream(fullPath2, currentFileReceived == 0 ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.None))
                                {
                                    var dataToWrite = buffer.Take(toRead).ToArray();
                                    fs.Write(dataToWrite, 0, toRead);
                                }
                                buffer.RemoveRange(0, toRead);
                                currentFileReceived += toRead;
                                receivedTotalSize += toRead;
                                
                                if (currentFileReceived >= currentFileSize)
                                {
                                    System.Diagnostics.Debug.WriteLine($"File received: {currentFilePath} ({currentFileReceived} bytes)");
                                    int progress = expectedTotalSize > 0
                                        ? (int)((receivedTotalSize * 100) / expectedTotalSize)
                                        : (expectedFileCount > 0 ? (receivedTotalSize > 0 ? 50 : 0) : 100);
                                    TransferProgress?.Invoke(this, Math.Min(100, progress));
                                    currentFilePath = null;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error writing file {fullPath2}: {ex.Message}");
                                ErrorOccurred?.Invoke(this, $"Failed to write file {currentFilePath}: {ex.Message}");
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private enum TransferState { WaitingForCommand, ReceivingFiles }
}
