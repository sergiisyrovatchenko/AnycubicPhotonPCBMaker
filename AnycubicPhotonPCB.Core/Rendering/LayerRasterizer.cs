using AnycubicPhotonPCB.Core.Gerber;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Rendering;

// 8-bit single channel image. 255 = feature / exposed, 0 = empty
public sealed class Mask
{
    public Mask(int width, int height, byte[]? data = null)
    {
        Width = width;
        Height = height;
        Data = data ?? new byte[checked(width * height)];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Data { get; }

    public byte this[int x, int y]
    {
        get => Data[y * Width + x];
        set => Data[y * Width + x] = value;
    }

    // Keeps the larger coverage of both masks in this one: the union of their features (same size)
    public void UnionWith(Mask other)
    {
        var t = Data;
        var o = other.Data;
        for (var i = 0; i < t.Length; i++)
            if (o[i] > t[i]) t[i] = o[i];
    }
}

// Quarter turn of the board image as seen in pixel space (after the flips): Left = counter-clockwise, Right = clockwise
public enum BoardRotation
{
    None,
    Left,
    Right,
}

public static class LayerRasterizer
{
    // Matrix that maps board millimetres (Y up) onto destination pixels (Y down),
    // stretching the board rectangle to fill the destination, optionally mirrored and then turned by 90°.
    // With a rotation the destination holds the turned board, so its width corresponds to the board height
    public static SKMatrix BoardToPixels(SKRect board, SKRect destination, bool flipHorizontal, bool flipVertical, BoardRotation rotation = BoardRotation.None)
    {
        var turned = rotation != BoardRotation.None;
        var width = turned ? destination.Height : destination.Width;
        var height = turned ? destination.Width : destination.Height;
        var sx = width / board.Width;
        var sy = height / board.Height;

        // Board top-left in pixel space is (left, maxY); Gerber Y grows upwards.
        var m = SKMatrix.CreateTranslation(-board.Left, -board.Bottom);
        m = m.PostConcat(SKMatrix.CreateScale(sx, -sy));
        if (flipHorizontal) m = m.PostConcat(SKMatrix.CreateScale(-1, 1)).PostConcat(SKMatrix.CreateTranslation(width, 0));
        if (flipVertical) m = m.PostConcat(SKMatrix.CreateScale(1, -1)).PostConcat(SKMatrix.CreateTranslation(0, height));

        // Exact quarter turns; SKMatrix.CreateRotationDegrees leaves rounding noise in the cosine terms
        m = rotation switch
        {
            BoardRotation.Right => m.PostConcat(new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1)),  // (x, y) -> (h - y, x)
            BoardRotation.Left => m.PostConcat(new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1)),    // (x, y) -> (y, w - x)
            _ => m,
        };
        return m.PostConcat(SKMatrix.CreateTranslation(destination.Left, destination.Top));
    }

    // Same as BoardToPixels but keeps the aspect ratio and centres the board
    public static SKMatrix BoardToPixelsFit(SKRect board, int width, int height, float margin, bool flipHorizontal, bool flipVertical, BoardRotation rotation = BoardRotation.None)
    {
        var turned = rotation != BoardRotation.None;
        var boardW = turned ? board.Height : board.Width;
        var boardH = turned ? board.Width : board.Height;
        var scale = Math.Min((width - 2 * margin) / boardW, (height - 2 * margin) / boardH);
        var w = boardW * scale;
        var h = boardH * scale;
        var dest = SKRect.Create((width - w) / 2, (height - h) / 2, w, h);
        return BoardToPixels(board, dest, flipHorizontal, flipVertical, rotation);
    }

    public static Mask Render(GerberImage image, SKMatrix transform, int width, int height, bool antialias)
    {
        var info = new SKImageInfo(width, height, SKColorType.Alpha8, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        bitmap.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.SetMatrix(in transform);

            // Anti-aliased shapes that share an edge (a track or teardrop ending exactly on a pad) each cover
            // half of the edge pixels; drawn over each other that gives 75%, a faint seam. Adding the coverage
            // (saturating at 100%) closes it. Without anti-aliasing coverage is 0 or 100% and both modes agree
            var darkBlend = antialias ? SKBlendMode.Plus : SKBlendMode.SrcOver;
            using var dark = new SKPaint { IsAntialias = antialias, Color = SKColors.White, Style = SKPaintStyle.Fill, BlendMode = darkBlend };
            using var clear = new SKPaint { IsAntialias = antialias, Color = SKColors.White, Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Clear };
            using var darkLine = new SKPaint { IsAntialias = antialias, Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 0, BlendMode = darkBlend };
            using var clearLine = new SKPaint { IsAntialias = antialias, Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 0, BlendMode = SKBlendMode.Clear };

            foreach (var obj in image.Objects)
            {
                var paint = obj.Hairline ? (obj.Dark ? darkLine : clearLine) : (obj.Dark ? dark : clear);
                canvas.DrawPath(obj.Path, paint);
            }
        }

        var mask = new Mask(width, height);
        var rowBytes = bitmap.RowBytes;
        var src = bitmap.GetPixelSpan();
        for (var y = 0; y < height; y++)
            src.Slice(y * rowBytes, width).CopyTo(mask.Data.AsSpan(y * width, width));

        return mask;
    }

    // Clears every pixel of target that is set in holes (threshold 50%)
    public static void Subtract(Mask target, Mask holes)
    {
        var t = target.Data;
        var h = holes.Data;
        for (var i = 0; i < t.Length; i++)
            if (h[i] >= 128) t[i] = 0;
    }

    // Scales coverage by (255 - holes) for anti-aliased previews
    public static void SubtractSoft(Mask target, Mask holes)
    {
        var t = target.Data;
        var h = holes.Data;
        for (var i = 0; i < t.Length; i++)
            t[i] = (byte)(t[i] * (255 - h[i]) / 255);
    }
}
