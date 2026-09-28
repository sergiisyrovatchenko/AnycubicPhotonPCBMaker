using System.IO.Compression;
using System.Text;
using AnycubicPhotonPCB.Core.Gerber;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Pcb;

// A source file (loose file or ZIP entry) before parsing
public sealed class SourceFile
{
    public SourceFile(string name, string text)
    {
        Name = name;
        Text = text;
    }

    public string Name { get; }
    public string Text { get; }
    public LayerType DetectedType { get; set; }
    public LayerSide DetectedSide { get; set; }

    public static List<SourceFile> FromPaths(IEnumerable<string> paths)
    {
        var result = new List<SourceFile>();
        foreach (var path in paths)
        {
            if (string.Equals(Path.GetExtension(path), ".zip", StringComparison.OrdinalIgnoreCase))
                result.AddRange(FromZip(path));
            else
                result.Add(new SourceFile(Path.GetFileName(path), ReadText(File.ReadAllBytes(path))));
        }

        Identify(result);
        return result;
    }

    public static List<SourceFile> FromDirectory(string directory)
    {
        return FromPaths(Directory.EnumerateFiles(directory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
    }

    private static IEnumerable<SourceFile> FromZip(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in zip.Entries.OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase))
        {
            if (entry.Length == 0 || entry.FullName.EndsWith('/')) continue;
            using var stream = entry.Open();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            yield return new SourceFile(entry.Name, ReadText(ms.ToArray()));
        }
    }

    private static string ReadText(byte[] data) => Encoding.Latin1.GetString(data);

    private static void Identify(List<SourceFile> files)
    {
        var ids = LayerIdentifier.Identify(files.Select(f => f.Name));
        foreach (var file in files)
        {
            var (type, side) = ids[file.Name];
            var excellon = ExcellonParser.LooksLikeExcellon(file.Text);
            if (!excellon && !LooksLikeGerber(file.Text))
            {
                // Name matches a layer pattern (e.g. "board-F_Cu.svg", "requirements.txt") but the content is not CAM data.
                (type, side) = (LayerType.Unknown, LayerSide.None);
            }
            else if (type is LayerType.Unknown or LayerType.Drawing)
            {
                // Content sniffing for files with generic names
                if (excellon)
                {
                    (type, side) = (LayerType.Drill, LayerSide.All);
                }
                else if (type == LayerType.Drawing && TryGetFileFunction(file.Text) is { } ff && LayerIdentifier.FromFileFunction(ff) is { } fromAttr)
                {
                    (type, side) = fromAttr;
                }
            }

            file.DetectedType = type;
            file.DetectedSide = side;
        }
    }

    private static bool LooksLikeGerber(string text)
    {
        var head = text.Length > 20000 ? text[..20000] : text;
        return head.Contains("%FS", StringComparison.Ordinal)
               || head.Contains("%MO", StringComparison.Ordinal)
               || (head.Contains("D0", StringComparison.Ordinal) && head.Contains('*') && !head.Contains('\0'));
    }

    private static string? TryGetFileFunction(string text)
    {
        const string marker = "%TF.FileFunction,";
        var i = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        var end = text.IndexOf('*', i);
        return end < 0 ? null : text.Substring(i + marker.Length, end - i - marker.Length);
    }
}

public sealed class PcbLayer
{
    public PcbLayer(int id, string fileName, LayerType type, LayerSide side, GerberImage image)
    {
        Id = id;
        FileName = fileName;
        Type = type;
        Side = side;
        Image = image;
    }

    public int Id { get; }
    public string FileName { get; }
    public LayerType Type { get; }
    public LayerSide Side { get; }
    public GerberImage Image { get; }
    public bool IsTopSide => Side == LayerSide.Top; // "Top" layers are flipped with the top flip options, everything else with the bottom ones
    public string DisplayName => $"{Type} ({Side})";
    public override string ToString() => $"{DisplayName} - {FileName}";
}

// A set of parsed layers that share the same board area
public sealed class PcbProject
{
    private PcbProject(List<PcbLayer> layers, SKRect boardBounds)
    {
        Layers = layers;
        BoardBounds = boardBounds;
    }

    public IReadOnlyList<PcbLayer> Layers { get; }

    // Board area in millimetres (Gerber coordinates, Y up). All layers are rendered relative to it
    public SKRect BoardBounds { get; }

    public PcbLayer? Outline => Layers.FirstOrDefault(l => l.Type == LayerType.Outline);

    public IEnumerable<PcbLayer> DrillLayers => Layers.Where(l => l.Type == LayerType.Drill);

    public static PcbProject Load(IEnumerable<SourceFile> files)
    {
        var layers = new List<PcbLayer>();
        var id = 0;
        foreach (var file in files)
        {
            if (file.DetectedType == LayerType.Unknown) continue;

            GerberImage image;
            try
            {
                image = file.DetectedType == LayerType.Drill && ExcellonParser.LooksLikeExcellon(file.Text)
                    ? ExcellonParser.Parse(file.Text)
                    : GerberParser.Parse(file.Text);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to parse '{file.Name}': {ex.Message}", ex);
            }

            if (image.IsEmpty) continue;

            var type = file.DetectedType;
            var side = file.DetectedSide;
            if (type == LayerType.Drawing && LayerIdentifier.FromFileFunction(image.FileFunction) is { } fromAttr)
                (type, side) = fromAttr;

            layers.Add(new PcbLayer(id++, file.Name, type, side, image));
        }

        return new PcbProject(layers, ComputeBoardBounds(layers));
    }

    private static SKRect ComputeBoardBounds(List<PcbLayer> layers)
    {
        var outline = layers.FirstOrDefault(l => l.Type == LayerType.Outline && !l.Image.Bounds.IsEmpty);
        if (outline != null) return outline.Image.Bounds;

        var any = false;
        var bounds = SKRect.Empty;
        foreach (var layer in layers)
        {
            var b = layer.Image.Bounds;
            if (b.Width <= 0 || b.Height <= 0) continue;
            bounds = any ? SKRect.Union(bounds, b) : b;
            any = true;
        }

        return bounds;
    }
}
