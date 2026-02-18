using System;
using System.Windows.Forms;
using System.Drawing;

namespace Designer.Modules.Console;

/// <summary>
/// Console module providing output and debug consoles.
/// </summary>
public class ConsoleModule
{
    private TextBox _outputConsole;
    private TextBox _debugConsole;

    public ConsoleModule()
    {
        InitializeConsoles();
    }

    private void InitializeConsoles()
    {
        // Output Console (copy via Ctrl+C or context menu)
        _outputConsole = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9),
            BackColor = Color.Black,
            ForeColor = Color.LightGreen,
            ShortcutsEnabled = true
        };
        _outputConsole.ContextMenuStrip = CreateConsoleContextMenu(_outputConsole);

        // Debug Console (copy via Ctrl+C or context menu)
        _debugConsole = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9),
            BackColor = Color.Black,
            ForeColor = Color.Yellow,
            ShortcutsEnabled = true
        };
        _debugConsole.ContextMenuStrip = CreateConsoleContextMenu(_debugConsole);
    }

    private static ContextMenuStrip CreateConsoleContextMenu(TextBox box)
    {
        var menu = new ContextMenuStrip();
        var copyItem = new ToolStripMenuItem("Copy", null, (s, e) => box.Copy()) { ShortcutKeyDisplayString = "Ctrl+C" };
        var selectAllItem = new ToolStripMenuItem("Select All", null, (s, e) => box.SelectAll()) { ShortcutKeyDisplayString = "Ctrl+A" };
        menu.Items.Add(copyItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(selectAllItem);
        return menu;
    }

    /// <summary>
    /// Gets the output console control.
    /// </summary>
    public Control GetOutputConsole() => _outputConsole;

    /// <summary>
    /// Gets the debug console control.
    /// </summary>
    public Control GetDebugConsole() => _debugConsole;

    /// <summary>
    /// Handles compiler message output.
    /// </summary>
    public void HandleCompilerMessage(string message)
    {
        if (_outputConsole != null)
        {
            _outputConsole.AppendText($"[INFO] {message}\r\n");
            _outputConsole.SelectionStart = _outputConsole.Text.Length;
            _outputConsole.ScrollToCaret();
        }
    }

    /// <summary>
    /// Handles compiler error output.
    /// </summary>
    public void HandleCompilerError(string error)
    {
        if (_outputConsole != null)
        {
            // TextBox doesn't support SelectionColor, so we'll use a different approach
            // For now, just append with prefix
            _outputConsole.AppendText($"[ERROR] {error}\r\n");
            _outputConsole.SelectionStart = _outputConsole.Text.Length;
            _outputConsole.ScrollToCaret();
        }
    }

    /// <summary>
    /// Handles compiler warning output.
    /// </summary>
    public void HandleCompilerWarning(string warning)
    {
        if (_outputConsole != null)
        {
            // TextBox doesn't support SelectionColor, so we'll use a different approach
            // For now, just append with prefix
            _outputConsole.AppendText($"[WARNING] {warning}\r\n");
            _outputConsole.SelectionStart = _outputConsole.Text.Length;
            _outputConsole.ScrollToCaret();
        }
    }

    /// <summary>
    /// Clears the output console.
    /// </summary>
    public void ClearOutput()
    {
        _outputConsole?.Clear();
    }

    /// <summary>
    /// Clears the debug console.
    /// </summary>
    public void ClearDebug()
    {
        _debugConsole?.Clear();
    }

    /// <summary>
    /// Writes debug message.
    /// </summary>
    public void WriteDebug(string message)
    {
        if (_debugConsole != null)
        {
            _debugConsole.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
            _debugConsole.SelectionStart = _debugConsole.Text.Length;
            _debugConsole.ScrollToCaret();
        }
    }
}
