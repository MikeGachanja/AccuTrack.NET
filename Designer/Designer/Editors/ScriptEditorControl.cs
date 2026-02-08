using System.Windows.Forms;

namespace Designer.Editors;

/// <summary>Script editor: Lua (or chosen language) with toolbar.</summary>
public class ScriptEditorControl : UserControl
{
    private readonly TextBox _textBox;
    public string ScriptName { get; }

    public ScriptEditorControl(string name)
    {
        ScriptName = name;
        _textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            Font = new Font("Consolas", 10f),
            AcceptsTab = true,
            WordWrap = false
        };
        _textBox.Text = $"-- {name}\r\n-- Edit script below.\r\n\r\n";
        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        toolbar.Items.Add(new ToolStripLabel($"Script: {name}") { DisplayStyle = ToolStripItemDisplayStyle.Text });
        toolbar.Items.Add(new ToolStripSeparator());
        var runBtn = new ToolStripButton("Run") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        runBtn.Click += (_, _) => { /* TODO: run script */ };
        toolbar.Items.Add(runBtn);
        Controls.Add(toolbar);
        Controls.Add(_textBox);
    }

    public string ScriptText { get => _textBox.Text; set => _textBox.Text = value ?? ""; }
}
