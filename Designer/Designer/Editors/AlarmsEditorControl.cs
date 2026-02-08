using System.Windows.Forms;

namespace Designer.Editors;

/// <summary>Alarms editor: list of alarm definitions (tag, condition, priority, message).</summary>
public class AlarmsEditorControl : UserControl
{
    private readonly DataGridView _grid;
    private readonly BindingSource _binding = new();
    private readonly List<AlarmEntry> _alarms = new();

    public AlarmsEditorControl()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            DataSource = _binding
        };
        _binding.DataSource = _alarms;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tag", HeaderText = "Tag" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Condition", HeaderText = "Condition" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Priority", HeaderText = "Priority" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Message", HeaderText = "Message" });

        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addBtn = new ToolStripButton("Add") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        addBtn.Click += (_, _) =>
        {
            _alarms.Add(new AlarmEntry { Tag = "", Condition = ">", Priority = "High", Message = "" });
            _binding.ResetBindings(false);
        };
        var delBtn = new ToolStripButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        delBtn.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is AlarmEntry e)
            {
                _alarms.Remove(e);
                _binding.ResetBindings(false);
            }
        };
        toolbar.Items.Add(addBtn);
        toolbar.Items.Add(delBtn);
        Controls.Add(toolbar);
        Controls.Add(_grid);
    }

    private class AlarmEntry
    {
        public string Tag { get; set; } = "";
        public string Condition { get; set; } = ">";
        public string Priority { get; set; } = "High";
        public string Message { get; set; } = "";
    }
}
