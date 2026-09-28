using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace AnycubicPhotonPCB.Maker;

// Building GDI+ bitmaps for the picture boxes. Pixels are assembled as whole 32-bit ARGB words per row
internal static class ImageConvert
{
    // A 32-bit ARGB bitmap; fillRow(y, row) fills one row of width pixels (0xAARRGGBB)
    public static Bitmap ToBitmap(int width, int height, Action<int, int[]> fillRow)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new int[width];
            for (var y = 0; y < height; y++)
            {
                fillRow(y, row);
                Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, row.Length);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    // Opaque grey pixel of the given level
    public static int Grey(int level) => unchecked((int)(0xFF000000u | (uint)level * 0x010101u));

    // Unpremultiplied RGBA Skia bitmap to a GDI+ bitmap with alpha (GDI+ stores BGRA)
    public static Bitmap FromSkia(SKBitmap source)
    {
        // A span cannot be captured by the row callback, so the pixels are copied once
        var pixels = source.GetPixelSpan().ToArray();
        var rowBytes = source.RowBytes;
        return ToBitmap(source.Width, source.Height, (y, row) =>
        {
            var src = MemoryMarshal.Cast<byte, uint>(pixels.AsSpan(y * rowBytes, row.Length * 4));
            for (var x = 0; x < row.Length; x++)
            {
                // RGBA in memory is 0xAABBGGRR as a little endian word; swap R and B
                var p = src[x];
                row[x] = unchecked((int)((p & 0xFF00FF00u) | ((p & 0xFFu) << 16) | ((p >> 16) & 0xFFu)));
            }
        });
    }

    // Pixels covered by a rectangle: a pixel belongs to it when its centre does, clamped to the image
    public static Rectangle PixelBounds(SKRect rect, int width, int height)
    {
        var x0 = Math.Clamp((int)Math.Ceiling(rect.Left - 0.5f), 0, width);
        var x1 = Math.Clamp((int)Math.Ceiling(rect.Right - 0.5f), 0, width);
        var y0 = Math.Clamp((int)Math.Ceiling(rect.Top - 0.5f), 0, height);
        var y1 = Math.Clamp((int)Math.Ceiling(rect.Bottom - 0.5f), 0, height);
        return Rectangle.FromLTRB(x0, y0, x1, y1);
    }

    // Shows a new image in the picture box and releases the one it replaces
    public static void ReplaceImage(this PictureBox box, Image? image)
    {
        var old = box.Image;
        box.Image = image;
        old?.Dispose();
    }
}
