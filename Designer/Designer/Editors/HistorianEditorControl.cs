using System.Windows.Forms;

namespace Designer.Editors;

/// <summary>Historian configurator: which tags to log, interval, retention.</summary>
public class HistorianEditorControl : UserControl
{
    private readonly DataGridView _grid;
    private readonly BindingSource _binding = new();
    private readonly List<HistorianTagEntry> _entries = new();

    public HistorianEditorControl()
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
        _binding.DataSource = _entries;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Tag", HeaderText = "Tag" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "IntervalSeconds", HeaderText = "Interval (s)" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RetentionDays", HeaderText = "Retention (days)" });

        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addBtn = new ToolStripButton("Add tag") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        addBtn.Click += (_, _) =>
        {
            _entries.Add(new HistorianTagEntry { Tag = "", IntervalSeconds = 60, RetentionDays = 30 });
            _binding.ResetBindings(false);
        };
        var delBtn = new ToolStripButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        delBtn.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is HistorianTagEntry e)
            {
                _entries.Remove(e);
                _binding.ResetBindings(false);
            }
        };
        toolbar.Items.Add(addBtn);
        toolbar.Items.Add(delBtn);
        Controls.Add(toolbar);
        Controls.Add(_grid);
    }

    private class HistorianTagEntry
    {
        public string Tag { get; set; } = "";
        public int IntervalSeconds { get; set; } = 60;
        public int RetentionDays { get; set; } = 30;
    }
}
