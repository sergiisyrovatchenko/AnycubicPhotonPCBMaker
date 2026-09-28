using AnycubicPhotonPCB.Core.Export;
using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;
using SkiaSharp;

namespace AnycubicPhotonPCB.Maker;

// Sketch of what a layer export puts on the printer screen, rendered at the size of the preview area.
// It uses the same placement, flips, inversion and extras as the real export (PhotonExporter / ScreenLayout),
// only scaled down and anti-aliased; the full resolution file is built when saving
internal static class ExportPreview
{
    // FitsTurned: the board (all copies) would fit if it were turned by 90° from its current orientation
    public sealed record Result(Bitmap Image, bool Fits, bool FitsTurned, int Copies);

    public static Result Render(PcbProject project, LayerExportRequest request, ExportOptions options, Size area)
    {
        var printer = options.Printer;
        var (screenW, screenH) = ScreenLayout.ScreenSize(printer);
        var scale = Math.Min(area.Width / (double)screenW, area.Height / (double)screenH);
        var width = Math.Max(1, (int)Math.Round(screenW * scale));
        var height = Math.Max(1, (int)Math.Round(screenH * scale));

        var (boardW, boardH) = ScreenLayout.BoardPixelSize(project.BoardBounds, printer.XyRes, options.Rotation);
        var (copies, fits) = ScreenLayout.PlaceCopies(boardW, boardH, options);
        var fitsTurned = ScreenLayout.PlaceCopies(boardH, boardW, options).Fits;

        // Each copy is rendered through its own transform; the copies never overlap, so their coverage is merged
        var flips = options.FlipsFor(request.Layer);
        var features = new Mask(width, height);
        var boards = new List<SKRect>();
        foreach (var (x, y) in copies)
        {
            var boardRect = SKRect.Create((float)(x * scale), (float)(y * scale), (float)(boardW * scale), (float)(boardH * scale));
            boards.Add(boardRect);
            var transform = LayerRasterizer.BoardToPixels(project.BoardBounds, boardRect, flips.Horizontal, flips.Vertical, options.Rotation);
            features.UnionWith(LayerOverlay.RenderLayer(project, request.Layer, request.Extras, transform, width, height, antialias: true));
        }

        return new Result(Compose(features, boards, request.Invert), fits, fitsTurned, copies.Count);
    }

    // Inside the boards: features dark on an exposed background, or exposed when inverted.
    // Outside the boards the whole screen is exposed, or dark when inverted (as in ScreenLayout.ComposeScreen)
    private static Bitmap Compose(Mask features, List<SKRect> boards, bool invert)
    {
        int width = features.Width, height = features.Height;
        var inside = new bool[width * height];
        foreach (var board in boards)
        {
            var r = ImageConvert.PixelBounds(board, width, height);
            for (var y = r.Top; y < r.Bottom; y++) Array.Fill(inside, true, y * width + r.Left, r.Width);
        }

        var outside = ImageConvert.Grey(invert ? 0 : 255);
        return ImageConvert.ToBitmap(width, height, (y, row) =>
        {
            var offset = y * width;
            for (var x = 0; x < width; x++)
            {
                int v = features.Data[offset + x];
                row[x] = inside[offset + x] ? ImageConvert.Grey(invert ? v : 255 - v) : outside;
            }
        });
    }
}
