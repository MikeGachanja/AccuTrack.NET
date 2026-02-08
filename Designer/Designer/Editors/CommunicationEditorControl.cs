using System.Windows.Forms;

namespace Designer.Editors;

/// <summary>Communication editor: list of connections (Modbus/OPC/S7), add/edit/delete, property grid for selected.</summary>
public class CommunicationEditorControl : UserControl
{
    private readonly DataGridView _grid;
    private readonly PropertyGrid _propertyGrid;
    private readonly BindingSource _binding = new();
    private readonly List<CommConnectionItem> _connections = new();

    public CommunicationEditorControl(string _)
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
        _grid.SelectionChanged += (s, _) => SyncPropertyGrid();
        _propertyGrid = new PropertyGrid { Dock = DockStyle.Fill };
        var toolbar = new ToolStrip { Dock = DockStyle.Top };
        var addBtn = new ToolStripButton("Add connection") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        addBtn.Click += (_, _) =>
        {
            var name = "Connection" + (_connections.Count + 1);
            _connections.Add(new CommConnectionItem { Name = name, Type = "Modbus", Host = "localhost", Port = 502 });
            _binding.ResetBindings(false);
            SyncPropertyGrid();
        };
        var delBtn = new ToolStripButton("Delete") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        delBtn.Click += (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is CommConnectionItem item)
            {
                _connections.Remove(item);
                _binding.ResetBindings(false);
                _propertyGrid.SelectedObject = null;
            }
        };
        toolbar.Items.Add(addBtn);
        toolbar.Items.Add(delBtn);
        _binding.DataSource = _connections;

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

        void ApplySplitLayout()
        {
            int w = split.Width;
            int minWidthNeeded = panel1Min + split.SplitterWidth + panel2Min;
            if (w < minWidthNeeded)
                return;
            split.Panel2MinSize = panel2Min;
            int maxDist = w - split.SplitterWidth - panel2Min;
            split.SplitterDistance = Math.Min(280, Math.Max(panel1Min, maxDist));
            split.Resize -= ApplySplitLayout;
        }
        split.Resize += ApplySplitLayout;

        Controls.Add(split);
        Controls.Add(toolbar);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Name", ReadOnly = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Type", HeaderText = "Type", ReadOnly = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Host", HeaderText = "Host", ReadOnly = false });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Port", HeaderText = "Port", ReadOnly = false });
    }

    private void SyncPropertyGrid()
    {
        _propertyGrid.SelectedObject = _grid.CurrentRow?.DataBoundItem is CommConnectionItem item ? item : null;
    }

    private class CommConnectionItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Modbus";
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 502;
        public byte SlaveId { get; set; } = 1;
    }
}
