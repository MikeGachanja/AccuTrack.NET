using System.Windows.Forms;

namespace AccuTrack.TagEngine;

/// <summary>
/// Tag editor: grid/list to add/edit/delete tags; address format compatible with SDK (Modbus, OPC).
/// </summary>
public class TagEditorControl : UserControl
{
    private readonly DataGridView _grid;

    public TagEditorControl()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoGenerateColumns = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Name", DataPropertyName = "Name" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Address", HeaderText = "Address", DataPropertyName = "Address" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DataType", HeaderText = "Data Type", DataPropertyName = "DataType" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Scale", HeaderText = "Scale", DataPropertyName = "Scale" });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Offset", HeaderText = "Offset", DataPropertyName = "Offset" });
        Controls.Add(_grid);
    }

    public void Bind(TagTableModel? model)
    {
        if (model == null) { _grid.DataSource = null; return; }
        _grid.DataSource = model.Tags;
    }
}
