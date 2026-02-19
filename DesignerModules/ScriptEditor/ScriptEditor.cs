using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Designer.Modules.ScriptEditor;
using MoonSharp.Interpreter;

namespace Designer.Modules.ScriptEditor;

/// <summary>
/// Editor for Lua scripts with Run/Output (MoonSharp) and optional syntax highlighting.
/// </summary>
public partial class ScriptEditor : UserControl
{
    private RichTextBox _codeTextBox;
    private LuaScript? _script;
    private bool _modified;
    private List<string> _availableTags = new List<string>();
    private LuaSyntaxHighlighter? _highlighter;
    private System.Windows.Forms.Timer? _highlightTimer;
    private bool _isHighlighting;

    public event EventHandler<bool>? ModifiedChanged;
    /// <summary>Raised when the user clicks Run (so the host can e.g. switch to the Debug tab).</summary>
    public event EventHandler? ScriptRunStarted;
    /// <summary>Raised when script produces output (e.g. print).</summary>
    public event EventHandler<string>? ScriptOutput;
    /// <summary>Raised when script execution fails.</summary>
    public event EventHandler<string>? ScriptError;

    public ScriptEditor()
    {
        InitializeComponent();
    }

    public bool IsModified => _modified;

    private void InitializeComponent()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(5)
        };

        // Toolbar: Run, Save, Clear
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 28,
            Padding = new Padding(0)
        };
        var runBtn = new Button
        {
            Text = "Run",
            Size = new Size(60, 24),
            Margin = new Padding(0, 0, 6, 0)
        };
        runBtn.Click += OnRunClicked;
        var saveBtn = new Button
        {
            Text = "Save",
            Size = new Size(60, 24),
            Margin = new Padding(0, 0, 6, 0)
        };
        saveBtn.Click += (s, e) => SaveScript();
        toolbar.Controls.Add(runBtn);
        toolbar.Controls.Add(saveBtn);

        // Code editor with Lua syntax highlighting and copy/paste
        var codeFont = new Font("Consolas", 10);
        _codeTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ScrollBars = RichTextBoxScrollBars.Both,
            Font = codeFont,
            WordWrap = false,
            BorderStyle = BorderStyle.FixedSingle,
            ShortcutsEnabled = true,
            AcceptsTab = true
        };
        _codeTextBox.ContextMenuStrip = CreateCodeEditorContextMenu(_codeTextBox);
        _highlighter = new LuaSyntaxHighlighter(_codeTextBox, codeFont);
        _highlightTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _highlightTimer.Tick += (s, e) =>
        {
            _highlightTimer!.Stop();
            SafeHighlight();
        };

        _codeTextBox.TextChanged += (s, e) =>
        {
            if (_isHighlighting) return;
            if (_script != null)
            {
                _modified = _codeTextBox.Text != _script.Code;
                ModifiedChanged?.Invoke(this, _modified);
            }
            _highlightTimer?.Stop();
            _highlightTimer?.Start();
        };

        mainLayout.Controls.Add(toolbar, 0, 0);
        mainLayout.Controls.Add(_codeTextBox, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(mainLayout);
    }

    private void SafeHighlight()
    {
        if (_highlighter == null) return;
        _isHighlighting = true;
        try
        {
            _highlighter.Highlight();
        }
        finally
        {
            _isHighlighting = false;
        }
    }

    private static ContextMenuStrip CreateCodeEditorContextMenu(RichTextBox codeBox)
    {
        var menu = new ContextMenuStrip();
        AddItem(menu, "Undo", (s, e) => codeBox.Undo(), Keys.Control | Keys.Z);
        menu.Items.Add(new ToolStripSeparator());
        AddItem(menu, "Cut", (s, e) => codeBox.Cut(), Keys.Control | Keys.X);
        AddItem(menu, "Copy", (s, e) => codeBox.Copy(), Keys.Control | Keys.C);
        AddItem(menu, "Paste", (s, e) => codeBox.Paste(), Keys.Control | Keys.V);
        menu.Items.Add(new ToolStripSeparator());
        AddItem(menu, "Select All", (s, e) => codeBox.SelectAll(), Keys.Control | Keys.A);
        return menu;
    }

    private static void AddItem(ContextMenuStrip menu, string text, EventHandler click, Keys shortcut)
    {
        var item = new ToolStripMenuItem(text, null, click) { ShortcutKeyDisplayString = shortcut.ToString().Replace("Control", "Ctrl") };
        menu.Items.Add(item);
    }

    private void OnRunClicked(object? sender, EventArgs e)
    {
        ScriptRunStarted?.Invoke(this, EventArgs.Empty);
        var code = _codeTextBox.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ScriptError?.Invoke(this, "Please enter a script first.");
            return;
        }
        ScriptOutput?.Invoke(this, "--- Run ---");
        try
        {
            var script = new Script();
            script.Options.DebugPrint = s => ScriptOutput?.Invoke(this, s ?? "");
            script.DoString(code);
            ScriptOutput?.Invoke(this, "--- Done ---");
        }
        catch (ScriptRuntimeException ex)
        {
            var msg = ex.DecoratedMessage ?? ex.Message;
            ScriptError?.Invoke(this, msg);
        }
        catch (SyntaxErrorException ex)
        {
            var msg = ex.DecoratedMessage ?? ex.Message;
            ScriptError?.Invoke(this, msg);
        }
        catch (Exception ex)
        {
            ScriptError?.Invoke(this, ex.Message);
        }
    }

    /// <summary>
    /// Sets the script to edit.
    /// </summary>
    public void SetScript(LuaScript script)
    {
        _script = script;
        _codeTextBox.Text = script.Code;
        _modified = false;
        _highlightTimer?.Stop();
        _highlighter?.UpdateTags(_availableTags);
        SafeHighlight();
    }

    /// <summary>
    /// Updates available tags for syntax highlighting (and future autocomplete).
    /// </summary>
    public void UpdateAvailableTags(List<string> tagNames)
    {
        _availableTags = tagNames ?? new List<string>();
        _highlighter?.UpdateTags(_availableTags);
        _highlightTimer?.Stop();
        SafeHighlight();
    }

    /// <summary>
    /// Prompts to save if modified.
    /// </summary>
    public bool MaybeSave()
    {
        if (!_modified || _script == null)
            return true;

        var result = MessageBox.Show(
            $"The script '{_script.Name}' has been modified. Do you want to save the changes?",
            "Save Changes",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            return SaveScript();
        }
        else if (result == DialogResult.Cancel)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Saves the script.
    /// </summary>
    public bool SaveScript()
    {
        if (_script == null)
            return false;

        _script.Code = _codeTextBox.Text;
        if (_script.SaveCode())
        {
            _modified = false;
            ModifiedChanged?.Invoke(this, false);
            return true;
        }

        MessageBox.Show("Failed to save script.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    /// <summary>
    /// Gets the current script code.
    /// </summary>
    public string GetCode() => _codeTextBox.Text;

    /// <summary>
    /// Sets the script code.
    /// </summary>
    public void SetCode(string code)
    {
        _codeTextBox.Text = code;
        _modified = false;
        _highlightTimer?.Stop();
        SafeHighlight();
    }
}
