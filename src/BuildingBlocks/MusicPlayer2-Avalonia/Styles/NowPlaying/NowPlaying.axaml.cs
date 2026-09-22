using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Playlist.Models;

namespace MusicPlayer2_Avalonia.Styles.NowPlaying;

public partial class NowPlaying : UserControl
{
    public static readonly StyledProperty<IAudioSpectrumSource?> SpectrumSourceProperty =
        AvaloniaProperty.Register<NowPlaying, IAudioSpectrumSource?>(nameof(SpectrumSource));

    public IAudioSpectrumSource? SpectrumSource
    {
        get => GetValue(SpectrumSourceProperty);
        set => SetValue(SpectrumSourceProperty, value);
    }

    public static readonly StyledProperty<bool> ShowAlbumCoverProperty =
        AvaloniaProperty.Register<NowPlaying, bool>(nameof(ShowAlbumCover), true);

    public bool ShowAlbumCover
    {
        get => GetValue(ShowAlbumCoverProperty);
        set => SetValue(ShowAlbumCoverProperty, value);
    }

    public static readonly StyledProperty<ListTrackDto?> CurrentTrackProperty =
        AvaloniaProperty.Register<NowPlaying, ListTrackDto?>(nameof(CurrentTrack));

    public static readonly StyledProperty<double> PositionSecondsProperty =
        AvaloniaProperty.Register<NowPlaying, double>(
            nameof(PositionSeconds),
            defaultValue: 0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IImage?> ArtworkProperty =
        AvaloniaProperty.Register<NowPlaying, IImage?>(nameof(Artwork), null);

    public static readonly StyledProperty<string> DurationTextProperty =
        AvaloniaProperty.Register<NowPlaying, string>(nameof(DurationText), "0:00");

    public static readonly StyledProperty<string> PositionTextProperty =
        AvaloniaProperty.Register<NowPlaying, string>(nameof(PositionText), "0:00");

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<NowPlaying, string?>(nameof(ErrorMessage), null);

    public static readonly StyledProperty<double> DurationSecondsProperty =
        AvaloniaProperty.Register<NowPlaying, double>(nameof(DurationSeconds), 0);

    public static readonly StyledProperty<bool> IsPlayingProperty =
        AvaloniaProperty.Register<NowPlaying, bool>(nameof(IsPlaying));

    public static readonly StyledProperty<ICommand?> TogglePlaybackCommandProperty =
        AvaloniaProperty.Register<NowPlaying, ICommand?>(
            nameof(TogglePlaybackCommand));

    public static readonly StyledProperty<ICommand?> PreviousCommandProperty =
        AvaloniaProperty.Register<NowPlaying, ICommand?>(
            nameof(PreviousCommand));

    public static readonly StyledProperty<ICommand?> NextCommandProperty =
        AvaloniaProperty.Register<NowPlaying, ICommand?>(
            nameof(NextCommand));

    public ListTrackDto? CurrentTrack
    {
        get => GetValue(CurrentTrackProperty);
        set => SetValue(CurrentTrackProperty, value);
    }

    public double PositionSeconds
    {
        get => GetValue(PositionSecondsProperty);
        set => SetValue(PositionSecondsProperty, value);
    }

    public bool IsPlaying
    {
        get => GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public ICommand? TogglePlaybackCommand
    {
        get => GetValue(TogglePlaybackCommandProperty);
        set => SetValue(TogglePlaybackCommandProperty, value);
    }

    public ICommand? PreviousCommand
    {
        get => GetValue(PreviousCommandProperty);
        set => SetValue(PreviousCommandProperty, value);
    }

    public ICommand? NextCommand
    {
        get => GetValue(NextCommandProperty);
        set => SetValue(NextCommandProperty, value);
    }

    public IImage? Artwork
    {
        get => GetValue(ArtworkProperty);
        set => SetValue(ArtworkProperty, value);
    }

    public double DurationSeconds
    {
        get => GetValue(DurationSecondsProperty);
        set => SetValue(DurationSecondsProperty, value);
    }

    public string PositionText
    {
        get => GetValue(PositionTextProperty);
        set => SetValue(PositionTextProperty, value);
    }

    public string DurationText
    {
        get => GetValue(DurationTextProperty);
        set => SetValue(DurationTextProperty, value);
    }

    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }
    public NowPlaying()
    {
        InitializeComponent();
    }
}