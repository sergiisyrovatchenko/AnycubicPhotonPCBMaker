using System.Buffers.Binary;

namespace AnycubicPhotonPCB.Core.Formats;

// Statistics of an exposed layer, shared by the layer image and scene.slice
public sealed record LayerStats(
    double AreaMm2,
    float XMin,
    float YMin,
    float XMax,
    float YMax,
    uint ObjectCount)
{
    public bool IsEmpty => AreaMm2 <= 0;
}

// Encodes a raster layer into the Photon Workshop 4 vector layer format ("pwszImg").
// Layout (little endian):
// "{==\0"  f32 area*2  f32 xMin yMin xMax yMax  u32 0  u32 objectCount
// "[--\0"  u32 0  u32 edgeCount  u32 1
//          edgeCount * { f32 x0 y0 x1 y1 (mm, relative to screen centre)  u8 flag }
// "--]\0"  "==}\0"
// Photon Workshop only emits axis aligned edges sorted by their starting Y, which is exactly the outline
// of the pixel grid. Vertical edges run towards +Y, horizontal edges towards +X; the flag is 1 when the
// exposed area lies to the left of the travel direction (i.e. on the -X side of a vertical edge or the
// +Y side of a horizontal edge)
public static class PwszLayerEncoder
{
    private const int EdgeSize = 17;

    private readonly record struct Edge(int X0, int Y0, int X1, int Y1, byte Flag);

    public static byte[] Encode(ReadOnlySpan<byte> pixels, int width, int height, double pixelSizeMm, out LayerStats stats)
    {
        stats = ComputeStats(pixels, width, height, pixelSizeMm);
        var edges = BuildEdges(pixels, width, height);
        edges.Sort((a, b) =>
        {
            var c = a.Y0.CompareTo(b.Y0);
            return c != 0 ? c : a.X0.CompareTo(b.X0);
        });

        var halfW = width * pixelSizeMm / 2;
        var halfH = height * pixelSizeMm / 2;
        var xs = new float[width + 1];
        var ys = new float[height + 1];
        for (var i = 0; i <= width; i++) xs[i] = Round(i * pixelSizeMm - halfW);
        for (var i = 0; i <= height; i++) ys[i] = Round(i * pixelSizeMm - halfH);

        var output = new byte[4 + 4 * 5 + 4 + 4 + 4 + 4 + 4 + 4 + edges.Count * EdgeSize + 8];
        var s = output.AsSpan();
        var p = 0;

        void Marker(string m, Span<byte> span)
        {
            for (var i = 0; i < 3; i++) span[p + i] = (byte)m[i];
            span[p + 3] = 0;
            p += 4;
        }

        void F(float v, Span<byte> span)
        {
            BinaryPrimitives.WriteSingleLittleEndian(span[p..], v);
            p += 4;
        }

        void U(uint v, Span<byte> span)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(span[p..], v);
            p += 4;
        }

        Marker("{==", s);
        F(Round(stats.AreaMm2 * 2), s);
        F(stats.XMin, s);
        F(stats.YMin, s);
        F(stats.XMax, s);
        F(stats.YMax, s);
        U(0, s);
        U(stats.ObjectCount, s);
        Marker("[--", s);
        U(0, s);
        U((uint)edges.Count, s);
        U(1, s);
        foreach (var e in edges)
        {
            F(xs[e.X0], s);
            F(ys[e.Y0], s);
            F(xs[e.X1], s);
            F(ys[e.Y1], s);
            s[p++] = e.Flag;
        }

