using AnycubicPhotonPCB.Core.Gerber;
using AnycubicPhotonPCB.Core.Pcb;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Rendering;

// Drill holes and board outline drawn into a layer on top of its own features. The drill files are all of them:
// plated, non plated, vias. The options are independent of each other:
// SubtractHoles: the holes are cut out of the features, as they will be on the board.
// DrillMarks: holes in bare areas (e.g. non plated mounting holes), which SubtractHoles leaves invisible because
//             there is no copper to cut, get a thin copper ring just inside their edge so they can be drilled.
// Outline: the board outline is drawn as a feature
public sealed record LayerExtras(bool SubtractHoles, bool DrillMarks, bool Outline)
{
    public static readonly LayerExtras None = new(false, false, false);

    public bool IsEmpty => !SubtractHoles && !DrillMarks && !Outline;

    // The copper layers take every option. Soldermask only has holes cut out (its Gerber draws the openings,
    // so marks and the outline make no sense there); the other layers are never changed
    public static LayerExtras For(LayerType type, bool subtractHoles, bool drillMarks, bool outline) => type switch
    {
        LayerType.Copper => new LayerExtras(subtractHoles, drillMarks, outline),
        LayerType.Soldermask when subtractHoles => new LayerExtras(true, false, false),
        _ => None,
    };
}

// Drill holes and board outline of a project, rasterised for one transform and size.
// Only the parts the extras need are rendered
public sealed class LayerOverlay
{
    // Width of the copper ring drawn inside a hole in a bare area (DrillMarks), in millimetres.
    // Its outer diameter is the hole diameter
    public const float MarkRingMm = 0.1f;

    private readonly List<SKRect> _holeBounds;
    private readonly SKMatrix _transform;
    private readonly int _width;
    private readonly int _height;
    private readonly bool _antialias;

    private LayerOverlay(Mask? holes, Mask? outline, List<SKRect> holeBounds, SKMatrix transform, int width, int height, bool antialias)
    {
        Holes = holes;
        Outline = outline;
        _holeBounds = holeBounds;
        _transform = transform;
        _width = width;
        _height = height;
        _antialias = antialias;
    }

    public Mask? Holes { get; }

    public Mask? Outline { get; }

    // A layer rasterised with its extras drawn in: the sequence every export, preview and file thumbnail uses
    public static Mask RenderLayer(PcbProject project, PcbLayer layer, LayerExtras extras, SKMatrix transform, int width, int height, bool antialias)
    {
        var features = LayerRasterizer.Render(layer.Image, transform, width, height, antialias);
        if (!extras.IsEmpty)
            Render(project, extras, transform, width, height, antialias).Apply(features, extras, antialias);
        return features;
    }

    public static LayerOverlay Render(PcbProject project, LayerExtras extras, SKMatrix transform, int width, int height, bool antialias)
    {
        Mask? holes = null, outline = null;
        var holeBounds = new List<SKRect>();
        foreach (var drill in project.DrillLayers)
        {
            if (extras.SubtractHoles)
            {
                var drillMask = LayerRasterizer.Render(drill.Image, transform, width, height, antialias);
                if (holes == null) holes = drillMask;
                else holes.UnionWith(drillMask);
            }

            if (extras.DrillMarks)
                holeBounds.AddRange(drill.Image.Objects.Where(o => o.Dark).Select(o => o.Path.Bounds).Where(b => b.Width > 0 && b.Height > 0));
        }

        if (extras.Outline && project.Outline is { } board)
            outline = LayerRasterizer.Render(board.Image, transform, width, height, antialias);

        return new LayerOverlay(holes, outline, holeBounds, transform, width, height, antialias);
    }

    // Draws the extras into features (255 = feature). Anti-aliased masks are combined by coverage,
    // printer masks (antialias false) with a 50% threshold
    public void Apply(Mask features, LayerExtras extras, bool antialias)
    {
        if (extras.Outline && Outline != null) features.UnionWith(Outline);

        // Bare holes are found before any hole is cut (a cut hole in a pad would look bare), the rings drawn last
        var rings = extras.DrillMarks ? BareHoleRings(features) : null;
        if (extras.SubtractHoles && Holes != null) Cut(features, Holes, antialias);
        if (rings != null) features.UnionWith(rings);
    }

    // Holes whose centre has no copper in this layer (mounting holes and the like) get a ring MarkRingMm wide just
    // inside the hole edge: its outer diameter is the hole diameter. Holes in pads keep just the pad.
    // Round holes get a circular ring, slots a capsule shaped one following their bounds
    private Mask? BareHoleRings(Mask features)
    {
        var image = new GerberImage();
        foreach (var b in _holeBounds)
        {
            var centre = _transform.MapPoint(b.MidX, b.MidY);
            var x = (int)centre.X;
            var y = (int)centre.Y;
            if (x < 0 || y < 0 || x >= features.Width || y >= features.Height || features[x, y] >= 128) continue;

            using var builder = new SKPathBuilder { FillType = SKPathFillType.EvenOdd };
            var radius = Math.Min(b.Width, b.Height) / 2;
            builder.AddRoundRect(b, radius, radius);
            var inner = SKRect.Inflate(b, -MarkRingMm, -MarkRingMm);
            if (inner.Width > 0 && inner.Height > 0)
            {
                var innerRadius = Math.Min(inner.Width, inner.Height) / 2;
                builder.AddRoundRect(inner, innerRadius, innerRadius);
            }

            image.Objects.Add(new GraphicObject(builder.Detach(), dark: true));
        }

        return image.IsEmpty ? null : LayerRasterizer.Render(image, _transform, _width, _height, _antialias);
    }

    private static void Cut(Mask features, Mask holes, bool antialias)
    {
        if (antialias) LayerRasterizer.SubtractSoft(features, holes);
        else LayerRasterizer.Subtract(features, holes);
    }
}
