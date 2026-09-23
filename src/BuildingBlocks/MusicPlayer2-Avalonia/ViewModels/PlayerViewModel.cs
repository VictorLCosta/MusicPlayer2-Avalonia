using System.Globalization;

using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Application.Playlist.Models;
using MusicPlayer2_Avalonia.Application.Settings;

namespace MusicPlayer2_Avalonia.ViewModels;

internal sealed partial class PlayerViewModel : ViewModelBase
{
    public EqualizerService Equalizer { get; }
    private readonly PlayerService _service;
    public IAudioSpectrumSource? SpectrumSource => _service.SpectrumSource;
    private readonly PlaybackQueue _queue;
    private readonly DispatcherTimer _timer;
    private bool _synchronizing;
    private readonly SettingsService _settings;
    private int _checkpointTicks;
    private bool _savingSession;
    private bool _initialized;
    private readonly IAlbumArtworkReader? _artworkReader;
    private int _artworkRequest;

    public PlayerViewModel(PlayerService service, PlaybackQueue queue, SettingsService settings,
        IEnumerable<IAlbumArtworkReader> artworkReaders, EqualizerService equalizer)
    {
        Equalizer = equalizer;
        _service = service;
        _queue = queue;
        _settings = settings;
        _artworkReader = artworkReaders.FirstOrDefault();
        _settings.Changed += SettingsChanged;
        ShowAlbumCover = settings.Current.ShowAlbumCover;
        VolumePercent = Math.Clamp(service.Volume * 100, 0, 100);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += OnTick;
    }

    [ObservableProperty] public partial ListTrackDto? CurrentTrack { get; private set; }
    [ObservableProperty] public partial IImage? Artwork { get; set; }
    [ObservableProperty] public partial bool ShowAlbumCover { get; private set; }
    [ObservableProperty] public partial bool IsPlaying { get; private set; }
    [ObservableProperty] public partial double DurationSeconds { get; private set; }
    [ObservableProperty] public partial double PositionSeconds { get; set; }
    [ObservableProperty] public partial string PositionText { get; private set; } = "0:00";
    [ObservableProperty] public partial string DurationText { get; private set; } = "0:00";
    [ObservableProperty] public partial string? ErrorMessage { get; private set; }
    [ObservableProperty] public partial bool IsBusy { get; private set; }
    [ObservableProperty] public partial string AudioSummary { get; private set; } = "Nenhuma música em reprodução";
    [ObservableProperty] public partial double VolumePercent { get; set; }

