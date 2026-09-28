using System.Globalization;
using System.Text;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Gerber;

// RS-274X (Gerber X1/X2) parser. Produces filled shapes in millimetres
public static class GerberParser
{
    public static GerberImage Parse(string text) => new GerberReader(text).Read();
}

internal sealed class Aperture
{
    public SKPath? Shape { get; init; }

    // Diameter of a plain circular aperture (strokes are drawn with round caps)
    public double? CircleDiameter { get; init; }

    // Contents of a block aperture (%AB)
    public List<GraphicObject>? Block { get; init; }
}

internal sealed class GerberReader
{
    private enum Interpolation { Linear, Clockwise, CounterClockwise }

    private readonly string _text;
    private readonly GerberImage _image = new();
    private readonly Dictionary<int, Aperture> _apertures = new();
    private readonly Dictionary<string, ApertureMacro> _macros = new(StringComparer.Ordinal);

    // Output target stack: step-repeat and block apertures collect objects before emitting them.
    private readonly Stack<(List<GraphicObject> Objects, Action<List<GraphicObject>> Close)> _targets = new();
    private List<GraphicObject> _target;

    // Coordinate format
    private int _xInt = 2, _xDec = 4, _yInt = 2, _yDec = 4;
    private bool _trailingZerosOmitted;
    private bool _incremental;
    private double _unit = 25.4; // mm per file unit; inches unless told otherwise

    // Graphics state
    private double _x, _y;
    private Interpolation _interpolation = Interpolation.Linear;
    private bool _multiQuadrant;
    private bool _dark = true;
    private int _currentAperture = -1;
    private int _lastOperation = 2;
    private bool _mirrorX, _mirrorY;
    private double _rotation;
    private double _scale = 1;

    // Region state
    private bool _regionMode;
    private List<SKPoint>? _contour;

    // Pending stroke with a circular aperture (centre line points)
    private List<SKPoint>? _stroke;
    private double _strokeWidth;

    private bool _stepRepeatOpen;
    private bool _finished;

    public GerberReader(string text)
    {
        _text = text;
        _target = _image.Objects;
    }

    public GerberImage Read()
    {
        var pos = 0;
        var len = _text.Length;
        while (pos < len && !_finished)
        {
            var c = _text[pos];
            if (c == '%')
            {
                var end = _text.IndexOf('%', pos + 1);
                if (end < 0) break;
                HandleExtended(_text.Substring(pos + 1, end - pos - 1));
                pos = end + 1;
            }
            else if (char.IsWhiteSpace(c))
            {
                pos++;
            }
            else
            {
                var end = _text.IndexOf('*', pos);
                if (end < 0) break;
                var word = _text.Substring(pos, end - pos);
                pos = end + 1;
                HandleWord(word);
            }
        }

        FinishAll();
        return _image;
    }

    #region Extended commands

    private void HandleExtended(string block)
    {
        var commands = block.Split('*').Select(s => s.Trim('\r', '\n', ' ', '\t')).Where(s => s.Length > 0).ToList();
        if (commands.Count == 0) return;

        if (commands[0].StartsWith("AM", StringComparison.Ordinal))
        {
            var name = commands[0][2..].Trim();
            _macros[name] = new ApertureMacro(name, commands.Skip(1));
            return;
        }

        foreach (var raw in commands)
        {
            var cmd = RemoveWhitespace(raw);
            if (cmd.Length < 2) continue;
            var code = cmd[..2];
            var body = cmd[2..];
            switch (code)
            {
                case "FS": ParseFormat(body); break;
                case "MO": _unit = body.StartsWith("MM", StringComparison.OrdinalIgnoreCase) ? 1 : 25.4; break;
                case "AD": ParseApertureDefinition(body); break;
                case "AB": HandleBlockAperture(body); break;
                case "SR": HandleStepRepeat(body); break;
                case "LP":
                    FlushStroke();
                    _dark = !body.StartsWith('C');
                    break;
                case "LM":
                    _mirrorX = body.Contains('X');
                    _mirrorY = body.Contains('Y');
                    break;
                case "LR": _rotation = ParseDouble(body); break;
                case "LS": _scale = ParseDouble(body); if (_scale <= 0) _scale = 1; break;
                case "TF": ParseFileAttribute(raw.Trim()[2..]); break;
                case "IP":
                    if (body.StartsWith("NEG", StringComparison.OrdinalIgnoreCase))
                        _image.Warnings.Add("Negative image polarity (%IPNEG) is not supported and was ignored.");
                    break;
                // TA, TO, TD, IN, LN, OF, SF, MI, IR, AS: not needed for rendering
            }
        }
    }

