using AnycubicPhotonPCB.Core.Export;
using System.Buffers.Binary;
using System.Text;

namespace AnycubicPhotonPCB.Core.Formats;

// Writes single-layer binary Photon Workshop files (file versions 1, 515, 516 and 517).
// Versions 1 to 516 are a direct port of photonic-etcher's build-photon-file.ts. Version 517 (Photon Mono 4) follows
// a file exported by Photon Workshop 4.1: the 516 layout plus a SOFTWARE and a MODEL section and a few new fields
public static class PhotonWorkshopWriter
{
    private const float LayerHeight = 0.050f;

    // Section sizes in bytes
    private const int ColourTableSize = 28;
    private const int LayerDefSize = 20 + 32; // section head and one layer
    private const int ExtraSize = 72;
    private const int MachineSize = 156;
    private const int SoftwareSize = 164;
    private const int ModelSize = 48;

    // What a file version contains. Version 1 has a shorter preview trailer and no colour table; 516 adds the EXTRA
    // and MACHINE sections, 517 the SOFTWARE and MODEL sections
    private sealed record Layout(int Version, int HeaderAddr, uint HeaderLength, int PreviewAddr)
    {
        public bool IsVersion1 => Version == 1;

        public bool HasMachine => Version >= 516;

        public bool HasSoftwareAndModel => Version >= 517;

        public static Layout Of(int version) => version switch
        {
            517 => new(517, 0x38, 92, 0xA4),
            516 => new(516, 0x34, 84, 0x98),
            _ => new(version, 0x30, 80, 0x90),
        };
    }

    // Start of every section in the file; the sections are written in this order
    private readonly record struct Addresses(int Preview, int PreviewSize, int ColourTable, int LayerDef, int Extra, int Machine, int Software, int Model, int LayerData)
    {
        public static Addresses Of(Layout layout, int previewPixels)
        {
            var previewSize = previewPixels * 2 + (layout.IsVersion1 ? 12 : 28);
            var colourTable = layout.PreviewAddr + previewSize;
            var layerDef = colourTable + (layout.IsVersion1 ? 16 : ColourTableSize);
            var extra = layerDef + LayerDefSize;
            var machine = extra + ExtraSize;
            var software = machine + MachineSize;
            var model = software + SoftwareSize;
            var layerData = layout.HasSoftwareAndModel ? model + ModelSize : layout.HasMachine ? software : extra;
            return new(layout.PreviewAddr, previewSize, colourTable, layerDef, extra, machine, software, model, layerData);
        }
    }

    // layerPixels: Native frame, ResolutionX * ResolutionY bytes, 255 = exposed.
    // previewRgba: Preview image as RGBA bytes of the printer preview resolution
    public static byte[] Write(PrinterModel printer, ReadOnlySpan<byte> layerPixels, ReadOnlySpan<byte> previewRgba, float exposureTime)
    {
        if (layerPixels.Length != printer.ResolutionX * printer.ResolutionY)
            throw new ArgumentException("Layer size does not match printer resolution.", nameof(layerPixels));

        var layout = Layout.Of(printer.FileVersion.Version);
        var layerData = printer.Encoding == PhotonEncoding.Rle4 ? PhotonRle.EncodeRle4(layerPixels) : PhotonRle.EncodeRle(layerPixels);
        var (previewW, previewH) = printer.PreviewResolution;
        var at = Addresses.Of(layout, previewW * previewH);
        var stats = layout.HasSoftwareAndModel ? ExposedArea(printer, layerPixels) : default;

        var output = new byte[at.LayerData + layerData.Length];
        var span = output.AsSpan();

        WriteFileTable(span, layout, at, printer);
        WriteHeader(span, layout, printer, exposureTime);
        WritePreview(span, layout, at, previewW, previewH, previewRgba);
        WriteLayerDef(span, at, layerData.Length, exposureTime, stats.Count);
        if (layout.HasMachine)
        {
            WriteExtra(span, at.Extra);
            WriteMachine(span, layout, at.Machine, printer);
        }

        if (layout.HasSoftwareAndModel)
        {
            WriteSoftware(span, at.Software);
            WriteModel(span, at.Model, stats);
        }

        layerData.CopyTo(span[at.LayerData..]);
        return output;
    }

