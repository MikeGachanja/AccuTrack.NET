using System.Windows.Forms;

namespace Designer.Editors;

/// <summary>Schedules editor: list of schedule entries (cron-like or time-based).</summary>
public class SchedulerEditorControl : UserControl
{
    private readonly DataGridView _grid;
    private readonly BindingSource _binding = new();
    private readonly List<ScheduleEntry> _schedules = new();

    public SchedulerEditorControl()
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
        _binding.DataSource = _schedules;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Name" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Schedule", HeaderText = "Schedule (cron/time)" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Action", HeaderText = "Action" });

        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addBtn = new ToolStripButton("Add") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        addBtn.Click += (_, _) =>
        {
            _schedules.Add(new ScheduleEntry { Name = "Schedule" + (_schedules.Count + 1), Schedule = "0 * * * *", Action = "" });
            _binding.ResetBindings(false);
        };
        var delBtn = new ToolStripButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        delBtn.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is ScheduleEntry e)
            {
                _schedules.Remove(e);
                _binding.ResetBindings(false);
            }
        };
        toolbar.Items.Add(addBtn);
        toolbar.Items.Add(delBtn);
        Controls.Add(toolbar);
        Controls.Add(_grid);
    }

    private class ScheduleEntry
    {
        public string Name { get; set; } = "";
        public string Schedule { get; set; } = "0 * * * *";
        public string Action { get; set; } = "";
    }
}
