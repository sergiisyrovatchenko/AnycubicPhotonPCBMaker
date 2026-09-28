using System.ComponentModel;
using System.Windows.Forms.VisualStyles;
using AnycubicPhotonPCB.Core.Export;
using AnycubicPhotonPCB.Core.Pcb;

namespace AnycubicPhotonPCB.Maker;

// List of the layers on the Export tab: the check box marks the layers written by Save, the selected layer is
// previewed, the Flip columns show and toggle the flips of the layer's board side. The columns come from the
// designer: file, layer, Flip horizontal, Flip vertical.
// The list never ends up without a selection, and layers unchecked once stay unchecked when the list is rebuilt
internal sealed class LayerExportList : ListView
{
    private const int FlipHorizontalColumn = 2;
    private const int FlipVerticalColumn = 3;

    // Layer files unchecked by the user, kept by file name across SetLayers
    private readonly HashSet<string> _unchecked = new(StringComparer.OrdinalIgnoreCase);

    private List<PcbLayer> _layers = [];
    private SideFlips _topFlips;
    private SideFlips _bottomFlips;
    private int _selectedIndex = -1;
    private bool _loading;

    // The current mouse press is on a Flip column: a double click there must not toggle the export check box
    private bool _flipClick;

    public LayerExportList()
    {
        OwnerDraw = true;
    }

    public event EventHandler? SelectedLayerChanged;

    public event EventHandler? CheckedLayersChanged;

    // A Flip cell was clicked; TopFlips / BottomFlips hold the new state
    public event EventHandler? FlipsChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SideFlips TopFlips
    {
        get => _topFlips;
        set
        {
            _topFlips = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SideFlips BottomFlips
    {
        get => _bottomFlips;
        set
        {
            _bottomFlips = value;
            Invalidate();
        }
    }

    public PcbLayer? SelectedLayer => _selectedIndex >= 0 && _selectedIndex < _layers.Count ? _layers[_selectedIndex] : null;

    public List<PcbLayer> CheckedLayers => CheckedItems.Cast<ListViewItem>().Select(i => (PcbLayer)i.Tag!).ToList();

    // Rebuilds the list; the selection and the unchecked layers are kept by file name. Raises no events
    public void SetLayers(IReadOnlyList<PcbLayer> layers)
    {
        var selected = SelectedLayer?.FileName;
        _layers = layers.ToList();

        _loading = true;
        BeginUpdate();
        Items.Clear();
        foreach (var layer in _layers)
        {
            var item = new ListViewItem(layer.FileName)
            {
                Checked = !_unchecked.Contains(layer.FileName),
                Tag = layer,
                ForeColor = SystemColors.WindowText,
            };
            item.SubItems.Add(layer.DisplayName);
            item.SubItems.Add("");
            item.SubItems.Add("");
            Items.Add(item);
        }

        EndUpdate();
        var index = _layers.FindIndex(l => string.Equals(l.FileName, selected, StringComparison.OrdinalIgnoreCase));
        SelectLayer(index >= 0 ? index : _layers.Count > 0 ? 0 : -1);
        _loading = false;
    }

    private void SelectLayer(int index)
    {
        _selectedIndex = index;
        if (index < 0) return;
        var item = Items[index];
        item.Selected = true;
        item.Focused = true;
        item.EnsureVisible();
    }

    private SideFlips FlipsOf(PcbLayer layer) => layer.IsTopSide ? _topFlips : _bottomFlips;

    // Clicking another row first deselects the old one (no selection for a moment), then selects the new one.
    // The restore is posted, so it only happens when nothing is selected once the click has been handled
    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        if (_loading) return;
        if (SelectedIndices.Count == 0)
        {
            BeginInvoke(() =>
            {
                if (SelectedIndices.Count == 0 && _selectedIndex >= 0 && _selectedIndex < Items.Count)
                    SelectLayer(_selectedIndex);
            });
            return;
        }

        if (SelectedIndices[0] == _selectedIndex) return;
        _selectedIndex = SelectedIndices[0];
        SelectedLayerChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnItemCheck(ItemCheckEventArgs e)
    {
        if (_flipClick) e.NewValue = e.CurrentValue;
        base.OnItemCheck(e);
    }

    // The check box marks the layers written by Save; it does not change the selection
    protected override void OnItemChecked(ItemCheckedEventArgs e)
    {
        base.OnItemChecked(e);
        if (_loading) return;
        var fileName = ((PcbLayer)e.Item.Tag!).FileName;
        if (e.Item.Checked) _unchecked.Remove(fileName);
        else _unchecked.Add(fileName);
        CheckedLayersChanged?.Invoke(this, EventArgs.Empty);
    }

    // The flips belong to the board side, so all layers of that side change together
    protected override void OnMouseDown(MouseEventArgs e)
    {
        _flipClick = false;
        var hit = e.Button == MouseButtons.Left ? HitTest(e.Location) : null;
        if (hit is { Item.Tag: PcbLayer layer, SubItem: { } subItem } &&
            hit.Item.SubItems.IndexOf(subItem) is var column and (FlipHorizontalColumn or FlipVerticalColumn))
        {
            _flipClick = true;
            var flips = FlipsOf(layer);
            flips = column == FlipHorizontalColumn ? flips with { Horizontal = !flips.Horizontal } : flips with { Vertical = !flips.Vertical };
            if (layer.IsTopSide) _topFlips = flips;
            else _bottomFlips = flips;
            Invalidate();
            FlipsChanged?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _flipClick = false;
        base.OnMouseUp(e);
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        e.DrawDefault = true;
        base.OnDrawColumnHeader(e);
    }

    // All columns are drawn here so the selection looks the same across the row: the export check box and the file
    // name, the layer name, and the flip check boxes of the layer's side
    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        if (e.Item is not { Tag: PcbLayer layer } item || e.SubItem == null) return;

        // For the first column the reported bounds span the whole row
        var bounds = e.ColumnIndex == 0 ? new Rectangle(e.Bounds.Left, e.Bounds.Top, Columns[0].Width, e.Bounds.Height) : e.Bounds;
        var selected = item.Selected;
        using (var back = new SolidBrush(selected ? SystemColors.Highlight : BackColor))
            e.Graphics.FillRectangle(back, bounds);
        var fore = selected ? SystemColors.HighlightText : SystemColors.WindowText;
        const TextFormatFlags textFlags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        var glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, CheckBoxState.UncheckedNormal);

        switch (e.ColumnIndex)
        {
            case 0:
            {
                var box = new Point(bounds.Left + 4, bounds.Top + (bounds.Height - glyph.Height) / 2);
                CheckBoxRenderer.DrawCheckBox(e.Graphics, box, item.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);
                var text = Rectangle.FromLTRB(box.X + glyph.Width + 4, bounds.Top, bounds.Right, bounds.Bottom);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Font, text, fore, textFlags);
                break;
            }
            case FlipHorizontalColumn or FlipVerticalColumn:
            {
                var box = new Point(bounds.Left + (bounds.Width - glyph.Width) / 2, bounds.Top + (bounds.Height - glyph.Height) / 2);
                var flips = FlipsOf(layer);
                var on = e.ColumnIndex == FlipHorizontalColumn ? flips.Horizontal : flips.Vertical;
                CheckBoxRenderer.DrawCheckBox(e.Graphics, box, on ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);
                break;
            }
            default:
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, Font, Rectangle.Inflate(bounds, -2, 0), fore, textFlags);
                break;
        }
    }
}
