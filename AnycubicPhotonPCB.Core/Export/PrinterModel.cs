namespace AnycubicPhotonPCB.Core.Export;

public enum PhotonEncoding
{
    // 7-bit run length (Photon, Photon S)
    Rle,

    // 4-bit colour + 12-bit run length, a.k.a. PW0
    Rle4,
}

public enum PrinterFileKind
{
    // Classic binary Photon Workshop file (ANYCUBIC header, versions 1 / 515 / 516 / 517)
    PhotonWorkshop,

    // ZIP based Photon Workshop 4.x file (.pm4u and friends)
    PhotonWorkshopZip,
}

public sealed record PrinterModel
{
    public required string Name { get; init; }

    public required string FileExtension { get; init; }

    // Machine name written to the MACHINE section of binary files (version 516 and later); Name when not set
    public string? MachineName { get; init; }

    public PrinterFileKind Kind { get; init; } = PrinterFileKind.PhotonWorkshop;

    // File version and "area number" written to the ANYCUBIC header
    public (int Version, int AreaNumber) FileVersion { get; init; }

    // Pixel size in millimetres
    public required double XyRes { get; init; }

    public required int ResolutionX { get; init; }

    public required int ResolutionY { get; init; }

    // Display width, height and machine Z in millimetres (required for file version 516)
    public (float Width, float Height, float Z)? PhysicalDimensions { get; init; }

    public (int Width, int Height) PreviewResolution { get; init; } = (224, 168);

    public bool Rotate180 { get; init; }

    public PhotonEncoding Encoding { get; init; } = PhotonEncoding.Rle4;

    public override string ToString() => Name;

    public static IReadOnlyList<PrinterModel> All { get; } =
    [
        new()
        {
            Name = "AnyCubic Photon Mono 4 Ultra (.pm4u)",
            FileExtension = "pm4u",
            Kind = PrinterFileKind.PhotonWorkshopZip,
            XyRes = 0.017,
            ResolutionX = 9024,
            ResolutionY = 5120,
            PhysicalDimensions = (153.408f, 87.04f, 165.0f),
            PreviewResolution = (168, 126),
            // Checked on the printer: the native frame is the screen as seen from above with the front at the
            // bottom, so the board lands where the export preview shows it. Photon Workshop itself places
            // centred models turned by 180° relative to this (same mirroring, only rotated)
            Rotate180 = false,
        },
        new()
        {
            // Binary Photon Workshop file version 517 (layout from a file exported by Photon Workshop 4.1). Same
            // screen as the Mono 4 Ultra but raster layers; like the Mono 4 Ultra the native frame is not rotated
            Name = "AnyCubic Photon Mono 4 (.pm4n)", FileExtension = "pm4n", MachineName = "Anycubic Photon Mono 4",
            FileVersion = (517, 9), XyRes = 0.017,
            ResolutionX = 9024, ResolutionY = 5120, PhysicalDimensions = (153.408f, 87.04f, 165.0f), Rotate180 = false,
        },
        new()
        {
            Name = "AnyCubic Photon Ultra (.dlp)", FileExtension = "dlp", FileVersion = (515, 5), XyRes = 0.080,
            ResolutionX = 1280, ResolutionY = 720, Rotate180 = true,
        },
        new()
        {
            Name = "AnyCubic Photon M3 (.pm3)", FileExtension = "pm3", FileVersion = (516, 8), XyRes = 0.040,
            ResolutionX = 4096, ResolutionY = 2560, PhysicalDimensions = (163.92f, 102.4f, 180.0f), Rotate180 = true,
        },
        new()
        {
            Name = "AnyCubic Photon M3 Max (.pm3m)", FileExtension = "pm3m", FileVersion = (516, 8), XyRes = 0.046,
            ResolutionX = 6480, ResolutionY = 3600, PhysicalDimensions = (298.08f, 165.6f, 300.0f), Rotate180 = true,
        },
        new()
        {
            Name = "AnyCubic Photon Mono SQ (.pmsq)", FileExtension = "pmsq", FileVersion = (515, 5), XyRes = 0.050,
            ResolutionX = 2400, ResolutionY = 2560, Rotate180 = false,
        },
        new()
        {
            Name = "AnyCubic Photon Zero (.pw0)", FileExtension = "pw0", FileVersion = (1, 4), XyRes = 0.1155,
            ResolutionX = 480, ResolutionY = 854, Rotate180 = false,
        },
        new()
        {
            Name = "AnyCubic Photon Mono 4K (.pwma)", FileExtension = "pwma", FileVersion = (516, 8), XyRes = 0.035,
            ResolutionX = 3840, ResolutionY = 2400, PhysicalDimensions = (134.4f, 84.0f, 165.0f), Rotate180 = true,
        },
        new()
        {
            Name = "AnyCubic Photon Mono X 6K & Photon M3 Plus (.pwmb)", FileExtension = "pwmb", FileVersion = (516, 8), XyRes = 0.0344,
            ResolutionX = 5760, ResolutionY = 3600, PhysicalDimensions = (197.0f, 122.8f, 245.0f), Rotate180 = true,
        },
        new()
        {
            Name = "AnyCubic Photon Mono (.pwmo)", FileExtension = "pwmo", FileVersion = (1, 4), XyRes = 0.051,
            ResolutionX = 1620, ResolutionY = 2560, Rotate180 = false,
        },
        new()
        {
            Name = "AnyCubic Photon Mono SE (.pwms)", FileExtension = "pwms", FileVersion = (1, 4), XyRes = 0.051,
            ResolutionX = 1620, ResolutionY = 2560, Rotate180 = false,
        },
        new()
        {
            Name = "AnyCubic Photon Mono X (.pwmx)", FileExtension = "pwmx", FileVersion = (1, 4), XyRes = 0.050,
            ResolutionX = 3840, ResolutionY = 2400, Rotate180 = false,
        },
        new()
        {
            Name = "AnyCubic Photon & Photon S (.pws)", FileExtension = "pws", FileVersion = (1, 4), XyRes = 0.047,
            ResolutionX = 1440, ResolutionY = 2560, Rotate180 = true, Encoding = PhotonEncoding.Rle,
        },
        new()
        {
            Name = "AnyCubic Photon X (.pwx)", FileExtension = "pwx", FileVersion = (1, 4), XyRes = 0.075,
            ResolutionX = 2560, ResolutionY = 1600, Rotate180 = true,
        },
    ];
}
