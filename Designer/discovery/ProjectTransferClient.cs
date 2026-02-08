using System.Buffers.Binary;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AccuTrack.Discovery;

/// <summary>
/// Transfers the built project to the Runtime via TCP (Qt protocol, port 8888).
/// Sends JSON "deploy" command, then binary file stream: [pathLen][path][fileSize][bytes]... pathLen 0 = end.
/// </summary>
public sealed class ProjectTransferClient
{
    /// <summary>Port used by Runtime for TCP project transfer (matches Qt Runtime and AccuTrack.Runtime.DeviceDiscovery.ProjectTransferServer.DefaultPort).</summary>
    public const int TransferPort = 8888;

    private readonly ProjectPackager _packager = new();
    private bool _transferring;

    public bool IsTransferring => _transferring;

    public event EventHandler<int>? TransferProgress;
    public event EventHandler<(bool success, string message)>? TransferComplete;
    public event EventHandler<string>? ErrorOccurred;

    public async Task<bool> DeployAsync(string buildDirectory, string deviceIp, int devicePort = TransferPort, CancellationToken cancellationToken = default)
    {
        if (_transferring)
        {
            ErrorOccurred?.Invoke(this, "Transfer already in progress");
            return false;
        }

        if (!_packager.Package(buildDirectory))
        {
            ErrorOccurred?.Invoke(this, "Failed to package project (build directory empty or missing).");
            return false;
        }

        _transferring = true;
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(deviceIp), devicePort, cancellationToken).ConfigureAwait(false);
            using var stream = client.GetStream();

            var projectName = _packager.ProjectName;
            var fileCount = _packager.Files.Count;
            var totalSize = _packager.TotalSize;

            var deployCmd = JsonSerializer.Serialize(new
            {
                command = "deploy",
                projectName,
                fileCount,
                totalSize
            }) + "\n";
            var cmdBytes = Encoding.UTF8.GetBytes(deployCmd);
            await stream.WriteAsync(cmdBytes, cancellationToken).ConfigureAwait(false);

            var responseBuffer = new byte[1024];
            var responseLen = await ReadLineAsync(stream, responseBuffer, cancellationToken).ConfigureAwait(false);
            if (responseLen < 0)
            {
                ErrorOccurred?.Invoke(this, "No response from Runtime");
                TransferComplete?.Invoke(this, (false, "Deploy failed."));
                return false;
            }
            var responseJson = Encoding.UTF8.GetString(responseBuffer.AsSpan(0, responseLen));
            using (var doc = JsonDocument.Parse(responseJson))
            {
                var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
                if (status != "ready")
                {
                    var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "Runtime not ready";
                    ErrorOccurred?.Invoke(this, msg ?? "Deploy failed");
                    TransferComplete?.Invoke(this, (false, msg ?? "Deploy failed."));
                    return false;
                }
            }

            TransferProgress?.Invoke(this, 5);
            long sent = 0;
            var files = _packager.Files;
            for (var i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = files[i];
                var pathBytes = Encoding.UTF8.GetBytes(entry.RelativePath);
                var pathLen = pathBytes.Length;
                var size = (int)Math.Min(entry.Size, int.MaxValue);

                var header = new byte[4 + pathLen + 4];
                BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), pathLen);
                pathBytes.CopyTo(header, 4);
                BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4 + pathLen, 4), size);

                await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                await using (var fs = File.OpenRead(entry.AbsolutePath))
                {
                    var toSend = size;
                    var buf = new byte[Math.Min(65536, toSend)];
                    while (toSend > 0)
                    {
                        var read = await fs.ReadAsync(buf.AsMemory(0, Math.Min(buf.Length, toSend)), cancellationToken).ConfigureAwait(false);
                        if (read <= 0) break;
                        await stream.WriteAsync(buf.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                        toSend -= read;
                    }
                }
                sent += entry.Size;
                var pct = totalSize > 0 ? (int)((sent * 95) / totalSize) + 5 : (i + 1) * 95 / files.Count;
                TransferProgress?.Invoke(this, Math.Min(99, pct));
            }

            var endMarker = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(endMarker, 0);
            await stream.WriteAsync(endMarker, cancellationToken).ConfigureAwait(false);

            responseLen = await ReadLineAsync(stream, responseBuffer, cancellationToken).ConfigureAwait(false);
            if (responseLen > 0)
            {
                responseJson = Encoding.UTF8.GetString(responseBuffer.AsSpan(0, responseLen));
                using (var doc = JsonDocument.Parse(responseJson))
                {
                    var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
                    if (status == "complete")
                    {
                        TransferProgress?.Invoke(this, 100);
                        TransferComplete?.Invoke(this, (true, "Project deployed successfully."));
                        return true;
                    }
                }
            }

            TransferProgress?.Invoke(this, 100);
            TransferComplete?.Invoke(this, (true, "Project deployed successfully."));
            return true;
        }
        catch (OperationCanceledException)
        {
            ErrorOccurred?.Invoke(this, "Transfer cancelled.");
            TransferComplete?.Invoke(this, (false, "Cancelled."));
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            TransferComplete?.Invoke(this, (false, ex.Message));
            return false;
        }
        finally
        {
            _transferring = false;
        }
    }

    private static async Task<int> ReadLineAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
    {
        var count = 0;
        while (count < buffer.Length)
        {
            var b = new byte[1];
            var read = await stream.ReadAsync(b, ct).ConfigureAwait(false);
            if (read <= 0) return -1;
            if (b[0] == (byte)'\n') return count;
            buffer[count++] = b[0];
        }
        return count;
    }
}
