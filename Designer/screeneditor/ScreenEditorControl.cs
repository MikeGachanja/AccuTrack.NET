using System.Windows.Forms;

namespace AccuTrack.ScreenEditor;

/// <summary>
/// Screen editor canvas: drag-drop components, select, move. Fires SelectionChanged for PropertyGrid.
/// </summary>
public class ScreenEditorControl : UserControl
{
    private ScreenTemplate? _model;
    private ComponentInstance? _selected;
    private Point _dragStart;
    private bool _dragging;

    private static readonly Dictionary<string, (int w, int h)> DefaultSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Button"] = (100, 32), ["Indicator"] = (40, 40), ["Tank"] = (80, 120), ["Gauge"] = (120, 120),
        ["Motor"] = (60, 60), ["Pump"] = (60, 60), ["TrendView"] = (300, 200), ["AlarmView"] = (280, 150),
        ["TextLabel"] = (120, 24), ["Numeric"] = (80, 24), ["CheckBox"] = (120, 24), ["ProgressBar"] = (200, 24),
        ["Slider"] = (200, 32), ["Rectangle"] = (100, 80), ["Line"] = (100, 4), ["Image"] = (64, 64),
    };

    public event EventHandler<ComponentInstance?>? SelectionChanged;

    public ScreenEditorControl()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && _selected != null && _model != null)
        {
            _model.Components.Remove(_selected);
            SelectedComponent = null;
            Invalidate();
        }
    }

    public ScreenTemplate? Model => _model;

    public void Bind(ScreenTemplate? model)
    {
        _model = model;
        _selected = null;
        if (model != null)
        {
            Width = Math.Max(400, model.Width);
            Height = Math.Max(300, model.Height);
        }
        Invalidate();
    }

    public ComponentInstance? SelectedComponent
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Invalidate();
            SelectionChanged?.Invoke(this, _selected);
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.Text) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (_model == null || e.Data?.GetData(DataFormats.Text) is not string typeName) return;
        var pt = PointToClient(new Point(e.X, e.Y));
        var (w, h) = DefaultSizes.TryGetValue(typeName, out var s) ? s : (80, 40);
        var comp = new ComponentInstance
        {
            Type = typeName,
            X = pt.X,
            Y = pt.Y,
            Width = w,
            Height = h
        };
        _model.Components.Add(comp);
        SelectedComponent = comp;
        Invalidate();
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (_model == null) return;
        var pt = new PointF(e.X, e.Y);
        ComponentInstance? hit = null;
        for (var i = _model.Components.Count - 1; i >= 0; i--)
        {
            var c = _model.Components[i];
            if (pt.X >= c.X && pt.X <= c.X + c.Width && pt.Y >= c.Y && pt.Y <= c.Y + c.Height)
            { hit = c; break; }
        }
        SelectedComponent = hit;
        if (hit != null && e.Button == MouseButtons.Left)
        {
            _dragStart = new Point((int)(pt.X - hit.X), (int)(pt.Y - hit.Y));
            _dragging = true;
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging || _model == null || _selected == null) return;
        _selected.X = e.X - _dragStart.X;
        _selected.Y = e.Y - _dragStart.Y;
        if (_selected.X < 0) _selected.X = 0;
        if (_selected.Y < 0) _selected.Y = 0;
        Invalidate();
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) _dragging = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        if (_model == null) return;
        g.DrawString($"Screen: {_model.Name} ({_model.Width}x{_model.Height})", Font, Brushes.Gray, 4, 4);
        foreach (var c in _model.Components)
        {
            var rect = new RectangleF((float)c.X, (float)c.Y, (float)c.Width, (float)c.Height);
            var isSel = c == _selected;
            g.FillRectangle(isSel ? new SolidBrush(Color.FromArgb(80, 120, 200)) : Brushes.LightGray, rect);
            g.DrawRectangle(isSel ? Pens.Blue : Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
            g.DrawString(c.Type, Font, Brushes.Black, rect.X + 2, rect.Y + Math.Max(0, (rect.Height - Font.Height) / 2));
        }
    }
}
