using System.ComponentModel;
using AnycubicPhotonPCB.Core.Export;

namespace AnycubicPhotonPCB.Maker;

// The copy matrix of the Export tab: one check box per cell, a checked cell gets a copy of the board.
// Cells is a bit mask as in ExportOptions.CopyCells; at least one cell always stays checked
internal sealed class CopyMatrix : UserControl
{
    private const int N = ExportOptions.CopyGrid;

    private readonly CheckBox[] _cells = new CheckBox[N * N];
    private bool _loading;

    public CopyMatrix()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            ColumnCount = N,
            RowCount = N,
            Margin = Padding.Empty,
        };
        for (var i = 0; i < N; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / N));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / N));
        }

        for (var i = 0; i < _cells.Length; i++)
        {
            var cell = new CheckBox { Anchor = AnchorStyles.None, AutoSize = true, Checked = i == 0, UseVisualStyleBackColor = true };
            cell.CheckedChanged += CellChanged;
            _cells[i] = cell;
            grid.Controls.Add(cell, i % N, i / N);
        }

        Controls.Add(grid);
        Size = new Size(78, 62);
    }

    // Raised when the user changes a cell (not when Cells is set)
    public event EventHandler? CellsChanged;

    // Bit (row * CopyGrid + column) per checked cell; 0 is taken as the first cell only
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Cells
    {
        get => _cells.Select((c, i) => c.Checked ? 1 << i : 0).Sum();
        set
        {
            var cells = value & ((1 << _cells.Length) - 1);
            if (cells == 0) cells = 1;
            _loading = true;
            for (var i = 0; i < _cells.Length; i++) _cells[i].Checked = (cells & (1 << i)) != 0;
            _loading = false;
        }
    }

    // Unchecking the last checked cell is undone
    private void CellChanged(object? sender, EventArgs e)
    {
        if (_loading) return;
        if (_cells.All(c => !c.Checked) && sender is CheckBox cell)
        {
            cell.Checked = true;
            return;
        }

        CellsChanged?.Invoke(this, EventArgs.Empty);
    }
}
