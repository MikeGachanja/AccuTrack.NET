using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Designer.Modules.ScriptEditor;

/// <summary>
/// Applies Lua syntax highlighting to a RichTextBox (keywords, comments, strings, numbers, tags).
/// </summary>
public sealed class LuaSyntaxHighlighter
{
    private readonly RichTextBox _box;
    private readonly Font _font;
    private List<string> _tagNames = new();
    private static readonly string[] Keywords = {
        "and", "break", "do", "else", "elseif", "end", "false", "for", "function",
        "if", "in", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
    };
    private static readonly Regex SingleLineComment = new Regex(@"--[^\n]*", RegexOptions.Compiled);
    private static readonly Regex MultiLineComment = new Regex(@"--\[\[[\s\S]*?\]\]", RegexOptions.Compiled);
    private static readonly Regex DoubleQuotedString = new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);
    private static readonly Regex SingleQuotedString = new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled);
    private static readonly Regex Number = new Regex(@"\b\d+\.?\d*\b", RegexOptions.Compiled);
    private static readonly Regex FunctionCall = new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*(?=\s*\()", RegexOptions.Compiled);

    private static readonly Color ColorKeyword = Color.FromArgb(0, 0, 128);
    private static readonly Color ColorComment = Color.Gray;
    private static readonly Color ColorString = Color.FromArgb(0, 128, 0);
    private static readonly Color ColorNumber = Color.FromArgb(128, 0, 128);
    private static readonly Color ColorFunction = Color.Blue;
    private static readonly Color ColorTag = Color.FromArgb(0, 170, 0);
    private static readonly Color ColorDefault = Color.Black;

    public LuaSyntaxHighlighter(RichTextBox richTextBox, Font? font = null)
    {
        _box = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));
        _font = font ?? new Font("Consolas", 10);
    }

    public void UpdateTags(IEnumerable<string> tagNames)
    {
        _tagNames = new List<string>(tagNames ?? Array.Empty<string>());
    }

    public void Highlight()
    {
        if (_box.IsDisposed || !_box.Visible) return;
        var text = _box.Text;
        if (string.IsNullOrEmpty(text))
            return;

        int caret = _box.SelectionStart;
        int len = _box.SelectionLength;
        _box.SuspendLayout();
        _box.SelectAll();
        _box.SelectionColor = ColorDefault;
        _box.SelectionFont = _font;
        _box.DeselectAll();

        int pos = 0;
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            int lineStart = pos;
            HighlightLine(line, lineStart);
            pos += line.Length + 1; // +1 for \n
        }

        _box.ResumeLayout(true);
        _box.Select(caret, len);
    }

    private void HighlightLine(string line, int lineStart)
    {
        // Multi-line comment (single line slice)
        foreach (Match m in MultiLineComment.Matches(line))
        {
            SetFormat(lineStart + m.Index, m.Length, ColorComment, bold: false);
        }

        // Single-line comment (takes precedence for rest of line in practice)
        var commentMatch = SingleLineComment.Match(line);
        if (commentMatch.Success)
        {
            SetFormat(lineStart + commentMatch.Index, commentMatch.Length, ColorComment, bold: false);
        }

        // Strings (avoid inside comments)
        foreach (Match m in DoubleQuotedString.Matches(line))
        {
            if (!IsInComment(line, m.Index)) SetFormat(lineStart + m.Index, m.Length, ColorString, false);
        }
        foreach (Match m in SingleQuotedString.Matches(line))
        {
            if (!IsInComment(line, m.Index)) SetFormat(lineStart + m.Index, m.Length, ColorString, false);
        }

        // Numbers
        foreach (Match m in Number.Matches(line))
        {
            if (!IsInComment(line, m.Index) && !IsInString(line, m.Index))
                SetFormat(lineStart + m.Index, m.Length, ColorNumber, false);
        }

        // Keywords (word boundaries)
        foreach (var kw in Keywords)
        {
            var rx = new Regex(@"\b" + Regex.Escape(kw) + @"\b", RegexOptions.Compiled);
            foreach (Match m in rx.Matches(line))
            {
                if (!IsInComment(line, m.Index) && !IsInString(line, m.Index))
                    SetFormat(lineStart + m.Index, m.Length, ColorKeyword, true);
            }
        }

        // Function calls (identifier followed by ()
        var keywordSet = new HashSet<string>(Keywords, StringComparer.Ordinal);
        foreach (Match m in FunctionCall.Matches(line))
        {
            if (!IsInComment(line, m.Index) && !IsInString(line, m.Index) && !keywordSet.Contains(m.Value))
                SetFormat(lineStart + m.Index, m.Length, ColorFunction, false);
        }

        // Tag names
        foreach (var tag in _tagNames)
        {
            if (string.IsNullOrEmpty(tag)) continue;
            var rx = new Regex(@"\b" + Regex.Escape(tag) + @"\b", RegexOptions.Compiled);
            foreach (Match m in rx.Matches(line))
            {
                if (!IsInComment(line, m.Index) && !IsInString(line, m.Index))
                    SetFormat(lineStart + m.Index, m.Length, ColorTag, true);
            }
        }
    }

    private static bool IsInComment(string line, int index)
    {
        int i = line.IndexOf("--", StringComparison.Ordinal);
        return i >= 0 && index >= i;
    }

    private static bool IsInString(string line, int index)
    {
        bool inDq = false, inSq = false;
        int i = 0;
        while (i < line.Length && i <= index)
        {
            if (inDq) { if (line[i] == '"' && (i == 0 || line[i - 1] != '\\')) inDq = false; i++; continue; }
            if (inSq) { if (line[i] == '\'' && (i == 0 || line[i - 1] != '\\')) inSq = false; i++; continue; }
            if (line[i] == '"') inDq = true;
            else if (line[i] == '\'') inSq = true;
            i++;
        }
        return inDq || inSq;
    }

    private void SetFormat(int start, int length, Color color, bool bold)
    {
        if (length <= 0) return;
        try
        {
            _box.Select(start, length);
            _box.SelectionColor = color;
            _box.SelectionFont = new Font(_font.FontFamily, _font.Size, bold ? FontStyle.Bold : FontStyle.Regular);
        }
        catch
        {
            // Ignore if position/length out of range during rapid edits
        }
    }
}
