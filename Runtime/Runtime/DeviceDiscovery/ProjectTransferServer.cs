using System.IO.Compression;
using System.Net;
using System.Text;

namespace AccuTrack.Runtime.DeviceDiscovery;

/// <summary>
/// HTTP server to receive uploaded project (zip or files); save to data/; raise progress and completion; on completion trigger project load and ReinitializeWithProject.
/// </summary>
public sealed class ProjectTransferServer
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private readonly string _dataPath;
    private readonly int _port;

    public ProjectTransferServer(string dataPath, int port = 9080)
    {
        _dataPath = dataPath;
        _port = port;
    }

    public event EventHandler? TransferStarted;
    public event EventHandler<(int percent, string status)>? TransferProgress;
    public event EventHandler<string>? TransferCompleted;
    public event EventHandler<string>? ErrorOccurred;

    public void Start()
    {
        _listener = new HttpListener();
        // Use localhost to avoid "Access is denied" on Windows (http://+:port requires admin or netsh urlacl)
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();
        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => ListenAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener = null;
        _listenTask?.GetAwaiter().GetResult();
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(ct).ConfigureAwait(false);
                _ = Task.Run(() => HandleRequestAsync(context));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath == "/upload")
            {
                TransferStarted?.Invoke(this, EventArgs.Empty);
                TransferProgress?.Invoke(this, (0, "Receiving..."));
                Directory.CreateDirectory(_dataPath);
                var contentType = context.Request.ContentType ?? "";
                if (contentType.Contains("multipart", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = context.Request.InputStream;
                    var boundary = GetBoundary(contentType);
                    var buffer = new byte[context.Request.ContentLength64];
                    var read = 0;
                    while (read < buffer.Length)
                        read += await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read)).ConfigureAwait(false);
                    TransferProgress?.Invoke(this, (50, "Extracting..."));
                    var tempZip = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");
                    try
                    {
                        await ExtractMultipartZipAsync(buffer, boundary, tempZip).ConfigureAwait(false);
                        ZipFile.ExtractToDirectory(tempZip, _dataPath, true);
                        TransferProgress?.Invoke(this, (100, "Done"));
                        TransferCompleted?.Invoke(this, _dataPath);
                    }
                    finally
                    {
                        if (File.Exists(tempZip)) File.Delete(tempZip);
                    }
                }
                else
                {
                    using var stream = context.Request.InputStream;
                    var zipPath = Path.Combine(_dataPath, "project.zip");
                    await using (var fs = File.Create(zipPath))
                        await stream.CopyToAsync(fs).ConfigureAwait(false);
                    TransferProgress?.Invoke(this, (50, "Extracting..."));
                    ZipFile.ExtractToDirectory(zipPath, _dataPath, true);
                    File.Delete(zipPath);
                    TransferProgress?.Invoke(this, (100, "Done"));
                    TransferCompleted?.Invoke(this, _dataPath);
                }
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/plain";
                var ok = Encoding.UTF8.GetBytes("OK");
                await context.Response.OutputStream.WriteAsync(ok).ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }
    }

    private static string? GetBoundary(string contentType)
    {
        var parts = contentType.Split(';');
        foreach (var p in parts)
        {
            var kv = p.Trim().Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length == 2 && kv[0].Trim().Equals("boundary", StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim().Trim('"');
        }
        return null;
    }

    private static async Task ExtractMultipartZipAsync(byte[] buffer, string? boundary, string zipPath)
    {
        if (string.IsNullOrEmpty(boundary)) throw new InvalidOperationException("No boundary");
        var b = Encoding.UTF8.GetBytes("--" + boundary);
        var start = FindSequence(buffer, b);
        if (start < 0) throw new InvalidOperationException("Boundary not found");
        start += b.Length;
        var headersEnd = FindSequence(buffer, new byte[] { 13, 10, 13, 10 }, start);
        if (headersEnd < 0) headersEnd = buffer.Length;
        var bodyStart = headersEnd + 4;
        var endBoundary = Encoding.UTF8.GetBytes("--" + boundary + "--");
        var bodyEnd = FindSequence(buffer, endBoundary, bodyStart);
        if (bodyEnd < 0) bodyEnd = buffer.Length;
        var zipLength = bodyEnd - bodyStart;
        await using var fs = File.Create(zipPath);
        await fs.WriteAsync(buffer.AsMemory(bodyStart, zipLength)).ConfigureAwait(false);
    }

    private static int FindSequence(byte[] buffer, byte[] seq, int start = 0)
    {
        for (var i = start; i <= buffer.Length - seq.Length; i++)
        {
            var found = true;
            for (var j = 0; j < seq.Length; j++)
            {
                if (buffer[i + j] != seq[j]) { found = false; break; }
            }
            if (found) return i;
        }
        return -1;
    }
}
