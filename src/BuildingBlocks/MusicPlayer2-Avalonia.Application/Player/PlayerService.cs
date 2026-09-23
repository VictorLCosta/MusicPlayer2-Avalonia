using Microsoft.EntityFrameworkCore;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Settings;
using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Application.Player;

public sealed class PlayerService : IDisposable
{
    private readonly IAudioEngine _audioEngine;
    private readonly SemaphoreSlim _playbackCommands = new(1, 1);
    private readonly IMusicPlayerDbContext _dbContext;
    private readonly PlaybackQueue _playbackQueue;
    private readonly AudioOutputService _audioOutput;
    private readonly SettingsService _settings;
    private readonly PlaybackSessionStore _sessionStore;
    private readonly EqualizerService _equalizer;
    private TimeSpan? _resumePosition;
    private bool _sessionRestored;
    private bool _sessionReady;
    private bool _disposed;

    public PlayerService(
        IAudioEngine audioEngine,
        IMusicPlayerDbContext dbContext,
        PlaybackQueue playbackQueue,
        AudioOutputService audioOutput,
        SettingsService settings,
        PlaybackSessionStore sessionStore,
        EqualizerService equalizer)
    {
        _audioEngine = audioEngine;
        _dbContext = dbContext;
        _playbackQueue = playbackQueue;
        _audioOutput = audioOutput;
        _settings = settings;
        _sessionStore = sessionStore;
        _equalizer = equalizer;
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
    }

    public Track? CurrentTrack { get; private set; }

    public bool IsPlaying => _audioEngine.IsPlaying;

    public IAudioSpectrumSource? SpectrumSource => _audioEngine as IAudioSpectrumSource;

    public TimeSpan Position => _resumePosition ?? _audioEngine.Position;

    public TimeSpan Duration => _audioEngine.Duration;

    public double Volume
    {
        get => _audioEngine.Volume;
        set => _audioEngine.Volume = value;
    }

    public Task PlayAsync(
        Guid trackId,
        CancellationToken cancellationToken = default) => SerializePlaybackAsync(async () =>
    {
        await LoadAndPlayAsync(trackId, cancellationToken).ConfigureAwait(false);
        _playbackQueue.SetCurrent(trackId);
    }, cancellationToken);

    public void Pause() => _audioEngine.Pause();

