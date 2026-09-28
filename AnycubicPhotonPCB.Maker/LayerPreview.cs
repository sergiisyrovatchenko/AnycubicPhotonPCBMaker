using AnycubicPhotonPCB.Core.Gerber;
using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;
using SkiaSharp;

namespace AnycubicPhotonPCB.Maker;

// Layer card previews rendered at the size they are shown, so the picture box never has to scale them.
// The rasterised layer is kept per size: changing the photoresist or the extras only recomposes it
internal static class LayerPreview
{
    // Border around the board inside the preview, in pixels. It shows what surrounds the board on the printer
    // screen (as in ScreenLayout.ComposeScreen): exposed (white) when the background is exposed, i.e. positive
    // photoresist on copper, dark when the features are exposed (negative photoresist)
    public const int Margin = 1;

    // Largest size with the board's aspect ratio (plus margins) that fits into area
    public static Size FitSize(Size area, SKRect board)
    {
        var w = area.Width - 2 * Margin;
        var h = area.Height - 2 * Margin;
        if (w <= 0 || h <= 0 || board.Width <= 0 || board.Height <= 0) return Size.Empty;
        var scale = Math.Min(w / board.Width, h / board.Height);
        return new Size(
            Math.Max(1, (int)Math.Round(board.Width * scale)) + 2 * Margin,
            Math.Max(1, (int)Math.Round(board.Height * scale)) + 2 * Margin);
    }

    // mirror: horizontal mirror, used for bottom layers so they read as seen from below, like the PCB tab
    public static SKMatrix Transform(SKRect board, Size size, bool mirror = false) =>
        LayerRasterizer.BoardToPixelsFit(board, size.Width, size.Height, Margin, mirror, false);

    // Anti-aliased coverage of one layer (255 = feature)
    public static Mask Rasterize(GerberImage image, SKRect board, Size size, bool mirror) =>
        LayerRasterizer.Render(image, Transform(board, size, mirror), size.Width, size.Height, antialias: true);

    // Final preview in one pass: either features exposed (inverted) or an exposed board area with dark
    // features, the same as the exported mask looks
    public static Bitmap Compose(Mask features, bool invert, SKRect board, Size size)
    {
        var area = ImageConvert.PixelBounds(Transform(board, size).MapRect(board), size.Width, size.Height);
        var src = features.Data;
        var border = ImageConvert.Grey(invert ? 0 : 255);
        return ImageConvert.ToBitmap(size.Width, size.Height, (y, row) =>
        {
            var insideRow = y >= area.Top && y < area.Bottom;
            var offset = y * size.Width;
            for (var x = 0; x < row.Length; x++)
            {
                if (!insideRow || x < area.Left || x >= area.Right)
                {
                    row[x] = border;
                    continue;
                }

                int v = src[offset + x];
                row[x] = ImageConvert.Grey(invert ? v : 255 - v);
            }
        });
    }
}

// Drill holes, drill marks and board outline of a project, rasterised once per preview size and set of extras
// and shared by all cards
internal sealed class OverlayCache(PcbProject project)
{
    private readonly Dictionary<(Size, LayerExtras, bool), LayerOverlay> _overlays = new();
    private readonly object _lock = new();

    // mirror: the overlay for mirrored (bottom) cards
    public LayerOverlay Get(Size size, LayerExtras extras, bool mirror)
    {
        var key = (size, extras, mirror);
        lock (_lock)
        {
            if (_overlays.TryGetValue(key, out var cached)) return cached;
        }

        var overlay = LayerOverlay.Render(project, extras, LayerPreview.Transform(project.BoardBounds, size, mirror), size.Width, size.Height, antialias: true);

        lock (_lock)
        {
            // Card sizes change while the window is resized; keep only a few recent ones
            if (_overlays.Count >= 16) _overlays.Clear();
            _overlays[key] = overlay;
        }

        return overlay;
    }
}
