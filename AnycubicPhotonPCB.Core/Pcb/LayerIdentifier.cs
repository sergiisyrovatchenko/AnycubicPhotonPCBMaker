using System.Text.RegularExpressions;

namespace AnycubicPhotonPCB.Core.Pcb;

public enum LayerType
{
    Unknown,
    Copper,
    Soldermask,
    Silkscreen,
    Solderpaste,
    Outline,
    Drill,
    Drawing,
}

public enum LayerSide
{
    None,
    Top,
    Bottom,
    Inner,
    All,
}

// Guesses layer type/side from file names (port of tracespace's whats-that-gerber rules)
// and from the Gerber X2 .FileFunction attribute
public static class LayerIdentifier
{
    private enum Cad { Any, KiCad, Altium, EagleLegacy, Eagle, EagleOshPark, EaglePcbNg, GedaPcb, DipTrace, OrCad, Allegro }

    private sealed record Matcher(Regex Regex, Cad[] Cads);

    private sealed record Rule(LayerType Type, LayerSide Side, Matcher[] Matchers);

    private static Matcher Ext(string ext, params Cad[] cads) =>
        new(new Regex(@"\.(" + ext + ")$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), cads);

    private static Matcher Match(string pattern, params Cad[] cads) =>
        new(new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), cads);

    private static readonly Cad[] EagleAll = [Cad.Eagle, Cad.EagleLegacy, Cad.EagleOshPark, Cad.EaglePcbNg];

    private static readonly Rule[] Rules =
    [
        new(LayerType.Unknown, LayerSide.None,
        [
            Ext("gpi", EagleAll), Ext("dri", EagleAll), Ext("csv"), Match("pnp_bom", Cad.EaglePcbNg),
            Ext("gbrjob"), Ext("pdf"), Ext("zip"), Ext("png"), Ext("jpg"), Ext("svg"), Ext("md"), Ext("rpt"),
        ]),
        new(LayerType.Copper, LayerSide.Top,
        [
            Ext("cmp", Cad.EagleLegacy), Ext("top", Cad.EagleLegacy, Cad.OrCad), Ext("gtl", Cad.KiCad, Cad.Altium),
            Ext(@"toplayer\.ger", Cad.EagleOshPark), Match(@"top\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"f[._]cu", Cad.KiCad), Match("copper_top", Cad.Eagle), Match("top_copper", Cad.EaglePcbNg),
            Match("top copper"), Match("toplayer"),
        ]),
        new(LayerType.Soldermask, LayerSide.Top,
        [
            Ext("stc", Cad.EagleLegacy), Ext("tsm", Cad.EagleLegacy), Ext("gts", Cad.KiCad, Cad.Altium), Ext("smt", Cad.OrCad),
            Ext(@"topsoldermask\.ger", Cad.EagleOshPark), Match(@"topmask\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"f[._]mask", Cad.KiCad), Match("soldermask_top", Cad.Eagle), Match("top_mask", Cad.EaglePcbNg),
            Match("top solder resist"), Match("topsoldermasklayer"),
        ]),
        new(LayerType.Silkscreen, LayerSide.Top,
        [
            Ext("plc", Cad.EagleLegacy), Ext("tsk", Cad.EagleLegacy), Ext("gto", Cad.KiCad, Cad.Altium), Ext("sst", Cad.OrCad),
            Ext(@"topsilkscreen\.ger", Cad.EagleOshPark), Match(@"topsilk\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"f[._]silks", Cad.KiCad), Match("silkscreen_top", Cad.Eagle), Match("top_silk", Cad.EaglePcbNg),
            Match("top silk screen"), Match("topsilklayer"),
        ]),
        new(LayerType.Solderpaste, LayerSide.Top,
        [
            Ext("crc", Cad.EagleLegacy), Ext("tsp", Cad.EagleLegacy), Ext("gtp", Cad.KiCad, Cad.Altium), Ext("spt", Cad.OrCad),
            Ext(@"tcream\.ger", Cad.EagleOshPark), Match(@"toppaste\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"f[._]paste", Cad.KiCad), Match("solderpaste_top", Cad.Eagle), Match("top_paste", Cad.EaglePcbNg),
            Match("toppastemasklayer"),
        ]),
        new(LayerType.Copper, LayerSide.Bottom,
        [
            Ext("sol", Cad.EagleLegacy), Ext("bot", Cad.EagleLegacy, Cad.OrCad), Ext("gbl", Cad.KiCad, Cad.Altium),
            Ext(@"bottomlayer\.ger", Cad.EagleOshPark), Match(@"bottom\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"b[._]cu", Cad.KiCad), Match("copper_bottom", Cad.Eagle), Match("bottom_copper", Cad.EaglePcbNg),
            Match("bottom copper"), Match("bottomlayer"),
        ]),
        new(LayerType.Soldermask, LayerSide.Bottom,
        [
            Ext("sts", Cad.EagleLegacy), Ext("bsm", Cad.EagleLegacy), Ext("gbs", Cad.KiCad, Cad.Altium), Ext("smb", Cad.OrCad),
            Ext(@"bottomsoldermask\.ger", Cad.EagleOshPark), Match(@"bottommask\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"b[._]mask", Cad.KiCad), Match("soldermask_bottom", Cad.Eagle), Match("bottom_mask", Cad.EaglePcbNg),
            Match("bottom solder resist"), Match("bottomsoldermasklayer"),
        ]),
        new(LayerType.Silkscreen, LayerSide.Bottom,
        [
            Ext("pls", Cad.EagleLegacy), Ext("bsk", Cad.EagleLegacy), Ext("gbo", Cad.KiCad, Cad.Altium), Ext("ssb", Cad.OrCad),
            Ext(@"bottomsilkscreen\.ger", Cad.EagleOshPark), Match(@"bottomsilk\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"b[._]silks", Cad.KiCad), Match("silkscreen_bottom", Cad.Eagle), Match("bottom_silk", Cad.EaglePcbNg),
            Match("bottom silk screen"), Match("bottomsilklayer"),
        ]),
        new(LayerType.Solderpaste, LayerSide.Bottom,
        [
            Ext("crs", Cad.EagleLegacy), Ext("bsp", Cad.EagleLegacy), Ext("gbp", Cad.KiCad, Cad.Altium), Ext("spb", Cad.OrCad),
            Ext(@"bcream\.ger", Cad.EagleOshPark), Match(@"bottompaste\.\w+$", Cad.GedaPcb, Cad.DipTrace),
            Match(@"b[._]paste", Cad.KiCad), Match("solderpaste_bottom", Cad.Eagle), Match("bottom_paste", Cad.EaglePcbNg),
            Match("bottompastemasklayer"),
        ]),
        new(LayerType.Copper, LayerSide.Inner,
        [
            Ext(@"ly\d+", Cad.EagleLegacy), Ext(@"gp?\d+", Cad.KiCad, Cad.Altium), Ext(@"in\d+", Cad.OrCad),
            Ext(@"internalplane\d+\.ger", Cad.EagleOshPark), Match(@"in(?:ner)?\d+[._]cu", Cad.KiCad), Match("inner", Cad.DipTrace),
        ]),
        new(LayerType.Outline, LayerSide.All,
        [
            Ext("dim", Cad.EagleLegacy), Ext("mil", Cad.EagleLegacy), Ext("gml", Cad.EagleLegacy), Ext(@"gm\d+", Cad.KiCad, Cad.Altium),
            Ext("gko", Cad.Altium), Ext("fab", Cad.OrCad), Ext("drd", Cad.OrCad),
            Match("outline", Cad.GedaPcb, Cad.EaglePcbNg), Match("boardoutline", Cad.EagleOshPark, Cad.DipTrace),
            Match(@"edge[._]cuts", Cad.KiCad), Match("profile", Cad.Eagle), Match(@"mechanical \d+"),
        ]),
        new(LayerType.Drill, LayerSide.All,
        [
            Ext("txt", Cad.EagleLegacy, Cad.Altium), Ext("xln", Cad.Eagle, Cad.EagleLegacy, Cad.EagleOshPark), Ext("exc", Cad.EagleLegacy),
            Ext("drd", Cad.EagleLegacy), Ext("drl", Cad.KiCad, Cad.DipTrace), Ext("tap", Cad.OrCad), Ext("npt", Cad.OrCad),
            Ext(@"plated-drill\.cnc", Cad.GedaPcb), Match("fab", Cad.GedaPcb), Match("npth", Cad.KiCad), Match("/drill/", Cad.EaglePcbNg),
        ]),
        new(LayerType.Drawing, LayerSide.None,
        [
            Ext("pos", Cad.KiCad), Ext("art", Cad.Allegro), Ext("gbr"), Ext("gbx"), Ext("ger"), Ext("pho"),
        ]),
    ];

    private sealed record Candidate(string FileName, LayerType Type, LayerSide Side, Cad[] Cads);

    // Identifies each file name, using the CAD package common to the whole set to break ties
    public static IReadOnlyDictionary<string, (LayerType Type, LayerSide Side)> Identify(IEnumerable<string> fileNames)
    {
        var names = fileNames.ToList();
        var candidates = names.SelectMany(GetCandidates).ToList();

        var cadVotes = candidates
            .SelectMany(c => c.Cads.Where(cad => cad != Cad.Any).Select(cad => (c.FileName, cad)))
            .Distinct()
            .GroupBy(v => v.cad)
            .Select(g => (Cad: g.Key, Count: g.Count()))
            .OrderByDescending(g => g.Count)
            .ToList();
        var commonCad = cadVotes.Count > 0 ? cadVotes[0].Cad : Cad.Any;

        var result = new Dictionary<string, (LayerType, LayerSide)>();
        foreach (var name in names)
        {
            var own = candidates.Where(c => c.FileName == name).ToList();
            var best = own.FirstOrDefault(c => c.Cads.Contains(commonCad)) ?? own.FirstOrDefault();
            result[name] = best == null ? (LayerType.Unknown, LayerSide.None) : (best.Type, best.Side);
        }

        return result;
    }

    private static IEnumerable<Candidate> GetCandidates(string fileName)
    {
        var name = Path.GetFileName(fileName).ToLowerInvariant();
        foreach (var rule in Rules)
        foreach (var matcher in rule.Matchers)
        {
            if (matcher.Regex.IsMatch(name))
                yield return new Candidate(fileName, rule.Type, rule.Side, matcher.Cads.Length == 0 ? [Cad.Any] : matcher.Cads);
        }
    }

    // Maps the Gerber X2 .FileFunction attribute (e.g. "Copper,L1,Top") to a layer type
    public static (LayerType Type, LayerSide Side)? FromFileFunction(string? fileFunction)
    {
        if (string.IsNullOrWhiteSpace(fileFunction)) return null;
        var parts = fileFunction.Split(',', StringSplitOptions.TrimEntries);
        var kind = parts[0].ToLowerInvariant();

        LayerSide Side(int index) => parts.Length > index
            ? parts[index].ToLowerInvariant() switch
            {
                "top" => LayerSide.Top,
                "bot" => LayerSide.Bottom,
                "inr" => LayerSide.Inner,
                _ => LayerSide.None,
            }
            : LayerSide.None;

        return kind switch
        {
            "copper" => (LayerType.Copper, Side(2)),
            "soldermask" => (LayerType.Soldermask, Side(1)),
            "legend" => (LayerType.Silkscreen, Side(1)),
            "paste" => (LayerType.Solderpaste, Side(1)),
            "profile" => (LayerType.Outline, LayerSide.All),
            "plated" or "nonplated" => (LayerType.Drill, LayerSide.All),
            _ => (LayerType.Drawing, LayerSide.None),
        };
    }
}