    partial void OnVolumePercentChanged(double value)
    {
        try
        {
            _service.Volume = Math.Clamp(value, 0, 100) / 100d;
            ErrorMessage = null;
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    private bool CanControl() => CurrentTrack is not null && !IsBusy;
    partial void OnCurrentTrackChanged(ListTrackDto? value) => UpdateCommands();
    partial void OnIsBusyChanged(bool value) => UpdateCommands();

    private void UpdateCommands()
    {
        TogglePlaybackCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }

    partial void OnPositionSecondsChanged(double value)
    {
        if (_synchronizing || !CanControl() || !double.IsFinite(value)) return;
        try
        {
            ErrorMessage = null;
            _service.Seek(TimeSpan.FromSeconds(Math.Clamp(value, 0, DurationSeconds)));
            PositionText = FormatTime(value);
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand(CanExecute = nameof(CanControl))]
    private void TogglePlayback()
    {
        try
        {
            ErrorMessage = null;
            if (_service.IsPlaying) _service.Pause(); else _service.Resume();
            Synchronize();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand(CanExecute = nameof(CanControl))]
    private void Stop()
    {
        try { _service.Stop(); Synchronize(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand(CanExecute = nameof(CanControl))]
    private Task PreviousAsync() => RunAsync(() => _service.PreviousAsync());

    [RelayCommand(CanExecute = nameof(CanControl))]
    private Task NextAsync() => RunAsync(() => _service.NextAsync());

    public Task PlayTrackAsync(ListTrackDto track, IEnumerable<ListTrackDto> tracks)
    {
        var ids = tracks.Select(item => item.TrackId).ToArray();
        return RunAsync(async () =>
        {
            await _service.PlayAsync(track.TrackId);
            _queue.Replace(ids);
            _queue.SetCurrent(track.TrackId);
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        try { ErrorMessage = null; await action(); }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; Synchronize(); }
    }

    public void StartUpdating() { Synchronize(); _timer.Start(); }
    public void StopUpdating() => _timer.Stop();
    private void OnTick(object? sender, EventArgs e)
    {
        Synchronize();
        if (++_checkpointTicks >= 20)
        {
            _checkpointTicks = 0;
            _ = SaveSessionAsync();
        }
    }

    public Task InitializeAsync() => RunAsync(async () =>
    {
        await _service.RestoreSessionAsync();
        _initialized = true;
        ShowAlbumCover = _settings.Current.ShowAlbumCover;
    });

    public async Task SaveSessionAsync()
    {
        if (_savingSession || !_initialized) return;
        _savingSession = true;
        try { await _service.SaveSessionAsync(); }
        catch (Exception) { ErrorMessage = "Não foi possível salvar a posição da reprodução."; }
        finally { _savingSession = false; }
    }

    private void SettingsChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(() =>
    {
        ShowAlbumCover = _settings.Current.ShowAlbumCover;
        _ = SaveSessionAsync();
    });

    private void Synchronize()
    {
        _synchronizing = true;
        try
        {
            var track = _service.CurrentTrack;
            if (track?.Id != CurrentTrack?.TrackId)
            {
                var audio = track?.AudioProperties;
                AudioSummary = track is null ? "Nenhuma música em reprodução" : string.Join("  ",
                    new[] {
                        Path.GetExtension(track.Source.Path).TrimStart('.').ToUpperInvariant(),
                        audio?.SampleRateHz > 0 ? $"{audio.SampleRateHz / 1000d:0.#} kHz" : null,
                        audio?.BitrateKbps > 0 ? $"{audio.BitrateKbps} kbps" : null,
                        audio?.Channels == 2 ? "Estéreo" : audio?.Channels == 1 ? "Mono" : null
                    }.Where(value => !string.IsNullOrWhiteSpace(value)));
                CurrentTrack = track is null ? null : new ListTrackDto(
                    Guid.Empty, track.Id, 0, track.Title, track.Artist?.Name,
                    track.Album?.Title, track.Duration, track.SourceFileSizeBytes ?? 0);
                var previous = Artwork as IDisposable;
                Artwork = null;
                previous?.Dispose();
                var request = ++_artworkRequest;
                if (track is not null && _artworkReader is not null)
                    _ = LoadArtworkAsync(track.Source.Path, request);
            }
            IsPlaying = _service.IsPlaying;
            var duration = _service.Duration.TotalSeconds;
            DurationSeconds = track is null ? 0 : Math.Max(0, duration > 0 ? duration : track.Duration.TotalSeconds);
            PositionSeconds = track is null ? 0 : Math.Clamp(_service.Position.TotalSeconds, 0, DurationSeconds);
            PositionText = FormatTime(PositionSeconds);
            DurationText = FormatTime(DurationSeconds);
        }
        finally { _synchronizing = false; }
    }

    private static string FormatTime(double seconds) => TimeSpan.FromSeconds(seconds)
        .ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss", CultureInfo.InvariantCulture);

    private async Task LoadArtworkAsync(string path, int request)
    {
        try
        {
            var bytes = await _artworkReader!.ReadAsync(path);
            if (bytes is not { Length: > 0 } || request != _artworkRequest) return;
            using var stream = new MemoryStream(bytes, writable: false);
            Artwork = Bitmap.DecodeToWidth(stream, 640);
        }
        catch (Exception)
        {
            // Missing or malformed artwork must not interrupt playback.
        }
    }

    public override void Dispose()
    {
        _artworkRequest++;
        (Artwork as IDisposable)?.Dispose();
        Artwork = null;
        _settings.Changed -= SettingsChanged;
        _timer.Stop();
        _timer.Tick -= OnTick;
        base.Dispose();
    }
}