    // "ANYCUBIC" mark, version and the addresses of the sections
    private static void WriteFileTable(Span<byte> o, Layout layout, Addresses at, PrinterModel printer)
    {
        Name(o, 0, "ANYCUBIC");
        U32(o, 12, (uint)layout.Version);
        U32(o, 16, (uint)printer.FileVersion.AreaNumber);
        U32(o, 20, (uint)layout.HeaderAddr);
        if (layout.HasSoftwareAndModel) U32(o, 24, (uint)at.Software);
        U32(o, 28, (uint)at.Preview);
        U32(o, 32, (uint)(layout.IsVersion1 ? at.LayerDef : at.ColourTable));
        U32(o, 36, (uint)at.LayerDef);
        U32(o, 40, (uint)(layout.IsVersion1 ? 0 : at.Extra)); // end of the layer definition (EXTRA from 516 on)
        if (layout.HasMachine)
        {
            U32(o, 44, (uint)at.Machine);
            U32(o, 48, (uint)at.LayerData);
        }
        else
        {
            U32(o, 44, (uint)at.LayerData);
        }

        if (layout.HasSoftwareAndModel) U32(o, 52, (uint)at.Model);
    }

    private static void WriteHeader(Span<byte> o, Layout layout, PrinterModel printer, float exposureTime)
    {
        var h = layout.HeaderAddr;
        Name(o, h, "HEADER");
        U32(o, h + 12, layout.HeaderLength);
        F32(o, h + 16, (float)(printer.XyRes * 1000));
        F32(o, h + 20, LayerHeight);
        F32(o, h + 24, 0.0f); // global exposure
        F32(o, h + 28, 0.0f); // light-off
        F32(o, h + 32, exposureTime); // bottom exposure
        F32(o, h + 36, 1); // bottom layer count
        F32(o, h + 40, 0.0f); // lift height
        F32(o, h + 44, 4.0f); // lift speed
        F32(o, h + 48, 4.0f); // retract speed
        F32(o, h + 52, 0.0f); // volume
        U32(o, h + 56, 1); // anti-alias
        U32(o, h + 60, (uint)printer.ResolutionX);
        U32(o, h + 64, (uint)printer.ResolutionY);
        F32(o, h + 68, 1.04f); // weight
        F32(o, h + 72, 1.04f); // price
        U32(o, h + 76, layout.IsVersion1 ? 32u : 36u); // resin type
        U32(o, h + 80, 0); // per layer settings
        U32(o, h + 84, layout.Version switch
        {
            1 => 0u,
            517 => (uint)Math.Ceiling(exposureTime + 5), // print time in seconds, as Photon Workshop 4.1 writes it
            516 => 2060u,
            _ => 2138u,
        });
        U32(o, h + 88, 0); // transition layers
        U32(o, h + 92, 0);
        if (layout.HasMachine) U32(o, h + 96, 0); // disable TSMC (advanced mode)
        if (layout.HasSoftwareAndModel)
        {
            U32(o, h + 100, 0); // grey level
            U32(o, h + 104, 0); // blur level
        }
    }

    private static void WritePreview(Span<byte> o, Layout layout, Addresses at, int width, int height, ReadOnlySpan<byte> rgba)
    {
        var p = at.Preview;
        Name(o, p, "PREVIEW");
        U32(o, p + 12, (uint)at.PreviewSize);
        U32(o, p + 16, (uint)width);
        U32(o, p + 20, layout.IsVersion1 ? 42u : 120u); // '*' or 'x'
        U32(o, p + 24, (uint)height);

        var pixels = width * height;
        for (var i = 0; i < pixels && i * 4 + 3 < rgba.Length; i++)
        {
            // Green channel only; enabling red/blue distorts the thumbnail on the printer (see photonic-etcher).
            var pixel = (ushort)((rgba[i * 4 + 1] >> 2) << 5);
            BinaryPrimitives.WriteUInt16LittleEndian(o[(p + 28 + i * 2)..], pixel);
        }

        if (layout.IsVersion1) return;

        // Colour table of the layer images
        var c = at.ColourTable;
        U32(o, c + 4, 16);
        U32(o, c + 8, 0xFFFFFFFF);
        U32(o, c + 12, 0xFFFFFFFF);
        U32(o, c + 16, 0xFFFFFFFF);
        U32(o, c + 20, 0xFFFFFFFF);
    }

    // exposedPixels: written by version 517, zero before
    private static void WriteLayerDef(Span<byte> o, Addresses at, int layerDataLength, float exposureTime, int exposedPixels)
    {
        var d = at.LayerDef;
        Name(o, d, "LAYERDEF");
        U32(o, d + 12, 4 + 32);
        U32(o, d + 16, 1); // layer count

        var l0 = d + 20;
        U32(o, l0, (uint)at.LayerData);
        U32(o, l0 + 4, (uint)layerDataLength);
        F32(o, l0 + 8, 0.0f);  // lift height
        F32(o, l0 + 12, 4.0f); // lift speed
        F32(o, l0 + 16, exposureTime);
        F32(o, l0 + 20, LayerHeight);
        U32(o, l0 + 24, (uint)exposedPixels);
    }

