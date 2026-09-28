using AnycubicPhotonPCB.Core.Pcb;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Rendering;

// Renders a realistic looking top / bottom view of the board (similar to pcb-stackup)
public static class BoardPreviewRenderer
{
    private static readonly SKColor Fr4 = SKColor.Parse("#916b55");
    private static readonly SKColor Copper = SKColor.Parse("#cc9933");
    private static readonly SKColor CopperUnderMask = SKColor.Parse("#4f7a2a");
    private static readonly SKColor Soldermask = SKColor.Parse("#004200");
    private static readonly SKColor Silkscreen = SKColors.White;
    private static readonly SKColor Paste = SKColor.Parse("#999999");

    // Returns an RGBA bitmap (caller disposes). Bottom views are mirrored horizontally
    public static SKBitmap Render(PcbProject project, LayerSide side, int maxWidth, int maxHeight)
    {
        var bounds = project.BoardBounds;
        var scale = Math.Min(maxWidth / bounds.Width, maxHeight / bounds.Height);
        var width = Math.Max(1, (int)Math.Round(bounds.Width * scale));
        var height = Math.Max(1, (int)Math.Round(bounds.Height * scale));
        var mirror = side == LayerSide.Bottom;
        var transform = LayerRasterizer.BoardToPixels(bounds, SKRect.Create(0, 0, width, height), mirror, false);

        // All the given layers merged into one mask; null when there are none
        Mask? Union(IEnumerable<PcbLayer> layers)
        {
            Mask? result = null;
            foreach (var layer in layers)
            {
                var m = LayerRasterizer.Render(layer.Image, transform, width, height, antialias: true);
                if (result == null) result = m;
                else result.UnionWith(m);
            }

            return result;
        }

        Mask? Layer(LayerType type) => Union(project.Layers.Where(l => l.Type == type && l.Side == side));

        var boardShape = BoardShape(project, transform, width, height);
        var copper = Layer(LayerType.Copper);
        var maskOpenings = Layer(LayerType.Soldermask);
        var silk = Layer(LayerType.Silkscreen);
        var paste = Layer(LayerType.Solderpaste);
        var drills = Union(project.DrillLayers);

        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        var pixels = bitmap.GetPixelSpan();
        for (var i = 0; i < width * height; i++)
        {
            float r = 0, g = 0, b = 0;
            var boardA = boardShape.Data[i] / 255f;
            Blend(ref r, ref g, ref b, Fr4, 1);

            var cu = copper != null ? copper.Data[i] / 255f : 0;
            if (maskOpenings != null)
            {
                var open = maskOpenings.Data[i] / 255f;
                Blend(ref r, ref g, ref b, Copper, cu * open);
                Blend(ref r, ref g, ref b, CopperUnderMask, cu * (1 - open));
                Blend(ref r, ref g, ref b, Soldermask, 0.75f * (1 - open) * (1 - cu));
            }
            else
            {
                Blend(ref r, ref g, ref b, Copper, cu);
            }

            if (paste != null) Blend(ref r, ref g, ref b, Paste, paste.Data[i] / 255f * 0.8f);
            if (silk != null) Blend(ref r, ref g, ref b, Silkscreen, silk.Data[i] / 255f);

            var alpha = boardA * (drills != null ? 1 - drills.Data[i] / 255f : 1);
            pixels[i * 4] = (byte)(r * 255);
            pixels[i * 4 + 1] = (byte)(g * 255);
            pixels[i * 4 + 2] = (byte)(b * 255);
            pixels[i * 4 + 3] = (byte)(alpha * 255);
        }

        return bitmap;
    }

    private static void Blend(ref float r, ref float g, ref float b, SKColor c, float a)
    {
        if (a <= 0) return;
        r += (c.Red / 255f - r) * a;
        g += (c.Green / 255f - g) * a;
        b += (c.Blue / 255f - b) * a;
    }

    // Board silhouette: the area enclosed by the outline layer (flood fill from the border), or the full
    // board rectangle if there is no outline or it is not closed
    public static Mask BoardShape(PcbProject project, SKMatrix transform, int width, int height)
    {
        var full = new Mask(width, height);
        Array.Fill(full.Data, (byte)255);
        if (project.Outline is not { } outline) return full;

        // Render with a 1 pixel margin so the flood fill can walk around the outline.
        var w = width + 2;
        var h = height + 2;
        var shifted = transform.PostConcat(SKMatrix.CreateTranslation(1, 1));
        var lines = LayerRasterizer.Render(outline.Image, shifted, w, h, antialias: false);

        var outside = new bool[w * h];
        var stack = new Stack<int>();
        stack.Push(0);
        outside[0] = true;
        while (stack.Count > 0)
        {
            var p = stack.Pop();
            var x = p % w;
            var y = p / w;
            void Visit(int nx, int ny)
            {
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) return;
                var q = ny * w + nx;
                if (outside[q] || lines.Data[q] >= 128) return;
                outside[q] = true;
                stack.Push(q);
            }

            Visit(x - 1, y);
            Visit(x + 1, y);
            Visit(x, y - 1);
            Visit(x, y + 1);
        }

        var result = new Mask(width, height);
        long inside = 0;
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            if (outside[(y + 1) * w + x + 1]) continue;
            result.Data[y * width + x] = 255;
            inside++;
        }

        // An open outline leaks and leaves only the lines themselves.
        return inside > (long)width * height / 10 ? result : full;
    }
}
