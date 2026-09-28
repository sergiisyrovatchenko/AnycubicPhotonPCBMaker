using System.ComponentModel;
using AnycubicPhotonPCB.Core.Export;
using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;

namespace AnycubicPhotonPCB.Maker;

// Export tab of the main form: options on the left (layers with their exposure and flips, placement, copies), a live
// preview of the selected layer on the right. The printer, photoresist, drill holes and outline come from the
// Options menu of the main form; the preview messages and the save progress are reported to it by events.
// The full resolution printer files are only built when saving. Every change is written to the settings right away
internal sealed partial class ExportView : UserControl
{
    private const string Title = "Export";

    private readonly Dictionary<AnchorCorner, RadioButton> _anchors;
    private readonly Dictionary<BoardRotation, RadioButton> _rotations;

    // Exposure chosen per layer file in this session; new layers start from the setting of their type
    private readonly Dictionary<string, float> _exposures = new(StringComparer.OrdinalIgnoreCase);

    private AppSettings _settings = null!;
    private PcbProject? _project;
    private CancellationTokenSource? _cts;
    private bool _loading;
    private int _previewVersion;

    public ExportView()
    {
        InitializeComponent();
        _anchors = new Dictionary<AnchorCorner, RadioButton>
        {
            [AnchorCorner.TopLeft] = topLeftRadio,
            [AnchorCorner.TopRight] = topRightRadio,
            [AnchorCorner.Center] = centerRadio,
            [AnchorCorner.BottomLeft] = bottomLeftRadio,
            [AnchorCorner.BottomRight] = bottomRightRadio,
        };
        _rotations = new Dictionary<BoardRotation, RadioButton>
        {
            [BoardRotation.None] = rotateNoneRadio,
            [BoardRotation.Left] = rotateLeftRadio,
            [BoardRotation.Right] = rotateRightRadio,
        };
    }

    // Message for the status bar: what the preview shows, or a warning (the board does not fit, the preview failed)
    public sealed record Status(string Text, bool IsWarning);

    // Files written so far while saving
    public sealed record SaveProgress(int Done, int Total);

    public event EventHandler<Status>? StatusChanged;

    // Raised while saving; null once saving has finished
    public event EventHandler<SaveProgress?>? SaveProgressChanged;

    // Called once by the main form; the settings are not available to the designer
    public void Bind(AppSettings settings)
    {
        _settings = settings;
        _loading = true;
        ShowOptions(settings.Export);
        _loading = false;
        ShowLayer();
    }

    // Copper layers of the board; the selection and unchecked layers are kept by file name
    public void SetLayers(PcbProject? project, IReadOnlyList<PcbLayer> layers)
    {
        _project = project;
        layersList.SetLayers(layers);
        ShowLayer();
    }

    // Photoresist, drill holes or outline changed in the Options menu
    public void RefreshPreview() => UpdatePreview();

    // Printer chosen in Options > Printer of the main form. Runtime only: hidden from the designer, which would
    // otherwise write it into MainForm.Designer.cs and set it before Bind
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PrinterModel Printer
    {
        get => _settings?.Export.Printer ?? PrinterModel.All[0];
        set
        {
            if (_settings is null || _settings.Export.Printer == value) return;
            _settings.Export = _settings.Export with { Printer = value };
            UpdatePreview();
        }
    }

