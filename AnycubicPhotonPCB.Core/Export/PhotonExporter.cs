using AnycubicPhotonPCB.Core.Formats;
using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;
using SkiaSharp;

namespace AnycubicPhotonPCB.Core.Export;

// Builds exposure masks and printer files. Port of photonic-etcher's render-to-photon.ts.
// The placement on the screen lives in ScreenLayout
public static class PhotonExporter
{
    // Layers exported at the same time. Each export of a high resolution printer holds a few full screen frames
    // (about 150 MB for the Mono 4 Ultra), so this is capped rather than using every core
    private static readonly int MaxParallelExports = Math.Clamp(Environment.ProcessorCount, 1, 3);

    // Exports the layers in parallel; the result keeps the order of requests.
    // progress receives the number of finished layers (reports may arrive out of order)
    public static List<ExportedFile> ExportAll(PcbProject project, IReadOnlyList<LayerExportRequest> requests, ExportOptions options, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var names = BuildFileNames(requests.Select(r => r.Layer.FileName).ToList(), options.Printer.FileExtension);
        var result = new ExportedFile[requests.Count];
        var finished = 0;
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelExports, CancellationToken = cancellationToken };

        try
        {
            Parallel.For(0, requests.Count, parallel, i =>
            {
                result[i] = Export(project, requests[i], options, names[i]);
                progress?.Report(Interlocked.Increment(ref finished));
            });
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count > 0)
        {
            // Surface the real error (e.g. an unsupported layer) instead of "One or more errors occurred"
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
        }

        return [.. result];
    }

    public static ExportedFile Export(PcbProject project, LayerExportRequest request, ExportOptions options, string fileName)
    {
        var printer = options.Printer;
        var flips = options.FlipsFor(request.Layer);
        var warnings = new List<string>();

        var rotation = options.Rotation;
        var board = BuildBoardMask(project, request, printer.XyRes, flips, rotation);
        var view = ScreenLayout.ComposeScreen(board, request.Invert, options, out var fits);
        if (!fits) warnings.Add("The board does not fit on the printer screen at the selected position and will be clipped.");

        var native = ScreenLayout.ToNativeFrame(view, printer);

        byte[] data;
        if (printer.Kind == PrinterFileKind.PhotonWorkshopZip)
        {
            var modelName = Path.GetFileNameWithoutExtension(fileName);
            data = Pm4uWriter.Write(
                printer,
                native,
                request.ExposureSeconds,
                modelName,
                (w, h, bg) => RenderPm4uPreview(project, request, flips, rotation, w, h, bg));
        }
        else
        {
            var preview = RenderPreviewRgba(project, request, flips, rotation, printer.PreviewResolution.Width, printer.PreviewResolution.Height);
            data = PhotonWorkshopWriter.Write(printer, native, preview, request.ExposureSeconds);
        }

        var file = new ExportedFile
        {
            Layer = request.Layer,
            FileName = fileName,
            Data = data,
        };
        file.Warnings.AddRange(warnings);
        return file;
    }

    // "board-F_Cu.gbr" -> "board-F_Cu.pm4u"; duplicates get a numeric suffix
    public static List<string> BuildFileNames(IReadOnlyList<string> sourceNames, string extension)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var source in sourceNames)
        {
            var stem = Path.GetFileNameWithoutExtension(source);
            if (string.IsNullOrEmpty(stem)) stem = source;
            counts.TryGetValue(stem, out var n);
            counts[stem] = n + 1;
            result.Add(n == 0 ? $"{stem}.{extension}" : $"{stem}_{n + 1}.{extension}");
        }

        return result;
    }

    // Renders the layer at printer resolution over the board rectangle: 255 where the screen must be
    // exposed, 0 where it must stay dark
    public static Mask BuildBoardMask(PcbProject project, LayerExportRequest request, double pixelSize, SideFlips flips, BoardRotation rotation = BoardRotation.None)
    {
        var bounds = project.BoardBounds;
        var (width, height) = ScreenLayout.BoardPixelSize(bounds, pixelSize, rotation);
        var transform = LayerRasterizer.BoardToPixels(bounds, SKRect.Create(0, 0, width, height), flips.Horizontal, flips.Vertical, rotation);
        var features = LayerOverlay.RenderLayer(project, request.Layer, request.Extras, transform, width, height, antialias: false);

        // Normal: features stay dark on an exposed background. Inverted: features are exposed.
        var data = features.Data;
        for (var i = 0; i < data.Length; i++)
        {
            var feature = data[i] >= 128;
            data[i] = feature == request.Invert ? (byte)255 : (byte)0;
        }

        return features;
    }

    // Anti-aliased rendering of the processed layer (after the extras and inversion) fitted into the given size
    public static Mask RenderLayerFit(PcbProject project, LayerExportRequest request, SideFlips flips, BoardRotation rotation, int width, int height, float margin)
    {
        var transform = LayerRasterizer.BoardToPixelsFit(project.BoardBounds, width, height, margin, flips.Horizontal, flips.Vertical, rotation);
        var mask = LayerOverlay.RenderLayer(project, request.Layer, request.Extras, transform, width, height, antialias: true);
        if (request.Invert) return mask;

        // Non-inverted: exposed background inside the board area, dark features.
        var mapped = transform.MapRect(project.BoardBounds);
        var boardArea = SKRect.Create(mapped.Left, mapped.Top, mapped.Width, mapped.Height);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var i = y * width + x;
            mask.Data[i] = boardArea.Contains(x + 0.5f, y + 0.5f) ? (byte)(255 - mask.Data[i]) : (byte)0;
        }

        return mask;
    }

    private static byte[] RenderPreviewRgba(PcbProject project, LayerExportRequest request, SideFlips flips, BoardRotation rotation, int width, int height)
    {
        var mask = RenderLayerFit(project, request, flips, rotation, width, height, 4);
        var rgba = new byte[width * height * 4];
        for (var i = 0; i < mask.Data.Length; i++)
        {
            var v = mask.Data[i];
            rgba[i * 4] = v;
            rgba[i * 4 + 1] = v;
            rgba[i * 4 + 2] = v;
            rgba[i * 4 + 3] = 255;
        }

        return rgba;
    }

    private static byte[] RenderPm4uPreview(PcbProject project, LayerExportRequest request, SideFlips flips, BoardRotation rotation, int width, int height, SKColor background)
    {
        var mask = RenderLayerFit(project, request, flips, rotation, width, height, Math.Max(4, width / 16f));
        var model = new SKColor(205, 205, 205);
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var pixels = bitmap.GetPixelSpan();
        for (var i = 0; i < mask.Data.Length; i++)
        {
            var a = mask.Data[i] / 255f;
            pixels[i * 4] = (byte)(background.Red + (model.Red - background.Red) * a);
            pixels[i * 4 + 1] = (byte)(background.Green + (model.Green - background.Green) * a);
            pixels[i * 4 + 2] = (byte)(background.Blue + (model.Blue - background.Blue) * a);
            pixels[i * 4 + 3] = 255;
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }
}