    // Lift settings of the bottom and normal layers (two stages each); no lift, as the single layer is not printed
    private static void WriteExtra(Span<byte> o, int e)
    {
        Name(o, e, "EXTRA");
        U32(o, e + 12, 24);
        U32(o, e + 16, 2);
        F32(o, e + 24, 4.0f);
        F32(o, e + 28, 4.0f);
        F32(o, e + 36, 4.0f);
        F32(o, e + 40, 4.0f);
        U32(o, e + 44, 2);
        F32(o, e + 52, 4.0f);
        F32(o, e + 56, 4.0f);
        F32(o, e + 64, 4.0f);
        F32(o, e + 68, 4.0f);
    }

    private static void WriteMachine(Span<byte> o, Layout layout, int m, PrinterModel printer)
    {
        Name(o, m, "MACHINE");
        U32(o, m + 12, MachineSize);

        var name = Encoding.UTF8.GetBytes(printer.MachineName ?? printer.Name);
        name.AsSpan(0, Math.Min(name.Length, 96)).CopyTo(o[(m + 16)..]);
        Name(o, m + 112, "pw0Img");
        if (layout.HasSoftwareAndModel)
        {
            U32(o, m + 128, 16); // max anti-aliasing level
            U32(o, m + 132, 7);  // property fields
        }

        var dims = printer.PhysicalDimensions ?? throw new InvalidOperationException($"{printer.Name} requires physical dimensions.");
        F32(o, m + 136, dims.Width);
        F32(o, m + 140, dims.Height);
        F32(o, m + 144, dims.Z);
        U32(o, m + 148, (uint)layout.Version);
        U32(o, m + 152, 6506241);
    }

    // No section name, just fixed length strings as written by Photon Workshop 4.1
    private static void WriteSoftware(Span<byte> o, int s)
    {
        Name(o, s, "ANYCUBIC-PC");
        U32(o, s + 32, SoftwareSize);
        Name(o, s + 36, "4.1.0");
        Name(o, s + 68, "win-x64CloudPws4.x");
        Name(o, s + 132, "3.3-CoreProfile");
    }

    // Bounds of the exposed area in millimetres from the screen centre, Z = the single layer
    private static void WriteModel(Span<byte> o, int m, AreaStats stats)
    {
        Name(o, m, "MODEL");
        F32(o, m + 16, stats.MinX);
        F32(o, m + 20, stats.MinY);
        F32(o, m + 28, stats.MaxX);
        F32(o, m + 32, stats.MaxY);
        F32(o, m + 36, LayerHeight);
    }

    private readonly record struct AreaStats(int Count, float MinX, float MinY, float MaxX, float MaxY);

    // Exposed pixel count and the bounds of the exposed pixels in millimetres from the screen centre
    private static AreaStats ExposedArea(PrinterModel printer, ReadOnlySpan<byte> pixels)
    {
        int w = printer.ResolutionX, h = printer.ResolutionY;
        int count = 0, x0 = w, x1 = -1, y0 = h, y1 = -1;
        for (var y = 0; y < h; y++)
        {
            var row = pixels.Slice(y * w, w);
            var first = row.IndexOfAnyExcept((byte)0);
            if (first < 0) continue;
            var last = row.LastIndexOfAnyExcept((byte)0);
            x0 = Math.Min(x0, first);
            x1 = Math.Max(x1, last);
            y0 = Math.Min(y0, y);
            y1 = y;
            count += w - row.Count((byte)0);
        }

        if (count == 0) return default;
        var px = printer.XyRes;
        return new AreaStats(count,
            (float)((x0 - w / 2.0) * px), (float)((y0 - h / 2.0) * px),
            (float)((x1 + 1 - w / 2.0) * px), (float)((y1 + 1 - h / 2.0) * px));
    }

    // Section and string names: ASCII into the zero filled buffer (the rest of the fixed size field stays zero)
    private static void Name(Span<byte> o, int offset, string name) => Encoding.ASCII.GetBytes(name).CopyTo(o[offset..]);

    private static void U32(Span<byte> s, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(s[offset..], value);

    private static void F32(Span<byte> s, int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(s[offset..], value);
}
