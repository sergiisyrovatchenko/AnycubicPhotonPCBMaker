using SkiaSharp;
using System.Globalization;

namespace AnycubicPhotonPCB.Core.Gerber;

// Aperture macro (%AM) definition. Evaluated when an aperture is defined with concrete parameters
internal sealed class ApertureMacro
{
    private readonly List<string> _statements;

    public ApertureMacro(string name, IEnumerable<string> statements)
    {
        Name = name;
        _statements = statements.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    }

    public string Name { get; }

    // Builds the aperture shape in millimetres
    public SKPath Build(IReadOnlyList<double> parameters, double unitToMm)
    {
        var vars = new Dictionary<int, double>();
        for (var i = 0; i < parameters.Count; i++) vars[i + 1] = parameters[i];

        var shape = new SKPath();
        foreach (var statement in _statements)
        {
            if (IsComment(statement)) continue;

            var compact = new string(statement.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (compact.StartsWith('$'))
            {
                var eq = compact.IndexOf('=');
                if (eq > 1 && int.TryParse(compact.AsSpan(1, eq - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                    vars[id] = MacroExpression.Evaluate(compact[(eq + 1)..], vars);
                continue;
            }

            var parts = compact.Split(',');
            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)) continue;
            var args = parts.Skip(1).Select(p => MacroExpression.Evaluate(p, vars)).ToArray();

            var primitive = BuildPrimitive(code, args, out var exposureOn);
            if (primitive == null || primitive.IsEmpty) continue;

            shape = exposureOn ? Geometry.Union(shape, primitive) : Geometry.Difference(shape, primitive);
        }

        var toMm = SKMatrix.CreateScale((float)unitToMm, (float)unitToMm);
        shape.Transform(in toMm);
        return shape;
    }

    private static bool IsComment(string statement)
    {
        if (statement.Length == 0 || statement[0] != '0') return false;
        return statement.Length == 1 || !(char.IsDigit(statement[1]) || statement[1] == ',');
    }

    private static double Arg(double[] args, int index) => index < args.Length ? args[index] : 0;

    private static SKPath? BuildPrimitive(int code, double[] a, out bool exposureOn)
    {
        exposureOn = Arg(a, 0) != 0;
        SKPath path;
        double rotation;

        switch (code)
        {
            case 1: // circle: exposure, diameter, cx, cy[, rotation]
                path = Geometry.Circle(Arg(a, 2), Arg(a, 3), Arg(a, 1));
                rotation = Arg(a, 4);
                break;

            case 2:
            case 20: // vector line: exposure, width, sx, sy, ex, ey, rotation
            {
                var w = Arg(a, 1);
                double sx = Arg(a, 2), sy = Arg(a, 3), ex = Arg(a, 4), ey = Arg(a, 5);
                var len = Math.Sqrt((ex - sx) * (ex - sx) + (ey - sy) * (ey - sy));
                if (len <= 0 || w <= 0) return null;
                var nx = -(ey - sy) / len * w / 2;
                var ny = (ex - sx) / len * w / 2;
                path = Geometry.Polygon(
                [
                    new SKPoint((float)(sx + nx), (float)(sy + ny)),
                    new SKPoint((float)(ex + nx), (float)(ey + ny)),
                    new SKPoint((float)(ex - nx), (float)(ey - ny)),
                    new SKPoint((float)(sx - nx), (float)(sy - ny)),
                ]);
                rotation = Arg(a, 6);
                break;
            }

            case 21: // center line: exposure, width, height, cx, cy, rotation
                path = Geometry.Rect(Arg(a, 3), Arg(a, 4), Arg(a, 1), Arg(a, 2));
                rotation = Arg(a, 5);
                break;

            case 22: // lower-left line (deprecated): exposure, width, height, x, y, rotation
                path = Geometry.Rect(Arg(a, 3) + Arg(a, 1) / 2, Arg(a, 4) + Arg(a, 2) / 2, Arg(a, 1), Arg(a, 2));
                rotation = Arg(a, 5);
                break;

            case 4: // outline: exposure, n, x0, y0, ... xn, yn, rotation
            {
                var n = (int)Arg(a, 1);
                var pts = new List<SKPoint>();
                for (var i = 0; i <= n; i++)
                    pts.Add(new SKPoint((float)Arg(a, 2 + i * 2), (float)Arg(a, 3 + i * 2)));
                path = Geometry.Polygon(pts);
                rotation = Arg(a, 4 + n * 2);
                break;
            }

            case 5: // polygon: exposure, vertices, cx, cy, diameter, rotation
                path = Geometry.RegularPolygon(Arg(a, 2), Arg(a, 3), Arg(a, 4), (int)Arg(a, 1), 0);
                rotation = Arg(a, 5);
                break;

            case 6: // moire: cx, cy, outer dia, ring thickness, gap, max rings, crosshair thickness, crosshair length, rotation
            {
                exposureOn = true;
                double cx = Arg(a, 0), cy = Arg(a, 1), od = Arg(a, 2), thick = Arg(a, 3), gap = Arg(a, 4);
                var rings = (int)Arg(a, 5);
                path = new SKPath();
                for (var i = 0; i < rings; i++)
                {
                    var outer = od - 2 * i * (thick + gap);
                    if (outer <= 0) break;
                    var inner = Math.Max(0, outer - 2 * thick);
                    var ring = Geometry.Difference(Geometry.Circle(cx, cy, outer), Geometry.Circle(cx, cy, inner));
                    path = Geometry.Union(path, ring);
                }

                double ct = Arg(a, 6), cl = Arg(a, 7);
                path = Geometry.Union(path, Geometry.Rect(cx, cy, cl, ct));
                path = Geometry.Union(path, Geometry.Rect(cx, cy, ct, cl));
                rotation = Arg(a, 8);
                break;
            }

            case 7: // thermal: cx, cy, outer dia, inner dia, gap, rotation
            {
                exposureOn = true;
                double cx = Arg(a, 0), cy = Arg(a, 1), od = Arg(a, 2), id = Arg(a, 3), gap = Arg(a, 4);
                path = Geometry.Difference(Geometry.Circle(cx, cy, od), Geometry.Circle(cx, cy, id));
                path = Geometry.Difference(path, Geometry.Rect(cx, cy, od * 1.1, gap));
                path = Geometry.Difference(path, Geometry.Rect(cx, cy, gap, od * 1.1));
                rotation = Arg(a, 5);
                break;
            }

            default:
                return null;
        }

        if (rotation != 0)
        {
            var rotate = SKMatrix.CreateRotationDegrees((float)rotation);
            path.Transform(in rotate);
        }
        return path;
    }
}

// Arithmetic expression evaluator for aperture macro parameters
internal static class MacroExpression
{
    public static double Evaluate(string text, IReadOnlyDictionary<int, double> vars)
    {
        var pos = 0;
        var value = ParseSum(text, ref pos, vars);
        return double.IsFinite(value) ? value : 0;
    }

    private static double ParseSum(string s, ref int pos, IReadOnlyDictionary<int, double> vars)
    {
        var value = ParseProduct(s, ref pos, vars);
        while (pos < s.Length)
        {
            var op = s[pos];
            if (op != '+' && op != '-') break;
            pos++;
            var rhs = ParseProduct(s, ref pos, vars);
            value = op == '+' ? value + rhs : value - rhs;
        }

        return value;
    }

    private static double ParseProduct(string s, ref int pos, IReadOnlyDictionary<int, double> vars)
    {
        var value = ParseUnary(s, ref pos, vars);
        while (pos < s.Length)
        {
            var op = s[pos];
            if (op != 'x' && op != 'X' && op != '/') break;
            pos++;
            var rhs = ParseUnary(s, ref pos, vars);
            value = op == '/' ? (rhs == 0 ? 0 : value / rhs) : value * rhs;
        }

        return value;
    }

    private static double ParseUnary(string s, ref int pos, IReadOnlyDictionary<int, double> vars)
    {
        if (pos < s.Length && (s[pos] == '-' || s[pos] == '+'))
        {
            var negative = s[pos] == '-';
            pos++;
            var v = ParseUnary(s, ref pos, vars);
            return negative ? -v : v;
        }

        return ParseAtom(s, ref pos, vars);
    }

    private static double ParseAtom(string s, ref int pos, IReadOnlyDictionary<int, double> vars)
    {
        if (pos >= s.Length) return 0;

        if (s[pos] == '(')
        {
            pos++;
            var v = ParseSum(s, ref pos, vars);
            if (pos < s.Length && s[pos] == ')') pos++;
            return v;
        }

        if (s[pos] == '$')
        {
            pos++;
            var start = pos;
            while (pos < s.Length && char.IsDigit(s[pos])) pos++;
            var id = int.Parse(s.AsSpan(start, pos - start), CultureInfo.InvariantCulture);
            return vars.TryGetValue(id, out var value) ? value : 0;
        }

        var numStart = pos;
        while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '.')) pos++;
        if (pos == numStart)
        {
            pos++; // skip unknown character
            return 0;
        }

        return double.Parse(s.AsSpan(numStart, pos - numStart), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
