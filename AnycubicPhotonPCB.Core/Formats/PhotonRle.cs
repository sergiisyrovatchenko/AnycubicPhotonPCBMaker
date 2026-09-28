namespace AnycubicPhotonPCB.Core.Formats;

// Run length encoders used by Photon Workshop files. Any pixel that is not 255 is treated as black
// (anti-aliasing is not needed for PCB masks)
public static class PhotonRle
{
    private const int RleMaxRun = 0x7f;
    private const int Rle4MaxRun = 0xfff;

    // PW0 / "RLE4": high nibble = colour (0x0 or 0xF), 12-bit run length over two bytes
    public static byte[] EncodeRle4(ReadOnlySpan<byte> pixels)
    {
        var output = new List<byte>(pixels.Length / 64 + 16);
        for (var i = 0; i < pixels.Length;)
        {
            var run = ScanRun(pixels, i, Rle4MaxRun);
            var color = pixels[i] == 255 ? 0xf : 0x0;
            output.Add((byte)((color << 4) | (run >> 8)));
            output.Add((byte)(run & 0xff));
            i += run;
        }

        return output.ToArray();
    }

    // Legacy 7-bit RLE: bit 7 = colour, bits 0..6 = run length
    public static byte[] EncodeRle(ReadOnlySpan<byte> pixels)
    {
        var output = new List<byte>(pixels.Length / 32 + 16);
        for (var i = 0; i < pixels.Length;)
        {
            var run = ScanRun(pixels, i, RleMaxRun);
            var color = pixels[i] == 255 ? 0x1 : 0x0;
            output.Add((byte)((color << 7) | run));
            i += run;
        }

        return output.ToArray();
    }

    public static byte[] DecodeRle4(ReadOnlySpan<byte> data, int pixelCount)
    {
        var result = new byte[pixelCount];
        var pos = 0;
        for (var i = 0; i + 1 < data.Length && pos < pixelCount; i += 2)
        {
            var color = (data[i] >> 4) == 0xf ? (byte)255 : (byte)0;
            var run = ((data[i] & 0xf) << 8) | data[i + 1];
            result.AsSpan(pos, Math.Min(run, pixelCount - pos)).Fill(color);
            pos += run;
        }

        return result;
    }

    public static byte[] DecodeRle(ReadOnlySpan<byte> data, int pixelCount)
    {
        var result = new byte[pixelCount];
        var pos = 0;
        foreach (var b in data)
        {
            if (pos >= pixelCount) break;
            var run = b & 0x7f;
            var color = (b & 0x80) != 0 ? (byte)255 : (byte)0;
            result.AsSpan(pos, Math.Min(run, pixelCount - pos)).Fill(color);
            pos += run;
        }

        return result;
    }

    private static int ScanRun(ReadOnlySpan<byte> pixels, int start, int maxRun)
    {
        var white = pixels[start] == 255;
        var run = 1;
        while (run < maxRun && start + run < pixels.Length && (pixels[start + run] == 255) == white) run++;
        return run;
    }
}
