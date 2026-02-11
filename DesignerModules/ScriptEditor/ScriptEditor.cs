using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Designer.Modules.ScriptEditor;

namespace Designer.Modules.ScriptEditor;

/// <summary>
/// Editor for Lua scripts with syntax highlighting.
/// </summary>
public partial class ScriptEditor : UserControl
{
    private TextBox _codeTextBox;
    private LuaScript? _script;
    private bool _modified;
    private List<string> _availableTags = new List<string>();

    public event EventHandler<bool>? ModifiedChanged;

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

        // Code editor (using TextBox for now, can be upgraded to ScintillaNET later)
        _codeTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new System.Drawing.Font("Consolas", 10),
            AcceptsTab = true,
            WordWrap = false
        };

        _codeTextBox.TextChanged += (s, e) =>
        {
            if (_script != null)
            {
                _modified = _codeTextBox.Text != _script.Code;
                ModifiedChanged?.Invoke(this, _modified);
            }
        };

        mainLayout.Controls.Add(_codeTextBox, 0, 0);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Status bar (optional)
        var statusLabel = new Label
        {
            Text = "Ready",
            Dock = DockStyle.Fill,
            Height = 20,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };
        mainLayout.Controls.Add(statusLabel, 0, 1);
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));

        Controls.Add(mainLayout);
    }

    /// <summary>
    /// Sets the script to edit.
    /// </summary>
    public void SetScript(LuaScript script)
    {
        _script = script;
        _codeTextBox.Text = script.Code;
        _modified = false;
    }

    /// <summary>
    /// Updates available tags for autocomplete.
    /// </summary>
    public void UpdateAvailableTags(List<string> tagNames)
    {
        _availableTags = tagNames ?? new List<string>();
        // TODO: Update autocomplete when ScintillaNET is integrated
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
    }
}
