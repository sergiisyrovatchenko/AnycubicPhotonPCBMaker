using System.ComponentModel;

namespace AnycubicPhotonPCB.Maker;

// Lays out layer cards in the number of columns that makes the board previews as large as possible
// for the current size and the board's aspect ratio (wide boards stack vertically, tall boards side by side)
[DesignerCategory("Code")]
internal sealed class CardGrid : Panel
{
    private readonly double _boardAspect; // width / height

    public CardGrid(double boardAspect)
    {
        _boardAspect = boardAspect > 0 ? boardAspect : 1;
        Padding = new Padding(0);
    }

    // Space a card needs besides its picture (header, footer, margins) in logical pixels
    private const int ChromeWidth = 24;
    private const int ChromeHeight = 84;

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        var cards = Controls.Cast<Control>().Where(c => c.Visible).ToList();
        if (cards.Count == 0) return;

        var area = DisplayRectangle;
        area = new Rectangle(area.X + Padding.Left, area.Y + Padding.Top, area.Width - Padding.Horizontal, area.Height - Padding.Vertical);
        if (area.Width <= 0 || area.Height <= 0) return;

        var chromeW = LogicalToDeviceUnits(ChromeWidth);
        var chromeH = LogicalToDeviceUnits(ChromeHeight);

        var bestColumns = 1;
        var bestScale = 0.0;
        for (var columns = 1; columns <= cards.Count; columns++)
        {
            var rows = (cards.Count + columns - 1) / columns;
            var pictureW = area.Width / (double)columns - chromeW;
            var pictureH = area.Height / (double)rows - chromeH;
            if (pictureW <= 0 || pictureH <= 0) continue;
            var scale = Math.Min(pictureW / _boardAspect, pictureH); // board height in pixels
            if (scale > bestScale)
            {
                bestScale = scale;
                bestColumns = columns;
            }
        }

        var rowsCount = (cards.Count + bestColumns - 1) / bestColumns;
        var cellW = area.Width / bestColumns;
        var cellH = area.Height / rowsCount;
        for (var i = 0; i < cards.Count; i++)
        {
            var col = i % bestColumns;
            var row = i / bestColumns;
            var m = cards[i].Margin;
            cards[i].Bounds = new Rectangle(
                area.X + col * cellW + m.Left,
                area.Y + row * cellH + m.Top,
                cellW - m.Horizontal,
                cellH - m.Vertical);
        }
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        PerformLayout();
    }
}
