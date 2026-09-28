using AnycubicPhotonPCB.Core.Rendering;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Export;

// Where the board and its copies go on the printer screen, and the conversion of the screen frame to the printer's
// native pixel order. Used by the exported files and the export dialog preview, so both place the boards identically
public static class ScreenLayout
{
    // Printer screen as seen from above with the front at the bottom (landscape), in pixels
    public static (int Width, int Height) ScreenSize(PrinterModel printer) =>
        (Math.Max(printer.ResolutionX, printer.ResolutionY), Math.Min(printer.ResolutionX, printer.ResolutionY));

    // Board size in printer pixels; width and height swap places when the board is turned
    public static (int Width, int Height) BoardPixelSize(SKRect board, double pixelSize, BoardRotation rotation = BoardRotation.None)
    {
        var width = Math.Max(1, (int)Math.Round(board.Width / pixelSize));
        var height = Math.Max(1, (int)Math.Round(board.Height / pixelSize));
        return rotation == BoardRotation.None ? (width, height) : (height, width);
    }

    // Top-left corner of a block of the given size on the landscape screen frame according to the anchor settings
    public static (int X, int Y, bool Fits) PlaceBoard(int boardWidth, int boardHeight, ExportOptions options)
    {
        var printer = options.Printer;
        var (screenW, screenH) = ScreenSize(printer);
        var ox = (int)Math.Round(options.OffsetXmm / printer.XyRes);
        var oy = (int)Math.Round(options.OffsetYmm / printer.XyRes);
        var (x, y) = options.Anchor switch
        {
            AnchorCorner.TopLeft => (ox, oy),
            AnchorCorner.TopRight => (screenW - ox - boardWidth, oy),
            AnchorCorner.BottomLeft => (ox, screenH - oy - boardHeight),
            AnchorCorner.BottomRight => (screenW - ox - boardWidth, screenH - oy - boardHeight),
            _ => ((int)Math.Round(screenW / 2.0 + ox - boardWidth / 2.0), (int)Math.Round(screenH / 2.0 + oy - boardHeight / 2.0)),
        };

        var fits = x >= 0 && y >= 0 && x + boardWidth <= screenW && y + boardHeight <= screenH;
        return (x, y, fits);
    }

    // Top-left corners of all copies of the board on the landscape screen frame. The used cells of the copy matrix
    // form a block (copies CopySpacingMm apart) that is placed like a single board by the anchor settings; empty
    // cells inside the block stay empty. Fits: the whole block is on the screen
    public static (List<(int X, int Y)> Copies, bool Fits) PlaceCopies(int boardWidth, int boardHeight, ExportOptions options)
    {
        const int n = ExportOptions.CopyGrid;
        var cells = Enumerable.Range(0, n * n).Where(i => (options.CopyCells & (1 << i)) != 0).Select(i => (Row: i / n, Col: i % n)).ToList();
        if (cells.Count == 0) cells.Add((0, 0));

        int minRow = cells.Min(c => c.Row), maxRow = cells.Max(c => c.Row);
        int minCol = cells.Min(c => c.Col), maxCol = cells.Max(c => c.Col);
        var gap = (int)Math.Round(Math.Max(0, options.CopySpacingMm) / options.Printer.XyRes);
        var blockWidth = (maxCol - minCol + 1) * (boardWidth + gap) - gap;
        var blockHeight = (maxRow - minRow + 1) * (boardHeight + gap) - gap;

        var (x, y, fits) = PlaceBoard(blockWidth, blockHeight, options);
        var copies = cells.Select(c => (x + (c.Col - minCol) * (boardWidth + gap), y + (c.Row - minRow) * (boardHeight + gap))).ToList();
        return (copies, fits);
    }

    // Places the board mask and its copies on a landscape screen frame according to the anchor and copy settings.
    // Outside the boards the screen is exposed, or dark when the features are exposed (invert)
    public static Mask ComposeScreen(Mask board, bool invert, ExportOptions options, out bool fits)
    {
        var (screenW, screenH) = ScreenSize(options.Printer);
        var screen = new Mask(screenW, screenH);
        if (!invert) Array.Fill(screen.Data, (byte)255);

        (var copies, fits) = PlaceCopies(board.Width, board.Height, options);
        foreach (var (x, y) in copies)
        {
            var x0 = Math.Max(0, x);
            var x1 = Math.Min(screenW, x + board.Width);
            if (x1 <= x0) continue;
            for (var row = Math.Max(0, y); row < Math.Min(screenH, y + board.Height); row++)
            {
                var src = board.Data.AsSpan((row - y) * board.Width + (x0 - x), x1 - x0);
                src.CopyTo(screen.Data.AsSpan(row * screenW + x0, x1 - x0));
            }
        }

        return screen;
    }

    // Converts the landscape screen frame into the printer's native pixel order (rotation handling)
    public static byte[] ToNativeFrame(Mask view, PrinterModel printer)
    {
        int resX = printer.ResolutionX, resY = printer.ResolutionY;
        var portrait = resX < resY;

        if (!portrait)
        {
            var copy = (byte[])view.Data.Clone();
            // 180° rotation of a landscape frame is a full reversal (vectorised)
            if (printer.Rotate180) Array.Reverse(copy);
            return copy;
        }

        var native = new byte[resX * resY];

        // Portrait screens: the landscape view is rotated by 90° (see render-to-photon.ts).
        for (var vy = 0; vy < view.Height; vy++)
        {
            var col = resX - 1 - vy;
            if (printer.Rotate180) col = resX - 1 - col;
            for (var vx = 0; vx < view.Width; vx++)
            {
                var row = printer.Rotate180 ? resY - 1 - vx : vx;
                native[row * resX + col] = view.Data[vy * view.Width + vx];
            }
        }

        return native;
    }
}