    // The preview is only rendered while the Export tab is shown
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        UpdatePreview();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _cts?.Cancel();
        base.OnHandleDestroyed(e);
    }

    #region Options

    private void LayersListSelectedLayerChanged(object? sender, EventArgs e) => ShowLayer();

    private void LayersListCheckedLayersChanged(object? sender, EventArgs e) => UpdateSaveButton();

    // Exposure of the selected layer, then the preview
    private void ShowLayer()
    {
        var layer = layersList.SelectedLayer;
        exposureNumeric.Enabled = layer != null;
        UpdateSaveButton();
        if (layer == null)
        {
            SetPreview(null);
            StatusChanged?.Invoke(this, new Status("The board has no copper layers to export.", false));
            return;
        }

        _loading = true;
        exposureNumeric.Value = Math.Clamp((decimal)Exposure(layer), exposureNumeric.Minimum, exposureNumeric.Maximum);
        _loading = false;
        UpdatePreview();
    }

    private float Exposure(PcbLayer layer) =>
        _exposures.TryGetValue(layer.FileName, out var seconds) ? seconds : _settings.GetExposure(layer.Type);

    private void ExposureNumericValueChanged(object? sender, EventArgs e)
    {
        if (_loading || layersList.SelectedLayer is not { } layer) return;
        var seconds = (float)exposureNumeric.Value;
        _exposures[layer.FileName] = seconds;
        _settings.SetExposure(layer.Type, seconds);
    }

    // Placement, flips or copies changed: remember them and refresh the preview
    private void OptionsChanged(object? sender, EventArgs e)
    {
        // A radio button being unchecked fires as well; only react to the new selection
        if (_loading || sender is RadioButton { Checked: false }) return;
        _settings.Export = ReadOptions();
        UpdatePreview();
    }

    private void UpdateSaveButton()
    {
        var count = layersList.CheckedItems.Count;
        saveButton.Enabled = count > 0 && _cts == null;
        saveButton.Text = count > 1 ? "Save all…" : "Save…";
    }

    // Controls <-> export options; the printer is not shown here and is kept as it is
    private void ShowOptions(ExportOptions options)
    {
        _anchors[options.Anchor].Checked = true;
        offsetXNumeric.Value = Math.Clamp((decimal)options.OffsetXmm, offsetXNumeric.Minimum, offsetXNumeric.Maximum);
        offsetYNumeric.Value = Math.Clamp((decimal)options.OffsetYmm, offsetYNumeric.Minimum, offsetYNumeric.Maximum);
        layersList.TopFlips = options.TopFlips;
        layersList.BottomFlips = options.BottomFlips;
        _rotations[options.Rotation].Checked = true;
        copyMatrix.Cells = options.CopyCells;
        copySpacingNumeric.Value = Math.Clamp((decimal)options.CopySpacingMm, copySpacingNumeric.Minimum, copySpacingNumeric.Maximum);
    }

    private ExportOptions ReadOptions() => _settings.Export with
    {
        Anchor = _anchors.First(a => a.Value.Checked).Key,
        OffsetXmm = (double)offsetXNumeric.Value,
        OffsetYmm = (double)offsetYNumeric.Value,
        TopFlips = layersList.TopFlips,
        BottomFlips = layersList.BottomFlips,
        Rotation = _rotations.First(r => r.Value.Checked).Key,
        CopyCells = copyMatrix.Cells,
        CopySpacingMm = (double)copySpacingNumeric.Value,
    };

    private LayerExportRequest Request(PcbLayer layer) => new()
    {
        Layer = layer,
        Invert = _settings.ExposesFeatures(layer.Type),
        Extras = _settings.ExtrasFor(layer.Type),
        ExposureSeconds = Exposure(layer),
    };

    #endregion

    #region Preview

    private void PreviewPictureSizeChanged(object? sender, EventArgs e) => UpdatePreview();

    // Renders the sketch off the UI thread; results of outdated requests are dropped
    private async void UpdatePreview()
    {
        var project = _project;
        var layer = layersList.SelectedLayer;
        var area = previewPicture.ClientSize;
        if (!Visible || _settings is null || project == null || layer == null || area.Width < 16 || area.Height < 16) return;

        var version = ++_previewVersion;
        var request = Request(layer);
        var options = _settings.Export;

        try
        {
            var result = await Task.Run(() => ExportPreview.Render(project, request, options, area));
            if (version != _previewVersion || IsDisposed)
            {
                result.Image.Dispose();
                return;
            }

            SetPreview(result.Image);
            var boards = result.Copies > 1 ? $"The {result.Copies} copies do" : "The board does";
            var text = result.Fits
                ? "Whole printer screen as seen from above; the bottom edge is the printer front. White = exposed." +
                  (result.Copies > 1 ? $" {result.Copies} copies." : "")
                : result.FitsTurned
                    ? $"⚠ {boards} not fit on the printer screen at this position and will be clipped. They fit when rotated by 90°."
                    : $"⚠ {boards} not fit on the printer screen at this position and will be clipped.";
            StatusChanged?.Invoke(this, new Status(text, !result.Fits));
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, new Status("Preview failed: " + ex.Message, true));
        }
    }

    private void SetPreview(Image? image)
    {
        ++_previewVersion;
        previewPicture.ReplaceImage(image);
    }

    #endregion

    #region Saving

    // Asks where to save first, then builds the full resolution files and writes them
    private async void SaveClicked(object? sender, EventArgs e)
    {
        var project = _project;
        var layers = layersList.CheckedLayers;
        if (project == null || layers.Count == 0) return;

        var options = _settings.Export;
        var requests = layers.Select(Request).ToList();
        var names = PhotonExporter.BuildFileNames(requests.Select(r => r.Layer.FileName).ToList(), options.Printer.FileExtension);

        var targets = ChooseTargets(names, options.Printer);
        if (targets == null) return;

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        mainLayout.Enabled = false;
        UpdateSaveButton();
        UseWaitCursor = true;
        SaveProgressChanged?.Invoke(this, new SaveProgress(0, requests.Count));
        try
        {
            // Layers finish in parallel, so progress reports can arrive out of order
            var done = 0;
            var progress = new Progress<int>(v =>
            {
                done = Math.Max(done, v);
                SaveProgressChanged?.Invoke(this, new SaveProgress(done, requests.Count));
            });
            var exported = await Task.Run(() => PhotonExporter.ExportAll(project, requests, options, progress, token), token);
            ReportSaved(exported, targets);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cts = null;
            UseWaitCursor = false;
            SaveProgressChanged?.Invoke(this, null);
            mainLayout.Enabled = true;
            UpdateSaveButton();
        }
    }

    // Full paths for the files: a save dialog for one layer, an output folder for several.
    // Null when the user cancels
    private List<string>? ChooseTargets(List<string> names, PrinterModel printer)
    {
        if (names.Count == 1)
        {
            using var dialog = new SaveFileDialog
            {
                FileName = names[0],
                Filter = $"{printer.Name}|*.{printer.FileExtension}|All files|*.*",
                InitialDirectory = _settings.LastOutputFolder ?? "",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return null;
            _settings.LastOutputFolder = Path.GetDirectoryName(dialog.FileName);
            return [dialog.FileName];
        }

        using var folder = new FolderBrowserDialog
        {
            Description = "Select the output folder",
            InitialDirectory = _settings.LastOutputFolder ?? "",
        };
        if (folder.ShowDialog(this) != DialogResult.OK) return null;

        var targets = names.Select(n => Path.Combine(folder.SelectedPath, n)).ToList();
        var existing = targets.Where(File.Exists).Select(Path.GetFileName).ToList();
        if (existing.Count > 0 &&
            MessageBox.Show(this, "Overwrite existing files?\n\n" + string.Join("\n", existing), Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return null;

        _settings.LastOutputFolder = folder.SelectedPath;
        return targets;
    }

    private void ReportSaved(List<ExportedFile> exported, List<string> targets)
    {
        var failed = new List<string>();
        for (var i = 0; i < exported.Count; i++)
        {
            if (!TryWrite(targets[i], exported[i].Data, out var error))
                failed.Add($"{Path.GetFileName(targets[i])}: {error}");
        }

        var warnings = exported.SelectMany(f => f.Warnings.Select(w => $"{f.FileName}: {w}")).ToList();
        var written = exported.Count - failed.Count;
        var message = exported.Count == 1 && failed.Count == 0
            ? $"Saved {targets[0]}"
            : $"Wrote {written} of {exported.Count} files to {Path.GetDirectoryName(targets[0])}";
        if (failed.Count > 0) message += "\n\nFailed:\n" + string.Join("\n", failed);
        if (warnings.Count > 0) message += "\n\nWarnings:\n" + string.Join("\n", warnings);

        MessageBox.Show(this, message, Title, MessageBoxButtons.OK,
            failed.Count > 0 ? MessageBoxIcon.Error : warnings.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    // The target may be read-only, locked by another program or on a removed drive (e.g. the printer's USB stick)
    private static bool TryWrite(string path, byte[] data, out string error)
    {
        try
        {
            File.WriteAllBytes(path, data);
            error = "";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            error = ex.Message;
            return false;
        }
    }

    #endregion
}
