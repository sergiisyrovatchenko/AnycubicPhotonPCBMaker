using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Gerber;

internal static class Geometry
{
    // Maximum deviation of a polyline from the true arc, in millimetres
    private const double ArcTolerance = 0.0005;

    public static SKPath Circle(double cx, double cy, double diameter)
    {
        using var builder = new SKPathBuilder();
        if (diameter > 0) builder.AddCircle((float)cx, (float)cy, (float)(diameter / 2));
        return builder.Detach();
    }

    public static SKPath Rect(double cx, double cy, double width, double height)
    {
        using var builder = new SKPathBuilder();
        if (width > 0 && height > 0)
            builder.AddRect(new SKRect((float)(cx - width / 2), (float)(cy - height / 2), (float)(cx + width / 2), (float)(cy + height / 2)));
        return builder.Detach();
    }

    public static SKPath Obround(double width, double height)
    {
        using var builder = new SKPathBuilder();
        if (width > 0 && height > 0)
        {
            var r = (float)(Math.Min(width, height) / 2);
            builder.AddRoundRect(new SKRect((float)(-width / 2), (float)(-height / 2), (float)(width / 2), (float)(height / 2)), r, r);
        }

        return builder.Detach();
    }

    public static SKPath RegularPolygon(double cx, double cy, double diameter, int vertices, double rotationDeg)
    {
        if (vertices < 3 || diameter <= 0) return new SKPath();
        var r = diameter / 2;
        var points = new SKPoint[vertices];
        for (var i = 0; i < vertices; i++)
        {
            var a = (rotationDeg + 360.0 * i / vertices) * Math.PI / 180;
            points[i] = new SKPoint((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a)));
        }

        return Polygon(points);
    }

    public static SKPath Polygon(IReadOnlyList<SKPoint> points)
    {
        using var builder = new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
        if (points.Count >= 3) builder.AddPoly(points.ToArray(), true);
        return builder.Detach();
    }

    // Open polyline (used as a stroke centre line)
    public static SKPath Polyline(IReadOnlyList<SKPoint> points)
    {
        using var builder = new SKPathBuilder();
        if (points.Count > 0) builder.AddPoly(points.ToArray(), false);
        return builder.Detach();
    }

    // Outline of a stroke with round caps and joins, or null when nothing is produced
    public static SKPath? StrokeOutline(SKPath centerLine, double width)
    {
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)width,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };
        using var builder = new SKPathBuilder();
        // Resolution scale > 1 keeps round caps smooth: coordinates are in millimetres.
        return paint.GetFillPath(centerLine, builder, SKRect.Empty, SKMatrix.CreateScale(100, 100)) ? builder.Detach() : null;
    }

    // Adds a hole to an aperture shape (standard apertures C/R/O/P allow a centred round hole)
    public static SKPath WithHole(SKPath shape, double holeDiameter)
    {
        if (holeDiameter <= 0) return shape;
        var result = shape.Op(Circle(0, 0, holeDiameter), SKPathOp.Difference);
        return result ?? shape;
    }

    public static SKPath Union(SKPath a, SKPath b)
    {
        if (a.IsEmpty) return new SKPath(b);
        if (b.IsEmpty) return a;
        var result = a.Op(b, SKPathOp.Union);
        if (result != null) return result;
        using var builder = new SKPathBuilder();
        builder.AddPath(a);
        builder.AddPath(b);
        return builder.Detach();
    }

    public static SKPath Difference(SKPath a, SKPath b)
    {
        if (a.IsEmpty || b.IsEmpty) return a;
        return a.Op(b, SKPathOp.Difference) ?? a;
    }

    // Interpolates the points of a circular arc (excluding the start point, including the end point)
    public static List<(double X, double Y)> ArcPoints(double sx, double sy, double ex, double ey, double cx, double cy, bool clockwise, bool fullCircleWhenClosed)
    {
        var r = Math.Sqrt((sx - cx) * (sx - cx) + (sy - cy) * (sy - cy));
        var a0 = Math.Atan2(sy - cy, sx - cx);
        var a1 = Math.Atan2(ey - cy, ex - cx);
        var sweep = clockwise ? a0 - a1 : a1 - a0;
        while (sweep < 0) sweep += 2 * Math.PI;
        while (sweep > 2 * Math.PI) sweep -= 2 * Math.PI;
        var closed = Math.Abs(sx - ex) < 1e-9 && Math.Abs(sy - ey) < 1e-9;
        if (closed) sweep = fullCircleWhenClosed ? 2 * Math.PI : 0;
        else if (sweep < 1e-12) sweep = 2 * Math.PI;

        var result = new List<(double, double)>();
        if (r <= 1e-9 || sweep <= 0)
        {
            result.Add((ex, ey));
            return result;
        }

        var step = r > ArcTolerance ? 2 * Math.Acos(1 - ArcTolerance / r) : Math.PI / 4;
        step = Math.Min(step, Math.PI / 16);
        var n = Math.Max(1, (int)Math.Ceiling(sweep / step));
        var dir = clockwise ? -1 : 1;
        for (var k = 1; k < n; k++)
        {
            var a = a0 + dir * sweep * k / n;
            result.Add((cx + r * Math.Cos(a), cy + r * Math.Sin(a)));
        }

        result.Add((ex, ey));
        return result;
    }

    // Monotone chain convex hull
    public static List<SKPoint> ConvexHull(List<SKPoint> points)
    {
        var pts = points.Distinct().OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        if (pts.Count < 3) return pts;
        var hull = new SKPoint[pts.Count * 2];
        var k = 0;
        static float Cross(SKPoint o, SKPoint a, SKPoint b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        foreach (var p in pts)
        {
            while (k >= 2 && Cross(hull[k - 2], hull[k - 1], p) <= 0) k--;
            hull[k++] = p;
        }

        for (int i = pts.Count - 2, t = k + 1; i >= 0; i--)
        {
            var p = pts[i];
            while (k >= t && Cross(hull[k - 2], hull[k - 1], p) <= 0) k--;
            hull[k++] = p;
        }

        return hull.Take(k - 1).ToList();
    }

    // Returns the vertices of a path flattened to line segments
    public static List<SKPoint> FlattenPoints(SKPath path)
    {
        var result = new List<SKPoint>();
        using var iter = path.CreateRawIterator();
        var pts = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iter.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    result.Add(pts[0]);
                    break;
                case SKPathVerb.Line:
                    result.Add(pts[1]);
                    break;
                case SKPathVerb.Quad:
                case SKPathVerb.Conic:
                    result.Add(pts[1]);
                    result.Add(pts[2]);
                    break;
                case SKPathVerb.Cubic:
                    result.Add(pts[1]);
                    result.Add(pts[2]);
                    result.Add(pts[3]);
                    break;
            }
        }

        return result;
    }
}
