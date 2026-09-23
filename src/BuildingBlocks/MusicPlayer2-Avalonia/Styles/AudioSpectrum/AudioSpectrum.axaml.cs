using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using MusicPlayer2_Avalonia.Application.Common;

namespace MusicPlayer2_Avalonia.Styles.AudioSpectrum;

public partial class AudioSpectrum : UserControl
{
    public static readonly StyledProperty<IAudioSpectrumSource?> SourceProperty =
        AvaloniaProperty.Register<AudioSpectrum, IAudioSpectrumSource?>(nameof(Source));

    public static readonly StyledProperty<IBrush?> BarBrushProperty =
        AvaloniaProperty.Register<AudioSpectrum, IBrush?>(nameof(BarBrush), Brushes.DodgerBlue);

    public static readonly StyledProperty<bool> ShowReflectionProperty =
        AvaloniaProperty.Register<AudioSpectrum, bool>(nameof(ShowReflection), true);

    public bool ShowReflection { get => GetValue(ShowReflectionProperty); set => SetValue(ShowReflectionProperty, value); }

    private readonly float[] _target = new float[64];
    private readonly float[] _levels = new float[64];
    private readonly float[] _peaks = new float[64];

    private readonly DispatcherTimer _timer;

    public IAudioSpectrumSource? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public IBrush? BarBrush { get => GetValue(BarBrushProperty); set => SetValue(BarBrushProperty, value); }

    static AudioSpectrum() => AffectsRender<AudioSpectrum>(BarBrushProperty, ShowReflectionProperty);

    public AudioSpectrum()
    {
        InitializeComponent();
        ClipToBounds = true;
        IsHitTestVisible = false;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000d / 30) };
        _timer.Tick += (_, _) => UpdateSpectrum();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();

        Array.Clear(_levels);
        Array.Clear(_peaks);

        base.OnDetachedFromVisualTree(e);
    }

    private void UpdateSpectrum()
    {
        if (!IsEffectivelyVisible) return;

        Array.Clear(_target);

        Source?.CopySpectrum(_target);
        for (int i = 0; i < _levels.Length; i++)
        {
            float target = float.IsFinite(_target[i]) ? Math.Clamp(_target[i], 0, 1) : 0;
            _levels[i] += (target - _levels[i]) * (target > _levels[i] ? 0.7f : 0.18f);
            _peaks[i] = Math.Max(_levels[i], _peaks[i] - 0.018f);
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        base.Render(context);
        var brush = BarBrush;

        if (brush is null) return;

        double step = Bounds.Width / _levels.Length;

        if (step <= 0 || Bounds.Height <= 0) return;

        double width = Math.Max(0.5, step * 0.65);
        double center = ShowReflection ? Bounds.Height * 0.65 : Bounds.Height;
        double range = Math.Max(0, center - 4);

        for (int i = 0; i < _levels.Length; i++)
        {
            double x = i * step + (step - width) / 2;
            double height = Math.Max(1, _levels[i] * range);
            context.FillRectangle(brush, new Rect(x, center - height, width, height));
            if (ShowReflection)
                using (context.PushOpacity(0.4))
                    context.FillRectangle(brush, new Rect(x, center + 2, width, Math.Max(1, height * 0.4)));
            if (_peaks[i] > 0.02)
                context.FillRectangle(brush, new Rect(x, center - _peaks[i] * range - 3, width, 1));
        }
    }
}
