using AnycubicPhotonPCB.Core.Export;
using SkiaSharp;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AnycubicPhotonPCB.Core.Formats;

// Writes single layer Photon Workshop 4 ZIP files (.pm4u). The layer is the native Photon Workshop 4 vector
// layer (layer_0.pwszImg), as produced by Photon Workshop itself.
// Machine and resin settings come from a template exported by Photon Workshop for the Photon Mono 4 Ultra
public static class Pm4uWriter
{
    private const float LayerHeight = 0.05f;
    private const string ResourcePrefix = "AnycubicPhotonPCB.Core.Resources.Pm4u.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Delegate that renders a preview PNG of the given size on the given background colour
    public delegate byte[] PreviewRenderer(int width, int height, SKColor background);

    // layerPixels: Native frame (ResolutionX * ResolutionY, 255 = exposed).
    // modelName: Name shown by the printer (usually the file name without extension)
    public static byte[] Write(
        PrinterModel printer,
        ReadOnlySpan<byte> layerPixels,
        float exposureTime,
        string modelName,
        PreviewRenderer renderPreview)
    {
        var width = printer.ResolutionX;
        var height = printer.ResolutionY;
        if (layerPixels.Length != width * height)
            throw new ArgumentException("Layer size does not match printer resolution.", nameof(layerPixels));

        var layerData = PwszLayerEncoder.Encode(layerPixels, width, height, printer.XyRes, out var stats);

        var settings = JsonNode.Parse(ReadResource("anycubic_photon_resins.pwsp"))!;
        var machine = settings["machine_type"]!;
        var resin = FindActiveResin(settings);
        var slicePara = resin?["slicepara"];
        var liftHeight = slicePara?["zup_height"]?.GetValue<double>() ?? 5.0;
        var liftSpeed = slicePara?["zup_speed"]?.GetValue<double>() ?? 8.0;
        if (slicePara != null)
        {
            // The single layer is a bottom layer, so the bottom exposure is the one that counts.
            slicePara["bott_time"] = exposureTime;
            slicePara["bott_time_dual"] = exposureTime;
            slicePara["exposure_time"] = exposureTime;
        }

        var density = resin?["property"]?["density"]?.GetValue<double>() ?? 1.2;
        var price = resin?["property"]?["price"]?.GetValue<double>() ?? 0;
        var bottleVolume = resin?["property"]?["volume"]?.GetValue<double>() ?? 1000;

        var volumeMl = stats.AreaMm2 * LayerHeight / 1000.0;
        var weightG = volumeMl * density;
        var cost = bottleVolume > 0 ? volumeMl * price / bottleVolume : 0;
        var printTime = (int)Math.Ceiling(exposureTime + 4);

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddText(zip, "anycubic_photon_resins.pwsp", settings.ToJsonString(JsonOptions));
            AddText(zip, "print_info.json", BuildPrintInfo(cost, printTime, volumeMl, weightG));
            AddText(zip, "software_info.conf", Encoding.UTF8.GetString(ReadResource("software_info.conf")));

            AddPreview(zip, 0, machine["prev_image_size"], machine["prev_back_color"], renderPreview, (168, 126));
            AddPreview(zip, 1, machine["prev2_image_size"], machine["prev2_back_color"], renderPreview, (168, 126));
            AddPreview(zip, 2, machine["cloudprev_imag_size"], machine["cloudprev_back_color"], renderPreview, (800, 600));

            AddText(zip, "layers_controller.conf", BuildLayersController(exposureTime, liftHeight, liftSpeed));
            AddText(zip, "lcd_function.json", BuildLcdFunction(modelName));

            AddBytes(zip, "layer_images/layer_0.pwszImg", layerData);
            AddBytes(zip, "calc_layer_volumes.data", BuildCalcLayerVolumes(volumeMl));
            AddBytes(zip, "scene.slice", BuildScene(stats));
        }

