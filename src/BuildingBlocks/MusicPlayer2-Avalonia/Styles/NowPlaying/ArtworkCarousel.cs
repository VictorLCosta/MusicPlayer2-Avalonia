using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace MusicPlayer2_Avalonia.Styles.NowPlaying;

public sealed class ArtworkCarousel : UserControl
{
    public static readonly StyledProperty<IImage?> ArtworkProperty = AvaloniaProperty.Register<ArtworkCarousel, IImage?>(nameof(Artwork));
    public static readonly StyledProperty<IImage?> PreviousArtworkProperty = AvaloniaProperty.Register<ArtworkCarousel, IImage?>(nameof(PreviousArtwork));
    public static readonly StyledProperty<IImage?> NextArtworkProperty = AvaloniaProperty.Register<ArtworkCarousel, IImage?>(nameof(NextArtwork));
    public static readonly StyledProperty<Guid?> TrackIdProperty = AvaloniaProperty.Register<ArtworkCarousel, Guid?>(nameof(TrackId));
    public static readonly StyledProperty<bool> HasPreviousTrackProperty = AvaloniaProperty.Register<ArtworkCarousel, bool>(nameof(HasPreviousTrack));
    public static readonly StyledProperty<bool> HasNextTrackProperty = AvaloniaProperty.Register<ArtworkCarousel, bool>(nameof(HasNextTrack));
    public static readonly StyledProperty<ICommand?> PreviousCommandProperty = AvaloniaProperty.Register<ArtworkCarousel, ICommand?>(nameof(PreviousCommand));
    public static readonly StyledProperty<ICommand?> NextCommandProperty = AvaloniaProperty.Register<ArtworkCarousel, ICommand?>(nameof(NextCommand));
    public static readonly StyledProperty<double> CaptionOpacityProperty = AvaloniaProperty.Register<ArtworkCarousel, double>(nameof(CaptionOpacity), 1);

    public IImage? Artwork { get => GetValue(ArtworkProperty); set => SetValue(ArtworkProperty, value); }
    public IImage? PreviousArtwork { get => GetValue(PreviousArtworkProperty); set => SetValue(PreviousArtworkProperty, value); }
    public IImage? NextArtwork { get => GetValue(NextArtworkProperty); set => SetValue(NextArtworkProperty, value); }
    public Guid? TrackId { get => GetValue(TrackIdProperty); set => SetValue(TrackIdProperty, value); }
    public bool HasPreviousTrack { get => GetValue(HasPreviousTrackProperty); set => SetValue(HasPreviousTrackProperty, value); }
    public bool HasNextTrack { get => GetValue(HasNextTrackProperty); set => SetValue(HasNextTrackProperty, value); }
    public ICommand? PreviousCommand { get => GetValue(PreviousCommandProperty); set => SetValue(PreviousCommandProperty, value); }
    public ICommand? NextCommand { get => GetValue(NextCommandProperty); set => SetValue(NextCommandProperty, value); }
    public double CaptionOpacity { get => GetValue(CaptionOpacityProperty); private set => SetValue(CaptionOpacityProperty, value); }

    private readonly Image _background = new() { Stretch = Stretch.UniformToFill, Margin = new Thickness(-24), Effect = new BlurEffect { Radius = 24 } };
    private readonly Image _incomingBackground = new() { Stretch = Stretch.UniformToFill, Margin = new Thickness(-24), Effect = new BlurEffect { Radius = 24 } };
    private readonly Image[] _images = [new(), new(), new()];
    private readonly Grid[] _slides = [new(), new(), new()];
    private readonly TextBlock[] _placeholders = [new(), new(), new()];
    private readonly TranslateTransform[] _translations = [new(), new(), new()];
    private readonly DispatcherTimer _animation = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private TaskCompletionSource<bool>? _completion;
    private Action? _animateFrame;
    private IPointer? _pointer;
    private Point _origin;
    private Point _lastPoint;
    private ulong _lastTimestamp;
    private double _velocity;
    private double _offset;
    private bool _horizontal;
    private bool _settling;
    private bool _committing;
    private int _generation;