    private void ParseFileAttribute(string body)
    {
        // .FileFunction,Copper,L1,Top
        if (!body.StartsWith(".FileFunction", StringComparison.OrdinalIgnoreCase)) return;
        var comma = body.IndexOf(',');
        if (comma > 0) _image.FileFunction = body[(comma + 1)..];
    }

    private void ParseFormat(string body)
    {
        // e.g. LAX46Y46, TAX25Y25, LAN2X24Y24
        var i = 0;
        while (i < body.Length && body[i] != 'X')
        {
            switch (body[i])
            {
                case 'L': _trailingZerosOmitted = false; break;
                case 'T': _trailingZerosOmitted = true; break;
                case 'D': _trailingZerosOmitted = false; break;
                case 'A': _incremental = false; break;
                case 'I': _incremental = true; break;
            }

            i++;
        }

        var xi = body.IndexOf('X');
        var yi = body.IndexOf('Y');
        if (xi >= 0 && xi + 2 < body.Length)
        {
            _xInt = body[xi + 1] - '0';
            _xDec = body[xi + 2] - '0';
        }

        if (yi >= 0 && yi + 2 < body.Length)
        {
            _yInt = body[yi + 1] - '0';
            _yDec = body[yi + 2] - '0';
        }
    }

    private void ParseApertureDefinition(string body)
    {
        // D10C,0.5X0.2  |  D11RoundRect,0.1X...  |  D12R,1X2
        if (body.Length < 2 || body[0] != 'D') return;
        var i = 1;
        while (i < body.Length && char.IsDigit(body[i])) i++;
        if (!int.TryParse(body.AsSpan(1, i - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) return;

        var rest = body[i..];
        var comma = rest.IndexOf(',');
        var name = comma >= 0 ? rest[..comma] : rest;
        var args = comma >= 0
            ? rest[(comma + 1)..].Split('X', StringSplitOptions.RemoveEmptyEntries).Select(ParseDouble).ToArray()
            : [];

        double A(int k) => k < args.Length ? args[k] : 0;
        double U(int k) => A(k) * _unit;

        // Standard apertures may have a round hole (one parameter) or a rectangular hole (two parameters, deprecated).
        SKPath Hole(SKPath shape, int k) =>
            args.Length > k + 1 ? Geometry.Difference(shape, Geometry.Rect(0, 0, U(k), U(k + 1)))
            : args.Length > k ? Geometry.WithHole(shape, U(k))
            : shape;

        Aperture aperture;
        switch (name)
        {
            case "C":
                aperture = args.Length > 1 && A(1) > 0
                    ? new Aperture { Shape = Hole(Geometry.Circle(0, 0, U(0)), 1) }
                    : new Aperture { Shape = Geometry.Circle(0, 0, U(0)), CircleDiameter = U(0) };
                break;
            case "R":
                aperture = new Aperture { Shape = Hole(Geometry.Rect(0, 0, U(0), U(1)), 2) };
                break;
            case "O":
                aperture = new Aperture { Shape = Hole(Geometry.Obround(U(0), U(1)), 2) };
                break;
            case "P":
                aperture = new Aperture { Shape = Hole(Geometry.RegularPolygon(0, 0, U(0), (int)A(1), A(2)), 3) };
                break;
            default:
                if (_macros.TryGetValue(name, out var macro))
                {
                    aperture = new Aperture { Shape = macro.Build(args, _unit) };
                }
                else
                {
                    _image.Warnings.Add($"Unknown aperture template '{name}' for D{id}.");
                    aperture = new Aperture { Shape = new SKPath() };
                }

                break;
        }

        _apertures[id] = aperture;
    }

    private void HandleBlockAperture(string body)
    {
        FlushStroke();
        if (body.Length == 0)
        {
            PopTarget();
            return;
        }

        if (body[0] != 'D' || !int.TryParse(body.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) return;

        var blockObjects = new List<GraphicObject>();
        PushTarget(blockObjects, objects => _apertures[id] = new Aperture { Block = objects });
    }

    private void HandleStepRepeat(string body)
    {
        FlushStroke();
        if (_stepRepeatOpen)
        {
            PopTarget();
            _stepRepeatOpen = false;
        }

        if (body.Length == 0) return;

        int nx = 1, ny = 1;
        double dx = 0, dy = 0;
        foreach (var (letter, value) in SplitWords(body))
        {
            switch (letter)
            {
                case 'X': nx = (int)ParseDouble(value); break;
                case 'Y': ny = (int)ParseDouble(value); break;
                case 'I': dx = ParseDouble(value) * _unit; break;
                case 'J': dy = ParseDouble(value) * _unit; break;
            }
        }

        if (nx <= 1 && ny <= 1) return;

        _stepRepeatOpen = true;
        PushTarget([], objects =>
        {
            for (var iy = 0; iy < Math.Max(1, ny); iy++)
            for (var ix = 0; ix < Math.Max(1, nx); ix++)
            {
                var m = SKMatrix.CreateTranslation((float)(ix * dx), (float)(iy * dy));
                foreach (var o in objects) _target.Add(o.Transform(m));
            }
        });
    }

    private void PushTarget(List<GraphicObject> objects, Action<List<GraphicObject>> close)
    {
        _targets.Push((objects, close));
        _target = objects;
    }

    private void PopTarget()
    {
        if (_targets.Count == 0) return;
        var (objects, close) = _targets.Pop();
        _target = _targets.Count > 0 ? _targets.Peek().Objects : _image.Objects;
        close(objects);
    }

    #endregion

    #region Word commands

    private void HandleWord(string word)
    {
        word = word.Trim();
        if (word.Length == 0) return;
        if (word.StartsWith("G04", StringComparison.Ordinal) || word.StartsWith("G4 ", StringComparison.Ordinal)) return;

        word = RemoveWhitespace(word);

        string? xs = null, ys = null, iStr = null, jStr = null;
        int? d = null;
        foreach (var (letter, value) in SplitWords(word))
        {
            switch (letter)
            {
                case 'G':
                    HandleG((int)ParseDouble(value));
                    break;
                case 'M':
                    var m = (int)ParseDouble(value);
                    if (m == 2 || m == 0 || m == 1) _finished = m == 2 || m == 0;
                    break;
                case 'D':
                    d = (int)ParseDouble(value);
                    break;
                case 'X': xs = value; break;
                case 'Y': ys = value; break;
                case 'I': iStr = value; break;
                case 'J': jStr = value; break;
            }
        }

        if (d is >= 10)
        {
            if (d != _currentAperture) FlushStroke();
            _currentAperture = d.Value;
            return;
        }

        var hasCoordinates = xs != null || ys != null || iStr != null || jStr != null;
        if (d == null && !hasCoordinates) return;

        var op = d ?? _lastOperation;
        if (op is < 1 or > 3) return;
        _lastOperation = op;

        var nx = xs != null ? ParseCoordinate(xs, _xInt, _xDec) + (_incremental ? _x : 0) : _x;
        var ny = ys != null ? ParseCoordinate(ys, _yInt, _yDec) + (_incremental ? _y : 0) : _y;
        var i = iStr != null ? ParseCoordinate(iStr, _xInt, _xDec) : 0;
        var j = jStr != null ? ParseCoordinate(jStr, _yInt, _yDec) : 0;

        switch (op)
        {
            case 1: Interpolate(nx, ny, i, j); break;
            case 2: Move(); break;
            case 3: Flash(nx, ny); break;
        }

        _x = nx;
        _y = ny;
    }

    private void HandleG(int g)
    {
        switch (g)
        {
            case 1: case 10: case 11: case 12: _interpolation = Interpolation.Linear; break;
            case 2: _interpolation = Interpolation.Clockwise; break;
            case 3: _interpolation = Interpolation.CounterClockwise; break;
            case 36:
                FlushStroke();
                _regionMode = true;
                _contour = null;
                break;
            case 37:
                CloseContour();
                _regionMode = false;
                break;
            case 74: _multiQuadrant = false; break;
            case 75: _multiQuadrant = true; break;
            case 70: _unit = 25.4; break;
            case 71: _unit = 1; break;
            case 90: _incremental = false; break;
            case 91: _incremental = true; break;
        }
    }

    // D02: the current point just moves; whatever was being drawn ends here
    private void Move()
    {
        if (_regionMode)
        {
            CloseContour();
        }
        else
        {
            FlushStroke();
        }
    }

    private void Interpolate(double nx, double ny, double i, double j)
    {
        List<(double X, double Y)> points;
        if (_interpolation == Interpolation.Linear)
        {
            points = [(nx, ny)];
        }
        else
        {
            var clockwise = _interpolation == Interpolation.Clockwise;
            var (cx, cy) = _multiQuadrant ? (_x + i, _y + j) : FindSingleQuadrantCenter(nx, ny, Math.Abs(i), Math.Abs(j), clockwise);
            points = Geometry.ArcPoints(_x, _y, nx, ny, cx, cy, clockwise, _multiQuadrant);
        }

        if (_regionMode)
        {
            _contour ??= [new SKPoint((float)_x, (float)_y)];
            foreach (var p in points) _contour.Add(new SKPoint((float)p.X, (float)p.Y));
            return;
        }

        if (!_apertures.TryGetValue(_currentAperture, out var aperture))
        {
            return;
        }

        if (aperture.CircleDiameter is { } diameter)
        {
            if (_stroke == null || Math.Abs(_strokeWidth - diameter) > 1e-12)
            {
                FlushStroke();
                _stroke = [new SKPoint((float)_x, (float)_y)];
                _strokeWidth = diameter;
            }

            foreach (var p in points) _stroke.Add(new SKPoint((float)p.X, (float)p.Y));
            return;
        }

        if (aperture.Shape == null) return;

        // Non-circular aperture: sweep the (convex) aperture along each segment.
        var outline = Geometry.FlattenPoints(aperture.Shape);
        if (outline.Count == 0) return;
        double px = _x, py = _y;
        foreach (var p in points)
        {
            var hullInput = new List<SKPoint>(outline.Count * 2);
            foreach (var o in outline)
            {
                hullInput.Add(new SKPoint((float)(o.X + px), (float)(o.Y + py)));
                hullInput.Add(new SKPoint((float)(o.X + p.X), (float)(o.Y + p.Y)));
            }

            var hull = Geometry.ConvexHull(hullInput);
            if (hull.Count >= 3) Emit(new GraphicObject(Geometry.Polygon(hull), _dark));
            px = p.X;
            py = p.Y;
        }
    }

    private (double, double) FindSingleQuadrantCenter(double ex, double ey, double i, double j, bool clockwise)
    {
        var best = (_x + i, _y + j);
        var bestError = double.MaxValue;
        foreach (var (si, sj) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
        {
            var cx = _x + si * i;
            var cy = _y + sj * j;
            var r0 = Math.Sqrt((_x - cx) * (_x - cx) + (_y - cy) * (_y - cy));
            var r1 = Math.Sqrt((ex - cx) * (ex - cx) + (ey - cy) * (ey - cy));
            var a0 = Math.Atan2(_y - cy, _x - cx);
            var a1 = Math.Atan2(ey - cy, ex - cx);
            var sweep = clockwise ? a0 - a1 : a1 - a0;
            while (sweep < 0) sweep += 2 * Math.PI;
            while (sweep >= 2 * Math.PI) sweep -= 2 * Math.PI;
            if (sweep > Math.PI / 2 + 1e-6) continue;
            var error = Math.Abs(r0 - r1);
            if (error < bestError)
            {
                bestError = error;
                best = (cx, cy);
            }
        }

        return best;
    }

    private void Flash(double nx, double ny)
    {
        FlushStroke();
        if (!_apertures.TryGetValue(_currentAperture, out var aperture)) return;

        var m = SKMatrix.Identity;
        if (_mirrorX || _mirrorY) m = m.PostConcat(SKMatrix.CreateScale(_mirrorX ? -1 : 1, _mirrorY ? -1 : 1));
        if (_rotation != 0) m = m.PostConcat(SKMatrix.CreateRotationDegrees((float)_rotation));
        if (_scale != 1) m = m.PostConcat(SKMatrix.CreateScale((float)_scale, (float)_scale));
        m = m.PostConcat(SKMatrix.CreateTranslation((float)nx, (float)ny));

        if (aperture.Block != null)
        {
            foreach (var o in aperture.Block) Emit(o.Transform(m, _dark ? o.Dark : !o.Dark));
            return;
        }

        if (aperture.Shape == null || aperture.Shape.IsEmpty) return;
        var path = new SKPath(aperture.Shape);
        path.Transform(in m);
        Emit(new GraphicObject(path, _dark));
    }

    private void CloseContour()
    {
        if (_contour is { Count: >= 3 })
            Emit(new GraphicObject(Geometry.Polygon(_contour), _dark));
        _contour = null;
    }

    private void FlushStroke()
    {
        if (_stroke == null) return;
        var points = _stroke;
        _stroke = null;
        if (points.Count < 2) return;

        var centerLine = Geometry.Polyline(points);
        if (_strokeWidth <= 0)
        {
            Emit(new GraphicObject(centerLine, _dark, hairline: true));
            return;
        }

        var outline = Geometry.StrokeOutline(centerLine, _strokeWidth);
        if (outline != null) Emit(new GraphicObject(outline, _dark));
    }

    private void Emit(GraphicObject obj) => _target.Add(obj);

    private void FinishAll()
    {
        FlushStroke();
        if (_regionMode) CloseContour();
        while (_targets.Count > 0) PopTarget();
    }

    #endregion

    #region Number parsing

    private double ParseCoordinate(string value, int intDigits, int decDigits)
    {
        if (value.Contains('.')) return ParseDouble(value) * _unit;

        var negative = value.StartsWith('-');
        var digits = value.TrimStart('+', '-');
        if (digits.Length == 0) return 0;

        double number;
        if (_trailingZerosOmitted)
        {
            var padded = digits.PadRight(intDigits + decDigits, '0');
            number = long.Parse(padded, CultureInfo.InvariantCulture) / Math.Pow(10, padded.Length - intDigits);
        }
        else
        {
            number = long.Parse(digits, CultureInfo.InvariantCulture) / Math.Pow(10, decDigits);
        }

        return (negative ? -number : number) * _unit;
    }

    private static double ParseDouble(string s)
    {
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static string RemoveWhitespace(string s)
    {
        if (!s.Any(char.IsWhiteSpace)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            if (!char.IsWhiteSpace(c)) sb.Append(c);
        return sb.ToString();
    }

    // Splits "G01X100Y-20D01" into (G,"01"), (X,"100"), (Y,"-20"), (D,"01")
    internal static IEnumerable<(char Letter, string Value)> SplitWords(string s)
    {
        var i = 0;
        while (i < s.Length)
        {
            var letter = s[i];
            if (!char.IsLetter(letter))
            {
                i++;
                continue;
            }

            var start = ++i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == '-' || s[i] == '+')) i++;
            yield return (char.ToUpperInvariant(letter), s[start..i]);
        }
    }

    #endregion
}