        return ms.ToArray();
    }

    private static JsonNode? FindActiveResin(JsonNode settings)
    {
        var machineExtern = settings["machine_extern"];
        var active = machineExtern?["active_resins"]?.AsArray().FirstOrDefault()?.GetValue<string>();
        if (active == null) return null;
        foreach (var listName in new[] { "user_resins", "factory_resins" })
        {
            if (machineExtern?[listName] is not JsonArray list) continue;
            var match = list.FirstOrDefault(r => r?["property"]?["name"]?.GetValue<string>() == active);
            if (match != null) return match;
        }

        return null;
    }

    private static string BuildPrintInfo(double cost, int printTime, double volumeMl, double weightG)
    {
        var info = new JsonObject
        {
            ["cost"] = cost,
            ["currency"] = "$",
            ["model_layers_count"] = 1,
            ["options_infos"] = null,
            ["print_time"] = printTime,
            ["sub_estimated_infos"] = new JsonArray
            {
                new JsonObject
                {
                    ["estimated_cost"] = cost,
                    ["estimated_cost_currency"] = "$",
                    ["estimated_time"] = printTime,
                    ["estimated_volume"] = volumeMl,
                    ["estimated_weight"] = weightG,
                    ["model_layers_count"] = 1,
                },
            },
            ["volume"] = volumeMl,
            ["weight"] = weightG,
        };
        return info.ToJsonString(JsonOptions);
    }

    private static string BuildLayersController(float exposureTime, double liftHeight, double liftSpeed)
    {
        var node = new JsonObject
        {
            ["count"] = 1,
            ["paras"] = new JsonArray
            {
                new JsonObject
                {
                    ["exposure_time"] = exposureTime,
                    ["layer_index"] = 0,
                    ["layer_minheight"] = 0.0,
                    ["layer_thickness"] = LayerHeight,
                    ["zup_height"] = liftHeight,
                    ["zup_speed"] = liftSpeed,
                },
            },
        };
        return node.ToJsonString(JsonOptions);
    }

    private static string BuildLcdFunction(string modelName)
    {
        var node = JsonNode.Parse(ReadResource("lcd_function.json"))!;
        if (node["models_processed_info"]?["models"] is JsonArray { Count: > 0 } models && models[0] is JsonObject model)
            model["name"] = modelName;
        return node.ToJsonString(JsonOptions);
    }

    private static byte[] BuildCalcLayerVolumes(double volumeMl)
    {
        const uint magic = 0x20251024;
        const float baseStep = 0.2f;
        const double rawVolumeToMilliliters = 0.0011;

        var data = new byte[16 + 48];
        var s = data.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(s, magic);
        BinaryPrimitives.WriteSingleLittleEndian(s[4..], baseStep);
        BinaryPrimitives.WriteUInt32LittleEndian(s[8..], 1);
        BinaryPrimitives.WriteUInt32LittleEndian(s[12..], 1); // entry count
        // entry: layer ref, z, thickness, 8 reserved, raw volume
        BinaryPrimitives.WriteUInt32LittleEndian(s[16..], 0);
        BinaryPrimitives.WriteSingleLittleEndian(s[20..], 0);
        BinaryPrimitives.WriteSingleLittleEndian(s[24..], baseStep);
        BinaryPrimitives.WriteSingleLittleEndian(s[60..], (float)Math.Round(volumeMl / rawVolumeToMilliliters, 4));
        return data;
    }

    private static byte[] BuildScene(LayerStats stats)
    {
        var data = new byte[464];
        var s = data.AsSpan();
        Encoding.ASCII.GetBytes("ANYCUBIC-PWSZ").CopyTo(s);
        Encoding.ASCII.GetBytes("AnycubicPhotonPCB").CopyTo(s[16..]);
        var p = 0x50;
        void U(uint v) { BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(p), v); p += 4; }
        void F(float v) { BinaryPrimitives.WriteSingleLittleEndian(data.AsSpan(p), v); p += 4; }

        U(3); // binary type: FPGA release
        U(3); // version (as written by Photon Workshop 4.1)
        U(0); // slice type
        U(0); // model unit: mm
        F(1); // point ratio
        U(1); // layer count
        F(stats.XMin);
        F(stats.YMin);
        F(0); // z min
        F(stats.XMax);
        F(stats.YMax);
        F(LayerHeight); // z max
        U(0); // model stats
        p += 64 * 4; // padding
        Encoding.ASCII.GetBytes("<---").CopyTo(s[p..]);
        p += 4;
        U(1); // layer definition count
        F(LayerHeight / 2); // Photon Workshop stores the middle of the layer
        F((float)Math.Round(stats.AreaMm2, 4));
        F(stats.XMin);
        F(stats.YMin);
        F(stats.XMax);
        F(stats.YMax);
        U(stats.ObjectCount);
        F(0); // max contour area (Photon Workshop writes 0)
        p += 8 * 4; // padding
        Encoding.ASCII.GetBytes("--->").CopyTo(s[p..]);
        return data;
    }

    private static void AddPreview(ZipArchive zip, int index, JsonNode? sizeNode, JsonNode? colorNode, PreviewRenderer render, (int W, int H) fallback)
    {
        var w = sizeNode?[0]?.GetValue<int>() ?? fallback.W;
        var h = sizeNode?[1]?.GetValue<int>() ?? fallback.H;
        var color = SKColors.Black;
        if (colorNode is JsonArray { Count: >= 3 } c)
        {
            color = new SKColor(
                (byte)Math.Round(c[0]!.GetValue<double>() * 255),
                (byte)Math.Round(c[1]!.GetValue<double>() * 255),
                (byte)Math.Round(c[2]!.GetValue<double>() * 255));
        }

        AddBytes(zip, $"preview_images/preview_{index}.png", render(w, h, color));
    }

    private static void AddText(ZipArchive zip, string name, string text) => AddBytes(zip, name, new UTF8Encoding(false).GetBytes(text));

    private static void AddBytes(ZipArchive zip, string name, byte[] data)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(data);
    }

    private static byte[] ReadResource(string name)
    {
        using var stream = typeof(Pm4uWriter).Assembly.GetManifestResourceStream(ResourcePrefix + name)
                           ?? throw new InvalidOperationException($"Missing embedded resource {name}.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
