using AnycubicPhotonPCB.Core.Export;
using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;

namespace AnycubicPhotonPCB.Maker;

internal sealed partial class MainForm : Form
{
    private static readonly LayerType[] TypeOrder =
    [
        LayerType.Copper, LayerType.Soldermask, LayerType.Silkscreen, LayerType.Solderpaste,
        LayerType.Outline, LayerType.Drill, LayerType.Drawing,
    ];

    private readonly AppSettings _settings = AppSettings.Load();
    private readonly string[] _startupPaths;
    private readonly string _appTitle;
    private PcbProject? _project;
    private int _loadVersion;
    private bool _suppressCheckEvents;
    private readonly HashSet<string> _reportedWarnings = [];

    // The Visual Studio designer instantiates the base Form, so no parameterless constructor is needed.
    // startupPaths: Files, a folder or a ZIP passed on the command line
    public MainForm(string[] startupPaths)
    {
        _startupPaths = startupPaths;
        InitializeComponent();
        _appTitle = Text;

        // Nothing to show until files are opened; the tabs stay visible in the designer
        tabs.Visible = false;
        boardLayout.SizeChanged += (_, _) => ArrangeBoardViews();
        exportView.StatusChanged += ExportViewStatusChanged;
        exportView.SaveProgressChanged += ExportViewSaveProgressChanged;
        exportView.Bind(_settings);
        // Read both drill options first: checking one menu item runs DrillsMenuItemCheckedChanged, which stores the
        // state of both items and would overwrite the other option before its item is set
        var (subtractHoles, drillMarks) = (_settings.SubtractHoles, _settings.DrillMarks);
        outlineMenuItem.Checked = _settings.DrawOutline;
        drillsSubtractMenuItem.Checked = subtractHoles;
        drillsMarksMenuItem.Checked = drillMarks;
        ShowPhotoresist();
        BuildPrinterMenu();
        ShowPrinterStatus();
    }

    // Options > Printer: one item per supported printer, the current one checked
    private void BuildPrinterMenu()
    {
        foreach (var printer in PrinterModel.All)
        {
            // The printer names already end with their file extension, e.g. "AnyCubic Photon Mono 4 Ultra (.pm4u)"
            var item = new ToolStripMenuItem(printer.Name)
            {
                Tag = printer,
                Checked = printer == exportView.Printer,
            };
            item.Click += PrinterMenuItemClicked;
            printerMenuItem.DropDownItems.Add(item);
        }
    }

