using System.Windows.Forms;

namespace AccuTrack.Components;

/// <summary>
/// Component palette: list of component types to drag onto the screen canvas.
/// </summary>
public class ComponentsView : UserControl
{
    private const string DragDataFormat = "AccuTrack.ComponentType";

    public ComponentsView()
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.List,
            FullRowSelect = true,
            BorderStyle = BorderStyle.None
        };
        list.MouseDown += OnMouseDown;
        foreach (var def in ComponentTypes.All)
            list.Items.Add(new ListViewItem(def.Type) { Tag = def });
        Controls.Add(list);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (sender is not ListView list || e.Button != MouseButtons.Left) return;
        var item = list.GetItemAt(e.X, e.Y);
        if (item?.Tag is not ComponentTypeDefinition def) return;
        list.DoDragDrop(def.Type, DragDropEffects.Copy);
    }

}
