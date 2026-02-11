using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Designer.Modules.Discovery;

/// <summary>
/// TCP client that sends a packaged project to the Runtime's ProjectTransferServer.
/// Protocol: JSON deploy command (newline), then [pathLen:4][path][size:4][bytes] per file, then pathLen=0.
/// </summary>
public class ProjectTransferClient
{
    private const int DefaultPort = 8888;
    private const int ChunkSize = 64 * 1024;

    public event EventHandler<string>? TransferStarted;
    public event EventHandler<int>? TransferProgress;
    public event EventHandler<string>? TransferComplete;
    public event EventHandler<string>? ErrorOccurred;

    public async Task<bool> DeployProjectAsync(string buildDirectory, string host, int port = DefaultPort, CancellationToken cancellationToken = default)
    {
        var packager = new ProjectPackager();
        packager.PackagingProgress += (_, msg) => 
        {
            System.Diagnostics.Debug.WriteLine($"Packager: {msg}");
        };
        
        if (!packager.PackageProject(buildDirectory))
        {
            string errorMsg = $"Failed to package project from {buildDirectory}";
            System.Diagnostics.Debug.WriteLine(errorMsg);
            ErrorOccurred?.Invoke(this, errorMsg);
            return false;
        }

        var files = packager.ProjectFiles;
        if (files.Count == 0)
        {
            string errorMsg = $"No files to transfer from {buildDirectory}";
            System.Diagnostics.Debug.WriteLine(errorMsg);
            ErrorOccurred?.Invoke(this, errorMsg);
            return false;
        }
        
        System.Diagnostics.Debug.WriteLine($"Transferring {files.Count} files ({packager.TotalSize} bytes) to {host}:{port}");

        var command = new Dictionary<string, object>
        {
            ["command"] = "deploy",
            ["projectName"] = packager.ProjectName,
            ["fileCount"] = files.Count,
            ["totalSize"] = packager.TotalSize
        };
        string commandJson = JsonSerializer.Serialize(command) + "\n";
        byte[] commandBytes = Encoding.UTF8.GetBytes(commandJson);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            using var stream = client.GetStream();

            TransferStarted?.Invoke(this, packager.ProjectName);
            await stream.WriteAsync(commandBytes, cancellationToken).ConfigureAwait(false);

            var buffer = new byte[4096];
            int read = await ReadLineAsync(stream, buffer, cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                ErrorOccurred?.Invoke(this, "No response from server");
                return false;
            }

            string responseLine = Encoding.UTF8.GetString(buffer, 0, read).Trim();
            using var doc = JsonDocument.Parse(responseLine);
            var root = doc.RootElement;
            string status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
            if (status != "ready")
            {
                string msg = root.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "Server not ready";
                ErrorOccurred?.Invoke(this, msg);
                return false;
            }

            long sentTotal = 0;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await SendFileAsync(stream, file, cancellationToken).ConfigureAwait(false))
                {
                    ErrorOccurred?.Invoke(this, "Failed to send file: " + file.RelativePath);
                    return false;
                }
                sentTotal += file.Size;
                int progress = packager.TotalSize > 0 ? (int)((sentTotal * 100) / packager.TotalSize) : 100;
                TransferProgress?.Invoke(this, Math.Min(100, progress));
            }

            var endMarker = BitConverter.GetBytes(0);
            await stream.WriteAsync(endMarker, cancellationToken).ConfigureAwait(false);

            read = await ReadLineAsync(stream, buffer, cancellationToken).ConfigureAwait(false);
            if (read > 0)
            {
                responseLine = Encoding.UTF8.GetString(buffer, 0, read).Trim();
                try
                {
                    using var completeDoc = JsonDocument.Parse(responseLine);
                    var completeRoot = completeDoc.RootElement;
                    if (completeRoot.TryGetProperty("status", out var cs) && cs.GetString() == "complete")
                    {
                        TransferComplete?.Invoke(this, "Project deployed successfully");
                        return true;
                    }
                }
                catch { /* ignore */ }
            }

            TransferComplete?.Invoke(this, "Project deployed successfully");
            return true;
        }
        catch (OperationCanceledException)
        {
            ErrorOccurred?.Invoke(this, "Transfer cancelled");
            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            return false;
        }
    }

    private static async Task<int> ReadLineAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int count = 0;
        while (count < buffer.Length)
        {
            int available = buffer.Length - count;
            int r = await stream.ReadAsync(buffer.AsMemory(count, available), ct).ConfigureAwait(false);
            if (r <= 0) return count;
            
            // Check if we got a newline in this read
            for (int i = count; i < count + r; i++)
            {
                if (buffer[i] == (byte)'\n')
                {
                    return i + 1; // Include the newline
                }
            }
            
            count += r;
        }
        return count;
    }

    private static async Task<bool> SendFileAsync(Stream stream, ProjectFile file, CancellationToken ct)
    {
        if (!File.Exists(file.AbsolutePath))
        {
            System.Diagnostics.Debug.WriteLine($"File not found: {file.AbsolutePath}");
            return false;
        }

        try
        {
            byte[] pathBytes = Encoding.UTF8.GetBytes(file.RelativePath);
            int pathLen = pathBytes.Length;
            int fileSize = (int)Math.Min(file.Size, int.MaxValue);

            System.Diagnostics.Debug.WriteLine($"Sending file: {file.RelativePath} ({fileSize} bytes)");

            byte[] pathLenBytes = BitConverter.GetBytes(pathLen);
            byte[] sizeBytes = BitConverter.GetBytes(fileSize);
            await stream.WriteAsync(pathLenBytes, ct).ConfigureAwait(false);
            await stream.WriteAsync(pathBytes, ct).ConfigureAwait(false);
            await stream.WriteAsync(sizeBytes, ct).ConfigureAwait(false);

            await using var fs = new FileStream(file.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, ChunkSize, true);
            var chunk = new byte[ChunkSize];
            long remaining = file.Size;
            long sent = 0;
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(remaining, ChunkSize);
                int read = await fs.ReadAsync(chunk.AsMemory(0, toRead), ct).ConfigureAwait(false);
                if (read <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected end of file: {file.RelativePath} (read {sent} of {file.Size} bytes)");
                    return false;
                }
                await stream.WriteAsync(chunk.AsMemory(0, read), ct).ConfigureAwait(false);
                remaining -= read;
                sent += read;
            }
            
            System.Diagnostics.Debug.WriteLine($"File sent successfully: {file.RelativePath} ({sent} bytes)");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending file {file.RelativePath}: {ex.Message}");
            return false;
        }
    }
}