    public ArtworkCarousel()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent;
        var grid = new Grid();
        grid.Children.Add(_background);
        grid.Children.Add(_incomingBackground);
        for (var i = 0; i < 3; i++)
        {
            _images[i].Stretch = Stretch.Uniform;
            _placeholders[i].Text = "♪";
            _placeholders[i].FontSize = 64;
            _placeholders[i].Foreground = Brushes.White;
            _placeholders[i].HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center;
            _placeholders[i].VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center;
            _slides[i].RenderTransform = _translations[i];
            _slides[i].Children.Add(_placeholders[i]);
            _slides[i].Children.Add(_images[i]);
            grid.Children.Add(_slides[i]);
        }
        Content = grid;
        _animation.Tick += (_, _) => _animateFrame?.Invoke();
        PointerPressed += Pressed;
        PointerMoved += Moved;
        PointerReleased += Released;
        PointerCaptureLost += (_, _) => { if (_pointer is not null) Reset(); };
        SizeChanged += (_, _) => Reset();
        RefreshImages();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        base.OnPropertyChanged(change);
        if (change.Property == TrackIdProperty && !_committing) Reset();
        if (change.Property == IsVisibleProperty && !IsVisible) Reset();
        if (!_settling && _pointer is null && (change.Property == ArtworkProperty || change.Property == PreviousArtworkProperty ||
            change.Property == NextArtworkProperty || change.Property == HasNextTrackProperty || change.Property == HasPreviousTrackProperty)) RefreshImages();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Reset();
        base.OnDetachedFromVisualTree(e);
    }

    private void RefreshImages()
    {
        IImage?[] sources = [PreviousArtwork, Artwork, NextArtwork];
        for (var i = 0; i < 3; i++)
        {
            _images[i].Source = sources[i];
            _placeholders[i].IsVisible = sources[i] is null;
        }
        _slides[0].IsVisible = HasPreviousTrack;
        _slides[2].IsVisible = HasNextTrack;
        _background.Source = Artwork;
        SetOffset(0);
    }

    private void SetOffset(double offset)
    {
        _offset = offset;
        var width = Math.Max(1, Bounds.Width);
        for (var i = 0; i < 3; i++) _translations[i].X = (i - 1) * width + offset;
        var progress = Math.Clamp(Math.Abs(offset) / width, 0, 1);
        _incomingBackground.Source = offset < 0 ? _images[2].Source : _images[0].Source;
        _background.Opacity = 0.55 * (1 - progress);
        _incomingBackground.Opacity = 0.55 * progress;
    }

    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (_settling || _pointer is not null || TrackId is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _origin = _lastPoint = e.GetPosition(this);
        _lastTimestamp = e.Timestamp;
        _velocity = 0;
        _horizontal = false;
        _pointer = e.Pointer;
        _pointer.Capture(this);
    }

    private void Moved(object? sender, PointerEventArgs e)
    {
        if (e.Pointer != _pointer) return;
        var point = e.GetPosition(this);
        var delta = point - _origin;
        if (!_horizontal)
        {
            if (Math.Abs(delta.Y) > 10 && Math.Abs(delta.Y) > Math.Abs(delta.X)) { Reset(); return; }
            if (Math.Abs(delta.X) < 8) return;
            _horizontal = true;
        }
        var elapsed = e.Timestamp - _lastTimestamp;
        if (elapsed > 0) _velocity = (point.X - _lastPoint.X) * 1000 / elapsed;
        _lastTimestamp = e.Timestamp;
        _lastPoint = point;
        var available = delta.X < 0 ? HasNextTrack && NextCommand?.CanExecute(null) == true
            : HasPreviousTrack && PreviousCommand?.CanExecute(null) == true;
        SetOffset(Math.Clamp(delta.X * (available ? 1 : 0.18), -Bounds.Width, Bounds.Width));
        CaptionOpacity = 1 - 0.7 * Math.Min(1, Math.Abs(_offset) / Math.Max(1, Bounds.Width));
        e.Handled = true;
    }

    private async void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _pointer) return;
        var pointer = _pointer;
        _pointer = null;
        pointer.Capture(null);
        if (!_horizontal) { Reset(); return; }
        e.Handled = true;
        _settling = true;
        var generation = _generation;
        var next = _offset < 0;
        var command = next ? NextCommand : PreviousCommand;
        var velocity = e.Timestamp - _lastTimestamp <= 100 ? _velocity : 0;
        var commit = (next ? HasNextTrack : HasPreviousTrack) && command?.CanExecute(null) == true &&
            (Math.Abs(_offset) >= Bounds.Width * 0.25 ||
             Math.Abs(_offset) >= 24 && Math.Abs(velocity) > 650 && Math.Sign(velocity) == Math.Sign(_offset));
        try
        {
            if (!await AnimateToAsync(commit ? (next ? -Bounds.Width : Bounds.Width) : 0, commit ? 0 : 1, 230)) return;
            if (commit)
            {
                if (!IsEffectivelyVisible || command?.CanExecute(null) != true) return;
                var oldTrack = TrackId;
                _committing = true;
                if (command is IAsyncRelayCommand asyncCommand) await asyncCommand.ExecuteAsync(null);
                else command!.Execute(null);
                if (generation != _generation) return;
                _committing = false;
                if (TrackId != oldTrack) RefreshImages();
                await AnimateToAsync(0, 1, 150);
            }
        }
        catch (Exception ex) { Debug.WriteLine(ex); }
        finally { if (generation == _generation) Reset(); }
    }

    private Task<bool> AnimateToAsync(double target, double caption, int milliseconds)
    {
        var start = _offset;
        var opacity = CaptionOpacity;
        var clock = Stopwatch.StartNew();
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completion = completion;
        _animateFrame = () =>
        {
            var progress = Math.Clamp(clock.Elapsed.TotalMilliseconds / milliseconds, 0, 1);
            var eased = 1 - Math.Pow(1 - progress, 3);
            SetOffset(start + (target - start) * eased);
            CaptionOpacity = opacity + (caption - opacity) * eased;
            if (progress < 1) return;
            _animation.Stop();
            _animateFrame = null;
            _completion = null;
            completion.TrySetResult(true);
        };
        _animation.Start();
        return completion.Task;
    }

    private void Reset()
    {
        _generation++;
        _animation.Stop();
        _animateFrame = null;
        _completion?.TrySetResult(false);
        _completion = null;
        var pointer = _pointer;
        _pointer = null;
        pointer?.Capture(null);
        _settling = _committing = _horizontal = false;
        CaptionOpacity = 1;
        RefreshImages();
    }
}
