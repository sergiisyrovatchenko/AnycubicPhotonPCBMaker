using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AnycubicPhotonPCB.Core.Export;
using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;

namespace AnycubicPhotonPCB.Maker;

// User preferences persisted between runs in <application name>.json next to the executable (AnycubicPhotonPCB.Maker.json)
public sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        AppContext.BaseDirectory, typeof(AppSettings).Assembly.GetName().Name + ".json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new PrinterConverter() },
    };

    // Default exposure for layer types that were never exported
    private const float DefaultExposureSeconds = 10;

    // The initial values below are the application defaults: used when there is no settings file and for any value
    // missing from it

    // Printer (Options > Printer) and placement on the screen (Export tab), stored as a whole.
    // Default: Photon Mono 4 Ultra, board in the centre of the screen, no flips
    public ExportOptions Export { get; set; } = new()
    {
        Printer = PrinterModel.All[0],
        Anchor = AnchorCorner.Center,
        TopFlips = default,
        BottomFlips = default,
    };

    // Last exposure time per layer type name (e.g. "Copper"); types not listed use DefaultExposureSeconds
    public Dictionary<string, float> ExposureByLayerType { get; set; } = new();

    // Options > Photoresist on the main form: true = negative (exposed areas stay, dry film),
    // false = positive (exposed areas dissolve)
    public bool NegativeResist { get; set; } = true;

    // Options > Subtract holes on the main form: all drill holes are cut out of the copper and soldermask layers
    public bool SubtractHoles { get; set; } = true;

    // Options > Drill holes (drill marks) on the main form: holes in bare areas get a ring in the copper layers
    public bool DrillMarks { get; set; } = true;

    // Options > Board outline on the main form: the outline layer is drawn into the copper layers
    public bool DrawOutline { get; set; } = true;

    public string? LastInputFolder { get; set; }

    public string? LastOutputFolder { get; set; }

    // True when the features of a layer of this type are exposed (white in the previews), false when the
    // background is. Negative photoresist keeps the exposed areas, so the features themselves get exposed.
    // The soldermask Gerber draws the openings rather than the mask, so there it is the other way round
    public bool ExposesFeatures(LayerType type) => type == LayerType.Soldermask ? !NegativeResist : NegativeResist;

    // Drill holes / marks and outline for a layer of this type, in the previews and the export
    public LayerExtras ExtrasFor(LayerType type) => LayerExtras.For(type, SubtractHoles, DrillMarks, DrawOutline);

    public float GetExposure(LayerType type) =>
        ExposureByLayerType.TryGetValue(type.ToString(), out var seconds) ? seconds : DefaultExposureSeconds;

    public void SetExposure(LayerType type, float seconds) => ExposureByLayerType[type.ToString()] = seconds;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath) && JsonNode.Parse(File.ReadAllText(FilePath)) is JsonObject json)
            {
                var settings = json.Deserialize<AppSettings>(JsonOptions) ?? new AppSettings();
                if (json["Export"] == null && json["PrinterExtension"] != null) settings.Export = LegacyExport(json);
                return settings;
            }
        }
        catch (Exception)
        {
            // Corrupt or incompatible settings: start with defaults.
        }

        return new AppSettings();
    }

    // Settings files written before the export options were stored as a whole kept them as separate fields
    private static ExportOptions LegacyExport(JsonObject json)
    {
        T Get<T>(string name, T fallback) => json[name] is { } node ? node.GetValue<T>() : fallback;

        var defaults = new ExportOptions { Printer = PrinterModel.All[0] };
        var extension = Get<string?>("PrinterExtension", null);
        return defaults with
        {
            Printer = PrinterModel.All.FirstOrDefault(p => p.FileExtension == extension) ?? defaults.Printer,
            Anchor = (AnchorCorner)Get("Anchor", (int)defaults.Anchor),
            OffsetXmm = Get("OffsetXmm", 0.0),
            OffsetYmm = Get("OffsetYmm", 0.0),
            TopFlips = new SideFlips(Get("FlipTopHorizontal", false), Get("FlipTopVertical", true)),
            BottomFlips = new SideFlips(Get("FlipBottomHorizontal", false), Get("FlipBottomVertical", false)),
            Rotation = (BoardRotation)Get("Rotation", 0),
            CopyCells = Get("CopyCells", defaults.CopyCells),
            CopySpacingMm = Get("CopySpacingMm", defaults.CopySpacingMm),
        };
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception)
        {
            // Settings are a convenience; never fail because of them.
        }
    }

    // A printer is stored by its file extension; an unknown one falls back to the first printer
    private sealed class PrinterConverter : JsonConverter<PrinterModel>
    {
        public override PrinterModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var extension = reader.GetString();
            return PrinterModel.All.FirstOrDefault(p => p.FileExtension == extension) ?? PrinterModel.All[0];
        }

        public override void Write(Utf8JsonWriter writer, PrinterModel value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.FileExtension);
    }
}