        Marker("--]", s);
        Marker("==}", s);
        return output;
    }

    // Rasterises a pwszImg layer back to pixels (even-odd scanline fill over vertical edges)
    public static byte[] Decode(ReadOnlySpan<byte> data, int width, int height, double pixelSizeMm)
    {
        if (data.Length < 56 || data[0] != '{' || data[1] != '=' || data[2] != '=') throw new InvalidDataException("Invalid pwszImg header.");
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data[40..]);
        var halfW = width * pixelSizeMm / 2;
        var halfH = height * pixelSizeMm / 2;
        var toggles = new byte[width * height];
        var p = 48;
        for (var i = 0; i < count; i++, p += EdgeSize)
        {
            var x0 = BinaryPrimitives.ReadSingleLittleEndian(data[p..]);
            var y0 = BinaryPrimitives.ReadSingleLittleEndian(data[(p + 4)..]);
            var x1 = BinaryPrimitives.ReadSingleLittleEndian(data[(p + 8)..]);
            var y1 = BinaryPrimitives.ReadSingleLittleEndian(data[(p + 12)..]);
            if (x0 != x1) continue;
            var c = (int)Math.Round((x0 + halfW) / pixelSizeMm);
            var r0 = (int)Math.Round((Math.Min(y0, y1) + halfH) / pixelSizeMm);
            var r1 = (int)Math.Round((Math.Max(y0, y1) + halfH) / pixelSizeMm);
            if (c >= width) continue;
            for (var r = Math.Max(0, r0); r < Math.Min(height, r1); r++) toggles[r * width + c] ^= 1;
        }

        var result = new byte[width * height];
        for (var r = 0; r < height; r++)
        {
            byte state = 0;
            for (var c = 0; c < width; c++)
            {
                state ^= toggles[r * width + c];
                result[r * width + c] = state == 1 ? (byte)255 : (byte)0;
            }
        }

        return result;
    }

    private static float Round(double v) => (float)Math.Round(v, 3, MidpointRounding.AwayFromZero);

    private static List<Edge> BuildEdges(ReadOnlySpan<byte> pixels, int width, int height)
    {
        var edges = new List<Edge>();

        // Vertical edges: boundaries between columns, merged across consecutive rows.
        // Key = x * 2 + flag (flag 0: left boundary of a run, 1: right boundary).
        var open = new List<(int Key, int StartRow)>();
        var next = new List<(int Key, int StartRow)>();
        var current = new List<int>();
        for (var r = 0; r <= height; r++)
        {
            current.Clear();
            if (r < height)
            {
                // Runs of exposed pixels: a left boundary (flag 0) where a run starts, a right one (flag 1) where it ends
                var row = pixels.Slice(r * width, width);
                for (var c = 0; c < width;)
                {
                    var start = NextExposed(row, c);
                    if (start < 0) break;
                    var end = RunEnd(row, start);
                    current.Add(start * 2);
                    current.Add(end * 2 + 1);
                    c = end;
                }
            }

            // Merge the sorted lists of open segments and boundaries of this row.
            next.Clear();
            int i = 0, j = 0;
            while (i < open.Count || j < current.Count)
            {
                if (j >= current.Count || (i < open.Count && open[i].Key < current[j]))
                {
                    Close(open[i], r);
                    i++;
                }
                else if (i >= open.Count || current[j] < open[i].Key)
                {
                    next.Add((current[j], r));
                    j++;
                }
                else
                {
                    next.Add(open[i]);
                    i++;
                    j++;
                }
            }

            (open, next) = (next, open);
        }

        void Close((int Key, int StartRow) segment, int endRow)
        {
            var x = segment.Key >> 1;
            var flag = (byte)(segment.Key & 1);
            edges.Add(new Edge(x, segment.StartRow, x, endRow, flag));
        }

        // Horizontal edges: boundaries between rows, merged across consecutive columns.
        // Type 0: exposed above, 1: exposed below
        for (var r = 0; r <= height; r++)
        {
            if (r == 0 || r == height)
            {
                // Top / bottom border of the screen: every exposed run of the outer row is an edge
                var row = pixels.Slice((r == 0 ? 0 : r - 1) * width, width);
                var type = (byte)(r == 0 ? 1 : 0);
                for (var c = 0; c < width;)
                {
                    var start = NextExposed(row, c);
                    if (start < 0) break;
                    var end = RunEnd(row, start);
                    edges.Add(new Edge(start, r, end, r, type));
                    c = end;
                }

                continue;
            }

            var above = pixels.Slice((r - 1) * width, width);
            var below = pixels.Slice(r * width, width);
            for (var c = 0; c < width;)
            {
                // Identical stretches of the two rows never contain an edge
                c += above[c..].CommonPrefixLength(below[c..]);
                if (c >= width) break;

                var type = EdgeType(above[c], below[c]);
                if (type < 0)
                {
                    c++;
                    continue;
                }

                var start = c;
                while (c < width && EdgeType(above[c], below[c]) == type) c++;
                edges.Add(new Edge(start, r, c, r, (byte)type));
            }
        }

        return edges;
    }

    private static int EdgeType(byte above, byte below)
    {
        var a = above == 255;
        var b = below == 255;
        return a == b ? -1 : b ? 1 : 0;
    }

    // Index of the first exposed (255) pixel at or after from, or -1
    private static int NextExposed(ReadOnlySpan<byte> row, int from)
    {
        var i = row[from..].IndexOf((byte)255);
        return i < 0 ? -1 : from + i;
    }

    // End (exclusive) of the exposed run starting at start
    private static int RunEnd(ReadOnlySpan<byte> row, int start)
    {
        var i = row[start..].IndexOfAnyExcept((byte)255);
        return i < 0 ? row.Length : start + i;
    }

    public static LayerStats ComputeStats(ReadOnlySpan<byte> pixels, int width, int height, double pixelSizeMm)
    {
        long count = 0;
        int minC = int.MaxValue, minR = int.MaxValue, maxC = -1, maxR = -1;

        // Connected components (8-connectivity) over horizontal runs.
        var parent = new List<int>();
        var prevRuns = new List<(int Start, int End, int Id)>();
        var curRuns = new List<(int Start, int End, int Id)>();

        int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        for (var r = 0; r < height; r++)
        {
            var row = pixels.Slice(r * width, width);
            curRuns.Clear();
            for (var c = 0; c < width;)
            {
                var start = NextExposed(row, c);
                if (start < 0) break;
                var end = RunEnd(row, start); // exclusive
                c = end;
                count += end - start;
                minC = Math.Min(minC, start);
                maxC = Math.Max(maxC, end - 1);
                minR = Math.Min(minR, r);
                maxR = r;

                var id = parent.Count;
                parent.Add(id);
                foreach (var pr in prevRuns)
                {
                    if (pr.End < start || pr.Start > end) continue; // 8-connected: touching diagonally counts
                    var a = Find(pr.Id);
                    var b = Find(id);
                    if (a != b) parent[Math.Max(a, b)] = Math.Min(a, b);
                }

                curRuns.Add((start, end, id));
            }

            (prevRuns, curRuns) = (curRuns, prevRuns);
        }

        if (count == 0) return new LayerStats(0, 0, 0, 0, 0, 0);

        uint objects = 0;
        for (var i = 0; i < parent.Count; i++)
            if (Find(i) == i) objects++;

        var halfW = width * pixelSizeMm / 2;
        var halfH = height * pixelSizeMm / 2;
        return new LayerStats(
            count * pixelSizeMm * pixelSizeMm,
            Round(minC * pixelSizeMm - halfW),
            Round(minR * pixelSizeMm - halfH),
            Round((maxC + 1) * pixelSizeMm - halfW),
            Round((maxR + 1) * pixelSizeMm - halfH),
            objects);
    }
}
