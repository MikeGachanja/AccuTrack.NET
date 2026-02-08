using System.Windows.Forms;

namespace Designer.Editors;

/// <summary>ML configurator: model configs, input/output tags, parameters.</summary>
public class MLConfiguratorControl : UserControl
{
    private readonly DataGridView _grid;
    private readonly PropertyGrid _propertyGrid;
    private readonly BindingSource _binding = new();
    private readonly List<MLModelEntry> _models = new();

    public MLConfiguratorControl()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            DataSource = _binding
        };
        _propertyGrid = new PropertyGrid { Dock = DockStyle.Fill };
        _grid.SelectionChanged += (s, _) => _propertyGrid.SelectedObject = _grid.CurrentRow?.DataBoundItem is MLModelEntry m ? m : null;
        _binding.DataSource = _models;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Model" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ModelPath", HeaderText = "Path" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InputTags", HeaderText = "Input tags" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OutputTags", HeaderText = "Output tags" });

        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addBtn = new ToolStripButton("Add model") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        addBtn.Click += (_, _) =>
        {
            _models.Add(new MLModelEntry { Name = "Model" + (_models.Count + 1), ModelPath = "", InputTags = "", OutputTags = "", IntervalSeconds = 60 });
            _binding.ResetBindings(false);
        };
        toolbar.Items.Add(addBtn);
        var delBtn = new ToolStripButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        delBtn.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is MLModelEntry m) { _models.Remove(m); _binding.ResetBindings(false); _propertyGrid.SelectedObject = null; }
        };
        toolbar.Items.Add(delBtn);

        const int panel1Min = 150;
        const int panel2Min = 180;
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };
        split.Panel1.Controls.Add(_grid);
        split.Panel2.Controls.Add(_propertyGrid);

        void ApplySplitLayout(object? sender, EventArgs e)
        {
            int w = split.Width;
            int minWidthNeeded = panel1Min + split.SplitterWidth + panel2Min;
            if (w < minWidthNeeded)
                return;
            split.Panel2MinSize = panel2Min;
            int maxDist = w - split.SplitterWidth - panel2Min;
            split.SplitterDistance = Math.Min(320, Math.Max(panel1Min, maxDist));
            split.Resize -= ApplySplitLayout;
        }
        split.Resize += ApplySplitLayout;

        Controls.Add(split);
        Controls.Add(toolbar);
    }

    private class MLModelEntry
    {
        public string Name { get; set; } = "";
        public string ModelPath { get; set; } = "";
        public string InputTags { get; set; } = "";
        public string OutputTags { get; set; } = "";
        public int IntervalSeconds { get; set; } = 60;
    }
}
