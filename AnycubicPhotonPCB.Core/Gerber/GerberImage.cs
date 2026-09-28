using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Gerber;

// A single filled shape in board space (millimetres, Y axis pointing up as in Gerber).
// Dark objects add material, clear objects erase everything drawn before them
public sealed class GraphicObject
{
    public GraphicObject(SKPath path, bool dark, bool hairline = false)
    {
        Path = path;
        Dark = dark;
        Hairline = hairline;
    }

    public SKPath Path { get; }

    public bool Dark { get; set; }

    // Zero-width stroke; rendered as a 1 px line
    public bool Hairline { get; }

    public GraphicObject Transform(SKMatrix matrix, bool? dark = null)
    {
        var path = new SKPath(Path);
        path.Transform(in matrix);
        return new GraphicObject(path, dark ?? Dark, Hairline);
    }
}

// Parsed contents of a Gerber or Excellon file as an ordered list of filled shapes
public sealed class GerberImage
{
    private SKRect? _bounds;

    public List<GraphicObject> Objects { get; } = [];

    // Value of the X2 .FileFunction attribute, if present (e.g. "Copper,L1,Top")
    public string? FileFunction { get; set; }

    public List<string> Warnings { get; } = [];

    public bool IsEmpty => Objects.Count == 0;

    // Bounding box of all dark objects in millimetres. Empty rectangle when there is nothing to draw
    public SKRect Bounds => _bounds ??= ComputeBounds();

    private SKRect ComputeBounds()
    {
        var any = false;
        var result = SKRect.Empty;
        foreach (var obj in Objects)
        {
            if (!obj.Dark) continue;
            var b = obj.Path.Bounds;
            if (b.Width <= 0 && b.Height <= 0 && !obj.Hairline) continue;
            result = any ? SKRect.Union(result, b) : b;
            any = true;
        }

        return result;
    }
}
