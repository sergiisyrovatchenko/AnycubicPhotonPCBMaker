using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;

namespace AnycubicPhotonPCB.Core.Export;

// One layer to export and how its mask is built
public sealed class LayerExportRequest
{
    public required PcbLayer Layer { get; init; }

    // True: the features are exposed; false: the background is exposed and the features stay dark
    public bool Invert { get; init; }

    // Drill holes / marks and board outline drawn into the layer (see LayerExtras.For)
    public LayerExtras Extras { get; init; } = LayerExtras.None;

    public float ExposureSeconds { get; init; } = 10;
}

// A printer file built for one layer
public sealed class ExportedFile
{
    public required PcbLayer Layer { get; init; }
    public required string FileName { get; init; }
    public required byte[] Data { get; init; }
    public List<string> Warnings { get; } = [];
}
