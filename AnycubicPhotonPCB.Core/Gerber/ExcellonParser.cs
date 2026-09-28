using System.Globalization;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Gerber;

// Excellon (NC drill) parser: drill hits, G85 slots and routed paths
public static partial class ExcellonParser
{
    // Heuristic check whether a text file is an Excellon drill file rather than a Gerber
    public static bool LooksLikeExcellon(string text)
    {
        if (text.Contains("%FS", StringComparison.Ordinal) || text.Contains("%MO", StringComparison.Ordinal)) return false;
        foreach (var raw in text.Split('\n').Take(200))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';')) continue;
            if (line.StartsWith("M48", StringComparison.Ordinal)) return true;
            if (ToolDefinitionRegex().IsMatch(line)) return true;
        }

        return false;
    }

    public static GerberImage Parse(string text)
    {
        var image = new GerberImage();
        var tools = new Dictionary<int, double>();

        var metric = false;
        var intDigits = -1;
        var decDigits = -1;
        var leadingZerosKept = false; // "LZ": leading zeros present, trailing omitted
        var incremental = false;

        double x = 0, y = 0;
        var tool = -1;
        var routing = false;   // G00 route mode
        var toolDown = false;  // M15
        List<SKPoint>? route = null;

        void FlushRoute()
        {
            if (route is { Count: >= 2 } && tools.TryGetValue(tool, out var d)) EmitStroke(image, route, d);
            route = null;
        }

        double Coordinate(string s)
        {
            var unit = metric ? 1.0 : 25.4;
            if (s.Contains('.')) return double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture) * unit;

            var negative = s.StartsWith('-');
            var digits = s.TrimStart('+', '-');
            if (digits.Length == 0) return 0;
            var ints = intDigits > 0 ? intDigits : metric ? 3 : 2;
            var decs = decDigits > 0 ? decDigits : metric ? 3 : 4;
            double value;
            if (leadingZerosKept)
            {
                var padded = digits.PadRight(ints + decs, '0');
                value = long.Parse(padded, CultureInfo.InvariantCulture) / Math.Pow(10, padded.Length - ints);
            }
            else
            {
                value = long.Parse(digits, CultureInfo.InvariantCulture) / Math.Pow(10, decs);
            }

            return (negative ? -value : value) * unit;
        }

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;

            if (line.StartsWith(';'))
            {
                // KiCad / Altium: ;FILE_FORMAT=4:4
                var m = FileFormatRegex().Match(line);
                if (m.Success)
                {
                    intDigits = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    decDigits = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                }

                continue;
            }

            var upper = line.ToUpperInvariant();
            if (upper.StartsWith("METRIC") || upper.StartsWith("INCH"))
            {
                metric = upper.StartsWith("METRIC");
                if (upper.Contains(",LZ")) leadingZerosKept = true;
                else if (upper.Contains(",TZ")) leadingZerosKept = false;

                // Format hint such as METRIC,LZ,000.000
                var fmt = FormatHintRegex().Match(upper);
                if (fmt.Success)
                {
                    intDigits = fmt.Groups[1].Length;
                    decDigits = fmt.Groups[2].Length;
                }

                continue;
            }

            if (upper.StartsWith("M71")) { metric = true; continue; }
            if (upper.StartsWith("M72")) { metric = false; continue; }
            if (upper.StartsWith("ICI,ON") || upper == "G91") { incremental = true; continue; }
            if (upper == "G90") { incremental = false; continue; }
            if (upper.StartsWith("M30") || upper.StartsWith("M00")) break;
            if (upper.StartsWith("M15")) { toolDown = true; continue; }
            if (upper.StartsWith("M16") || upper.StartsWith("M17")) { toolDown = false; FlushRoute(); continue; }

            // Tool definition or selection: T1C0.8 / T01 / T1F00S00C0.035
            var toolMatch = ToolRegex().Match(upper);
            if (toolMatch.Success && upper.StartsWith('T'))
            {
                FlushRoute();
                var id = int.Parse(toolMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                var cIndex = upper.IndexOf('C');
                if (cIndex > 0)
                {
                    var dm = NumberRegex().Match(upper, cIndex + 1);
                    if (dm.Success)
                        tools[id] = double.Parse(dm.Value, NumberStyles.Float, CultureInfo.InvariantCulture) * (metric ? 1 : 25.4);
                }

                tool = id;
                continue;
            }

            if (!upper.Contains('X') && !upper.Contains('Y'))
            {
                if (upper.StartsWith("G00")) { routing = true; FlushRoute(); }
                else if (upper.StartsWith("G05")) { routing = false; FlushRoute(); }
                continue;
            }

            // Slot: X..Y..G85X..Y..
            var g85 = upper.IndexOf("G85", StringComparison.Ordinal);
            if (g85 > 0)
            {
                var (sx, sy) = ReadXY(upper[..g85], x, y);
                var (ex, ey) = ReadXY(upper[(g85 + 3)..], sx, sy);
                if (tools.TryGetValue(tool, out var d))
                {
                    EmitStroke(image, [new SKPoint((float)sx, (float)sy), new SKPoint((float)ex, (float)ey)], d);
                }

                x = ex;
                y = ey;
                continue;
            }

            var isG00 = upper.StartsWith("G00");
            var isG01 = upper.StartsWith("G01");
            if (isG00) { routing = true; FlushRoute(); }

            var (nx, ny) = ReadXY(upper, x, y);

            if (routing && (isG01 || upper.StartsWith("G02") || upper.StartsWith("G03") || (!isG00 && toolDown)))
            {
                route ??= [new SKPoint((float)x, (float)y)];
                route.Add(new SKPoint((float)nx, (float)ny));
            }
            else if (!routing || !toolDown)
            {
                if (!routing && tools.TryGetValue(tool, out var d))
                    image.Objects.Add(new GraphicObject(Geometry.Circle(nx, ny, d), true));
            }

            x = nx;
            y = ny;
        }

        FlushRoute();
        return image;

        (double, double) ReadXY(string s, double px, double py)
        {
            double rx = px, ry = py;
            foreach (var (letter, value) in GerberReader.SplitWords(s))
            {
                if (letter == 'X') rx = Coordinate(value) + (incremental ? px : 0);
                else if (letter == 'Y') ry = Coordinate(value) + (incremental ? py : 0);
            }

            return (rx, ry);
        }
    }

    private static void EmitStroke(GerberImage image, List<SKPoint> centerLine, double diameter)
    {
        var outline = Geometry.StrokeOutline(Geometry.Polyline(centerLine), diameter);
        if (outline != null) image.Objects.Add(new GraphicObject(outline, true));
    }

    [GeneratedRegex(@"^T\d+[^XY]*C[\d.]+")]
    private static partial Regex ToolDefinitionRegex();

    [GeneratedRegex(@"FILE_FORMAT\s*=\s*(\d+)\s*:\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex FileFormatRegex();

    [GeneratedRegex(@"(0+)\.(0+)")]
    private static partial Regex FormatHintRegex();

    [GeneratedRegex(@"^T(\d+)")]
    private static partial Regex ToolRegex();

    [GeneratedRegex(@"[\d.]+")]
    private static partial Regex NumberRegex();
}
