using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;

namespace AnycubicPhotonPCB.Core.Export;

public enum AnchorCorner
{
    TopLeft,
    TopRight,
    Center,
    BottomLeft,
    BottomRight,
}

// Mirroring of the exported layers of one board side
public readonly record struct SideFlips(bool Horizontal, bool Vertical);

// How the layers are placed on the printer screen
public sealed record ExportOptions
{
    public const int CopyGrid = 3;
    public required PrinterModel Printer { get; init; }
    public AnchorCorner Anchor { get; init; } = AnchorCorner.Center;
    public double OffsetXmm { get; init; }
    public double OffsetYmm { get; init; }
    public SideFlips TopFlips { get; init; }
    public SideFlips BottomFlips { get; init; }
    public BoardRotation Rotation { get; init; }
    public int CopyCells { get; init; } = 1;
    public double CopySpacingMm { get; init; } = 2;
    public SideFlips FlipsFor(PcbLayer layer) => layer.IsTopSide ? TopFlips : BottomFlips;
}
