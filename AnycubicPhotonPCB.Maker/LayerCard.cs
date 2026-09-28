using AnycubicPhotonPCB.Core.Pcb;
using AnycubicPhotonPCB.Core.Rendering;

namespace AnycubicPhotonPCB.Maker;

// Preview card of a single layer. Fills the space it is given; the layers to export are chosen on the Export tab.
// Inversion, drill holes and board outline come from the Options menu of the main form.
// Bottom layers are mirrored horizontally, as seen from below, the same as the Bottom view of the PCB tab.
// The preview is rendered lazily (only while the card is visible) at the size it is displayed
internal sealed partial class LayerCard : UserControl
{
    private readonly PcbProject _project;
    private readonly PcbLayer _layer;
    private readonly OverlayCache _overlays;
    private readonly System.Windows.Forms.Timer _renderTimer = new() { Interval = 150 };
    private bool _inverted;
    private LayerExtras _extras;
    private int _renderVersion;

    // Rasterised layer for the last rendered size, reused when only Invert / the extras change
    private Mask? _features;
    private Size _featuresSize;

    // The Visual Studio designer instantiates the base UserControl, so no parameterless constructor is needed
    public LayerCard(PcbProject project, PcbLayer layer, OverlayCache overlays, bool inverted, LayerExtras extras)
    {
        InitializeComponent();
        _renderTimer.Tick += RenderTimerTick;
        picture.SizeChanged += (_, _) => ScheduleRender();
        Disposed += (_, _) =>
        {
            _renderTimer.Dispose();
            picture.Image?.Dispose();
        };

        _project = project;
        _layer = layer;
        _overlays = overlays;
        _inverted = inverted;
        _extras = extras;

        titleLabel.Text = $"{layer.DisplayName} - {layer.FileName}";
        ScheduleRender();
    }

    public PcbLayer Layer => _layer;

    // Shows features exposed (white) instead of dark; follows Options > Photoresist
    public bool Inverted
    {
        get => _inverted;
        set
        {
            if (_inverted == value) return;
            _inverted = value;
            RenderPreview();
        }
    }

    // Drill holes / marks and board outline drawn into the layer; set for all cards from the Options menu
    public LayerExtras Extras
    {
        get => _extras;
        set
        {
            if (_extras == value) return;
            _extras = value;
            RenderPreview();
        }
    }

    // Cards on hidden tabs start rendering when their tab is shown
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        ScheduleRender();
    }

    // Resizing produces many size changes in a row; render once the size settles
    private void ScheduleRender()
    {
        _renderTimer.Stop();
        _renderTimer.Start();
    }

    private void RenderTimerTick(object? sender, EventArgs e)
    {
        _renderTimer.Stop();
        RenderPreview();
    }

    private async void RenderPreview()
    {
        if (!Visible) return;

        var project = _project;
        var layer = _layer;
        var overlays = _overlays;
        var size = LayerPreview.FitSize(picture.ClientSize, project.BoardBounds);
        if (size.IsEmpty) return;

        var version = ++_renderVersion;
        var invert = _inverted;
        var extras = _extras;
        var mirror = layer.Side == LayerSide.Bottom;
        var cached = _featuresSize == size ? _features : null;

        try
        {
            // Rasterising, composing and building the bitmap all happen off the UI thread
            var (bitmap, features) = await Task.Run(() =>
            {
                var f = cached ?? LayerPreview.Rasterize(layer.Image, project.BoardBounds, size, mirror);
                var shown = f;
                if (!extras.IsEmpty)
                {
                    // The cached rasterisation stays untouched, so changing the extras only recomposes
                    shown = new Mask(f.Width, f.Height, (byte[])f.Data.Clone());
                    overlays.Get(size, extras, mirror).Apply(shown, extras, antialias: true);
                }

                return (LayerPreview.Compose(shown, invert, project.BoardBounds, size), f);
            });

            if (version != _renderVersion || IsDisposed)
            {
                bitmap.Dispose();
                return;
            }

            _features = features;
            _featuresSize = size;
            picture.ReplaceImage(bitmap);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
