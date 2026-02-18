using System.Diagnostics;
using Runtime.Modules.Console;

namespace Runtime;

/// <summary>
/// Forwards Debug and Trace output to the runtime console so messages appear on the Logs screen.
/// </summary>
internal sealed class ConsoleTraceListener : TraceListener
{
    private readonly IConsole _console;
    private readonly string _source;
    private string _buffer = "";

    public ConsoleTraceListener(IConsole console, string source = "Runtime")
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _source = source ?? "Runtime";
    }

    public override void Write(string? message)
    {
        if (string.IsNullOrEmpty(message)) return;
        _buffer += message;
    }

    public override void WriteLine(string? message)
    {
        if (!string.IsNullOrEmpty(message))
            _buffer += message;
        Flush();
    }

    public override void Flush()
    {
        if (string.IsNullOrEmpty(_buffer)) return;
        var msg = _buffer.TrimEnd('\r', '\n');
        _buffer = "";
        if (string.IsNullOrEmpty(msg)) return;
        _console.LogDebug(msg, _source);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Flush();
        base.Dispose(disposing);
    }
}