    private void PrinterMenuItemClicked(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: PrinterModel printer }) return;
        foreach (ToolStripMenuItem item in printerMenuItem.DropDownItems) item.Checked = item == sender;
        exportView.Printer = printer;
        ShowPrinterStatus();
    }

    // Status bar: the export preview message, grey or red for a warning
    private void ExportViewStatusChanged(object? sender, ExportView.Status status)
    {
        previewStatusLabel.Text = status.Text;
        previewStatusLabel.ForeColor = status.IsWarning ? Color.Firebrick : SystemColors.GrayText;
    }

    // Status bar: progress bar while the Export tab saves files
    private void ExportViewSaveProgressChanged(object? sender, ExportView.SaveProgress? progress)
    {
        progressStatusBar.Visible = progress != null;
        if (progress == null) return;
        progressStatusBar.Maximum = progress.Total;
        progressStatusBar.Value = Math.Min(progress.Done, progress.Total);
    }

    // Status bar: the selected printer and its screen
    private void ShowPrinterStatus()
    {
        var printer = exportView.Printer;
        var (w, h) = ScreenLayout.ScreenSize(printer);
        printerStatusLabel.Text = printer.Name;
        screenStatusLabel.Text = $"Screen {w} × {h} px, {w * printer.XyRes:0.#} × {h * printer.XyRes:0.#} mm, pixel {printer.XyRes * 1000:0.#} µm";
    }

    // Options > Photoresist: positive (exposed areas dissolve) or negative (exposed areas stay, dry film).
    // Decides whether the background or the features are exposed, in the layer previews and the export
    private void PhotoresistMenuItemClicked(object? sender, EventArgs e)
    {
        _settings.NegativeResist = sender == negativeResistMenuItem;
        ShowPhotoresist();
        foreach (var card in LayerCards) card.Inverted = _settings.ExposesFeatures(card.Layer.Type);
        exportView.RefreshPreview();
    }

    private void ShowPhotoresist()
    {
        positiveResistMenuItem.Checked = !_settings.NegativeResist;
        negativeResistMenuItem.Checked = _settings.NegativeResist;
    }

    private IEnumerable<LayerCard> LayerCards =>
        tabs.TabPages.Cast<TabPage>().SelectMany(p => p.Controls.OfType<CardGrid>()).SelectMany(g => g.Controls.OfType<LayerCard>());

    // Options > Subtract holes and Options > Drill holes, two independent options: Subtract holes cuts the holes out
    // as they will be on the board; Drill holes adds rings inside the holes in bare areas, which Subtract cannot show. Applies to the
    // copper layers (Subtract to the soldermask as well), in the previews and the export
    private void DrillsMenuItemCheckedChanged(object? sender, EventArgs e)
    {
        _settings.SubtractHoles = drillsSubtractMenuItem.Checked;
        _settings.DrillMarks = drillsMarksMenuItem.Checked;
        ApplyExtras();
    }

    // Options > Board outline: the outline is drawn into the copper layers, in the previews and the export
    private void OutlineMenuItemCheckedChanged(object? sender, EventArgs e)
    {
        _settings.DrawOutline = outlineMenuItem.Checked;
        ApplyExtras();
    }

    private void ApplyExtras()
    {
        foreach (var card in LayerCards) card.Extras = _settings.ExtrasFor(card.Layer.Type);
        exportView.RefreshPreview();
    }

    // Height of the "Top" / "Bottom" captions on the PCB tab, in logical pixels
    private const int BoardCaptionHeight = 24;

    // Last applied arrangement of the PCB tab, to avoid rebuilding the table when nothing changed
    private (bool SideBySide, int A, int B, int Odd) _boardArrangement;

    // The top and bottom views are stacked or placed side by side, whichever shows the board larger for the
    // current window and board shape. Both views always get exactly the same size: absolute rows / columns,
    // with an odd leftover pixel moved into the table padding (the last row / column would take it otherwise)
    private void ArrangeBoardViews()
    {
        var area = boardLayout.ClientSize + boardLayout.Padding.Size;
        var caption = LogicalToDeviceUnits(BoardCaptionHeight);
        var aspect = _project is { } p && p.BoardBounds.Height > 0 ? p.BoardBounds.Width / (double)p.BoardBounds.Height : 2;

        // Board height in pixels each arrangement allows
        var stackedHeight = Math.Max(0, (area.Height - 2 * caption) / 2);
        var stacked = Math.Min(area.Width / aspect, stackedHeight);
        var sideWidth = Math.Max(0, area.Width / 2);
        var side = Math.Min(sideWidth / aspect, area.Height - caption);
        var sideBySide = side > stacked;

        var arrangement = sideBySide
            ? (true, sideWidth, area.Height - caption, area.Width - 2 * sideWidth)
            : (false, area.Width, stackedHeight, Math.Max(0, area.Height - 2 * caption - 2 * stackedHeight));
        if (arrangement == _boardArrangement) return;
        _boardArrangement = arrangement;

        boardLayout.SuspendLayout();
        boardLayout.ColumnStyles.Clear();
        boardLayout.RowStyles.Clear();
        if (sideBySide)
        {
            boardLayout.ColumnCount = 2;
            boardLayout.RowCount = 2;
            boardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, sideWidth));
            boardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, sideWidth));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, caption));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            boardLayout.SetCellPosition(topCaption, new TableLayoutPanelCellPosition(0, 0));
            boardLayout.SetCellPosition(bottomCaption, new TableLayoutPanelCellPosition(1, 0));
            boardLayout.SetCellPosition(topPicture, new TableLayoutPanelCellPosition(0, 1));
            boardLayout.SetCellPosition(bottomPicture, new TableLayoutPanelCellPosition(1, 1));
            boardLayout.Padding = new Padding(0, 0, arrangement.Item4, 0);
        }
        else
        {
            boardLayout.ColumnCount = 1;
            boardLayout.RowCount = 4;
            boardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, caption));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, stackedHeight));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, caption));
            boardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, stackedHeight));
            boardLayout.SetCellPosition(topCaption, new TableLayoutPanelCellPosition(0, 0));
            boardLayout.SetCellPosition(topPicture, new TableLayoutPanelCellPosition(0, 1));
            boardLayout.SetCellPosition(bottomCaption, new TableLayoutPanelCellPosition(0, 2));
            boardLayout.SetCellPosition(bottomPicture, new TableLayoutPanelCellPosition(0, 3));
            boardLayout.Padding = new Padding(0, 0, 0, arrangement.Item4);
        }

        boardLayout.ResumeLayout(true);
    }

    // Shows the rendered top / bottom views on the PCB tab (null clears them)
    private void ShowBoard(Bitmap? top, Bitmap? bottom)
    {
        topPicture.ReplaceImage(top);
        bottomPicture.ReplaceImage(bottom);
        _boardArrangement = default;
        ArrangeBoardViews();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // Sizes are final only after the form has been scaled and laid out
        ArrangeBoardViews();
        if (_startupPaths.Length > 0) await LoadPathsAsync(_startupPaths);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _settings.Save();
        base.OnFormClosed(e);
    }

    #region Event handlers

    private void MainFormDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
    }

    private async void MainFormDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) await LoadPathsAsync(paths);
    }

    private void FileListItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_suppressCheckEvents) return;
        reloadTimer.Stop();
        reloadTimer.Start();
    }

    private async void ReloadTimerTick(object? sender, EventArgs e)
    {
        reloadTimer.Stop();
        await ReloadProjectAsync();
    }

    #endregion

    #region File selection

    private async void OpenFilesClicked(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Gerber and drill files",
            Multiselect = true,
            Filter = "Gerber / drill files|*.g*;*.drl;*.txt;*.xln;*.exc;*.drd;*.cmp;*.sol;*.stc;*.sts;*.plc;*.pls;*.crc;*.crs;*.dim;*.mil;*.pho;*.art;*.zip|All files|*.*",
            InitialDirectory = _settings.LastInputFolder ?? "",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _settings.LastInputFolder = Path.GetDirectoryName(dialog.FileNames[0]);
        await LoadSourcesAsync(() => SourceFile.FromPaths(dialog.FileNames));
    }

    private async void OpenFolderClicked(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Select a folder with Gerber files", InitialDirectory = _settings.LastInputFolder ?? "" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _settings.LastInputFolder = dialog.SelectedPath;
        await LoadSourcesAsync(() => SourceFile.FromDirectory(dialog.SelectedPath));
    }

    private async void OpenZipClicked(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Title = "Select a ZIP with Gerber files", Filter = "ZIP archives|*.zip", InitialDirectory = _settings.LastInputFolder ?? "" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _settings.LastInputFolder = Path.GetDirectoryName(dialog.FileName);
        await LoadSourcesAsync(() => SourceFile.FromPaths([dialog.FileName]));
    }

    private async Task LoadPathsAsync(string[] paths)
    {
        if (paths.Length == 0) return;
        if (paths.Length == 1 && Directory.Exists(paths[0])) await LoadSourcesAsync(() => SourceFile.FromDirectory(paths[0]));
        else await LoadSourcesAsync(() => SourceFile.FromPaths(paths.Where(File.Exists)));
    }

    private async Task LoadSourcesAsync(Func<List<SourceFile>> load)
    {
        SetBusy("Reading files…");
        List<SourceFile> files;
        try
        {
            files = await Task.Run(load);
        }
        catch (Exception ex)
        {
            SetIdle();
            MessageBox.Show(this, ex.Message, "Unable to read files", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _suppressCheckEvents = true;
        fileList.BeginUpdate();
        fileList.Items.Clear();
        foreach (var file in files)
        {
            var known = file.DetectedType != LayerType.Unknown;
            var item = new ListViewItem(file.Name)
            {
                Checked = known && file.DetectedType != LayerType.Drawing,
                Tag = file,
                ForeColor = known ? SystemColors.WindowText : SystemColors.GrayText,
            };
            item.SubItems.Add(known ? $"{file.DetectedType} ({file.DetectedSide})" : "—");
            fileList.Items.Add(item);
        }

        fileList.EndUpdate();
        _suppressCheckEvents = false;
        _reportedWarnings.Clear();
        tabs.Visible = true;
        tabs.SelectedIndex = 0;
        await ReloadProjectAsync();
    }

    #endregion

    #region Rendering

    private async Task ReloadProjectAsync()
    {
        var version = ++_loadVersion;
        var selected = fileList.Items.Cast<ListViewItem>().Where(i => i.Checked).Select(i => (SourceFile)i.Tag!).ToList();
        if (selected.Count == 0)
        {
            _project = null;
            ShowBoard(null, null);
            RebuildLayerTabs();
            SetIdle();
            return;
        }

        SetBusy("Rendering board…");
        try
        {
            // Unknown files that were checked manually are treated as generic Gerber drawings.
            foreach (var f in selected.Where(f => f.DetectedType == LayerType.Unknown)) f.DetectedType = LayerType.Drawing;

            var project = await Task.Run(() => PcbProject.Load(selected));
            if (version != _loadVersion) return;
            if (project.Layers.Count == 0 || project.BoardBounds.Width <= 0)
            {
                _project = null;
                ShowBoard(null, null);
                RebuildLayerTabs();
                SetIdle("no drawable layers");
                return;
            }

            var (top, bottom) = await Task.Run(() =>
            {
                using var t = BoardPreviewRenderer.Render(project, LayerSide.Top, 1400, 1000);
                using var b = BoardPreviewRenderer.Render(project, LayerSide.Bottom, 1400, 1000);
                return (ImageConvert.FromSkia(t), ImageConvert.FromSkia(b));
            });
            if (version != _loadVersion)
            {
                top.Dispose();
                bottom.Dispose();
                return;
            }

            _project = project;
            ShowBoard(top, bottom);
            RebuildLayerTabs();

            SetIdle($"{project.BoardBounds.Width:0.##} × {project.BoardBounds.Height:0.##} mm");
            ReportParserWarnings(project);
        }
        catch (Exception ex)
        {
            if (version != _loadVersion) return;
            SetIdle();
            MessageBox.Show(this, ex.Message, "Unable to render board", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Things the Gerber parser could not handle (unknown aperture templates, negative image polarity, …) explain a
    // layer that does not look right; each one is shown once for the opened files
    private void ReportParserWarnings(PcbProject project)
    {
        var warnings = project.Layers
            .SelectMany(l => l.Image.Warnings.Select(w => $"{l.FileName}: {w}"))
            .Where(_reportedWarnings.Add)
            .ToList();
        if (warnings.Count == 0) return;

        MessageBox.Show(this, "Some features of the Gerber files are not supported and were skipped:\n\n" + string.Join("\n", warnings),
            _appTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static string GroupOf(LayerType type) => type switch
    {
        LayerType.Copper => "Copper",
        LayerType.Soldermask => "Soldermask",
        LayerType.Silkscreen => "Silkscreen",
        LayerType.Solderpaste => "Solder paste",
        LayerType.Outline or LayerType.Drill => "Drill & Outline",
        _ => "Other layers",
    };

    // Creates one tab per layer group after the PCB and Export tabs (both come from the designer);
    // the cards share the tab area so the previews are as large as possible
    private void RebuildLayerTabs()
    {
        var selectedPage = tabs.SelectedTab;
        var selectedGroup = selectedPage?.Tag as string;

        tabs.SuspendLayout();
        foreach (var page in tabs.TabPages.Cast<TabPage>().Where(p => p != boardTab && p != exportTab).ToList())
        {
            tabs.TabPages.Remove(page);
            page.Dispose();
        }

        if (_project != null)
        {
            var groups = _project.Layers
                .OrderBy(l => Array.IndexOf(TypeOrder, l.Type))
                .ThenBy(l => l.Side == LayerSide.Top ? 0 : l.Side == LayerSide.Bottom ? 2 : 1)
                .GroupBy(l => GroupOf(l.Type))
                .ToList();

            // Drill holes and the outline are rasterised once per preview size and shared by all cards
            var overlays = new OverlayCache(_project);
            foreach (var group in groups)
            {
                var grid = new CardGrid(_project.BoardBounds.Width / (double)_project.BoardBounds.Height)
                {
                    Dock = DockStyle.Fill,
                    BackColor = SystemColors.Control,
                };
                foreach (var layer in group)
                    grid.Controls.Add(new LayerCard(_project, layer, overlays, _settings.ExposesFeatures(layer.Type), _settings.ExtrasFor(layer.Type)));

                var page = new TabPage(group.Key) { Tag = group.Key, UseVisualStyleBackColor = true };
                page.Controls.Add(grid);
                tabs.TabPages.Add(page);
            }
        }

        tabs.ResumeLayout(true);
        var restore = selectedPage == boardTab || selectedPage == exportTab
            ? selectedPage
            : tabs.TabPages.Cast<TabPage>().FirstOrDefault(p => selectedGroup != null && Equals(p.Tag, selectedGroup));
        if (restore != null) tabs.SelectedTab = restore;
        UpdateExportLayers();
    }

    // The Export tab lists the copper layers; which of them are saved is chosen there
    private void UpdateExportLayers()
    {
        var layers = _project?.Layers
            .Where(l => l.Type == LayerType.Copper)
            .OrderBy(l => l.Side == LayerSide.Top ? 0 : 1)
            .ToList() ?? [];
        exportView.SetLayers(_project, layers);
    }

    private void SetBusy(string activity)
    {
        Text = $"{_appTitle} - {activity}";
        UseWaitCursor = true;
    }

    // detail: board size (or why there is no board) for the status bar
    private void SetIdle(string? detail = null)
    {
        Text = _appTitle;
        boardStatusLabel.Text = detail ?? "No board";
        UseWaitCursor = false;
    }

    #endregion
}