    public async Task<(Guid Id, string Path)?> GetAdjacentTrackAsync(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, -1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, 1);
        await _playbackCommands.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_playbackQueue.Peek(offset) is not { } id) return null;
            var track = await _dbContext.Tracks.AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id).ConfigureAwait(false);
            return track is null ? null : (track.Id, track.Source.Path);
        }
        finally { _playbackCommands.Release(); }
    }

    public void Resume()
    {
        if (CurrentTrack is null)
        {
            throw new InvalidOperationException("Nenhuma faixa foi carregada.");
        }

        if (_resumePosition is { } position)
        {
            _audioEngine.PlayFrom(position);
            _resumePosition = null;
        }
        else
            _audioEngine.Play();
    }

    public void Stop()
    {
        _audioEngine.StopAudio();
        _resumePosition = null;
        CurrentTrack = null;
    }

    public Task LoadPlaylistAsync(Guid playlistId, CancellationToken cancellationToken = default) =>
        SerializePlaybackAsync(() => LoadPlaylistCoreAsync(playlistId, cancellationToken), cancellationToken);

    private async Task LoadPlaylistCoreAsync(
        Guid playlistId,
        CancellationToken cancellationToken = default)
    {
        await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        var playlist = await _dbContext.Playlists
            .Include(entity => entity.PlaylistItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == playlistId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Playlist '{playlistId}' was not found.");

        _playbackQueue.Replace(playlist.PlaylistItems
            .OrderBy(item => item.Position)
            .Select(item => item.TrackId));
        if (!_settings.Current.ContinuePlaybackOnPlaylistChange)
            Stop();
        else if (CurrentTrack is { } current && _playbackQueue.TrackIds.Contains(current.Id))
            _playbackQueue.SetCurrent(current.Id);
    }

    public Task NextAsync(CancellationToken cancellationToken = default) => SerializePlaybackAsync(async () =>
    {
        var trackId = _playbackQueue.Peek(1) ?? (_playbackQueue.CurrentTrackId is null
            ? _playbackQueue.TrackIds[0] : Guid.Empty);
        if (trackId == Guid.Empty)
        {
            return;
        }

        await LoadAndPlayAsync(trackId, cancellationToken).ConfigureAwait(false);
        _playbackQueue.TryMoveNext(out _);
    }, cancellationToken);

    public Task PreviousAsync(CancellationToken cancellationToken = default) => SerializePlaybackAsync(async () =>
    {
        if (_playbackQueue.Peek(-1) is not { } trackId)
        {
            return;
        }

        await LoadAndPlayAsync(trackId, cancellationToken).ConfigureAwait(false);
        _playbackQueue.TryMovePrevious(out _);
    }, cancellationToken);

    public void Seek(TimeSpan position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(position, TimeSpan.Zero);

        if (_resumePosition is not null)
            _resumePosition = position;
        else
            _audioEngine.Seek(position);
    }

    public Task RestoreSessionAsync() => SerializePlaybackAsync(RestoreSessionOnceAsync);

    private async Task RestoreSessionOnceAsync()
    {
        await _equalizer.InitializeAsync().ConfigureAwait(false);
        if (_sessionRestored) return;
        _sessionRestored = true;
        try { await RestoreSessionCoreAsync().ConfigureAwait(false); }
        finally { _sessionReady = true; }
    }

    private async Task RestoreSessionCoreAsync()
    {
        var settings = await _settings.LoadAsync().ConfigureAwait(false);
        if (!settings.RememberPlaybackPosition)
        {
            await _sessionStore.SaveAsync(null).ConfigureAwait(false);
            return;
        }
        var session = await _sessionStore.LoadAsync().ConfigureAwait(false);
        if (session?.TrackId is not { } trackId) return;
        var track = await _dbContext.Tracks.Include(item => item.Artist).Include(item => item.Album)
            .FirstOrDefaultAsync(item => item.Id == trackId).ConfigureAwait(false);
        if (track is null || !File.Exists(track.Source.Path)) return;
        await _audioEngine.LoadAsync(new Uri(track.Source.Path)).ConfigureAwait(false);
        var position = double.IsFinite(session.PositionSeconds) ? session.PositionSeconds : 0;
        _resumePosition = TimeSpan.FromSeconds(Math.Clamp(position, 0, Math.Max(0, track.Duration.TotalSeconds - 1)));
        var queueIds = session.Queue ?? [];
        var existingIds = await _dbContext.Tracks.Where(item => queueIds.Contains(item.Id))
            .Select(item => item.Id).ToListAsync().ConfigureAwait(false);
        _playbackQueue.Replace(queueIds.Where(existingIds.Contains));
        _playbackQueue.SetCurrent(trackId);
        CurrentTrack = track;
    }

    public Task SaveSessionAsync()
    {
        if (!_sessionReady && CurrentTrack is null) return Task.CompletedTask;
        var seconds = Math.Max(0, Position.TotalSeconds);
        if (!double.IsFinite(seconds)) seconds = 0;
        if (CurrentTrack is { } track && seconds >= track.Duration.TotalSeconds - 1) seconds = 0;
        return _sessionStore.SaveAsync(_settings.Current.RememberPlaybackPosition && CurrentTrack is not null
            ? new PlaybackSession(CurrentTrack.Id, seconds, _playbackQueue.TrackIds.ToArray())
            : null);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _audioEngine.PlaybackEnded -= OnPlaybackEnded;
        _disposed = true;
        _playbackCommands.Dispose();
    }

    private async Task SerializePlaybackAsync(Func<Task> command, CancellationToken cancellationToken = default)
    {
        await _playbackCommands.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await command().ConfigureAwait(false); }
        finally { _playbackCommands.Release(); }
    }

    private async Task LoadAndPlayAsync(Guid trackId, CancellationToken cancellationToken)
    {
        await _settings.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (CurrentTrack is not null)
            await SaveSessionAsync().ConfigureAwait(false);
        var track = await _dbContext.Tracks
            .Include(item => item.Artist)
            .Include(item => item.Album)
            .FirstOrDefaultAsync(item => item.Id == trackId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Track '{trackId}' was not found.");

        if (!File.Exists(track.Source.Path))
        {
            throw new FileNotFoundException(
                "O arquivo de audio da faixa nao foi encontrado.",
                track.Source.Path);
        }

        await _audioOutput.InitializeAsync().ConfigureAwait(false);
        await _equalizer.InitializeAsync().ConfigureAwait(false);
        await _audioEngine.LoadAsync(new Uri(track.Source.Path)).ConfigureAwait(false);
        _resumePosition = null;
        _audioEngine.Play();
        CurrentTrack = track;
    }

    private void OnPlaybackEnded(object? sender, EventArgs eventArgs)
    {
        if (!_disposed)
        {
            _ = AdvanceAfterPlaybackEndedAsync();
        }
    }

    private async Task AdvanceAfterPlaybackEndedAsync()
    {
        try
        {
            await NextAsync().ConfigureAwait(false);
        }
        catch
        {
            Stop();
        }
    }
}
