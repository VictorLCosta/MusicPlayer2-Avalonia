using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace MusicPlayer2_Avalonia.Application.Player;

public sealed class PlayerService : IDisposable
{
    private readonly IAudioEngine _audioEngine;
    private readonly IMusicPlayerDbContext _dbContext;
    private readonly PlaybackQueue _playbackQueue;
    private bool _disposed;

    public PlayerService(
        IAudioEngine audioEngine,
        IMusicPlayerDbContext dbContext,
        PlaybackQueue playbackQueue)
    {
        _audioEngine = audioEngine;
        _dbContext = dbContext;
        _playbackQueue = playbackQueue;
        _audioEngine.PlaybackEnded += OnPlaybackEnded;
    }

    public Track? CurrentTrack { get; private set; }

    public bool IsPlaying => _audioEngine.IsPlaying;

    public TimeSpan Position => _audioEngine.Position;

    public TimeSpan Duration => _audioEngine.Duration;

    public double Volume
    {
        get => _audioEngine.Volume;
        set => _audioEngine.Volume = value;
    }

    public async Task PlayAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        await LoadAndPlayAsync(trackId, cancellationToken).ConfigureAwait(false);
        _playbackQueue.SetCurrent(trackId);
    }

    public void Pause() => _audioEngine.Pause();

    public void Resume()
    {
        if (CurrentTrack is null)
        {
            throw new InvalidOperationException("Nenhuma faixa foi carregada.");
        }

        _audioEngine.Play();
    }

    public void Stop()
    {
        _audioEngine.StopAudio();
        CurrentTrack = null;
    }

    public async Task LoadPlaylistAsync(
        Guid playlistId,
        CancellationToken cancellationToken = default)
    {
        var playlist = await _dbContext.Playlists
            .Include(entity => entity.PlaylistItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == playlistId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Playlist '{playlistId}' was not found.");

        _playbackQueue.Replace(playlist.PlaylistItems
            .OrderBy(item => item.Position)
            .Select(item => item.TrackId));
    }

    public async Task NextAsync(CancellationToken cancellationToken = default)
    {
        if (!_playbackQueue.TryMoveNext(out var trackId))
        {
            return;
        }

        await LoadAndPlayAsync(trackId, cancellationToken).ConfigureAwait(false);
    }

    public async Task PreviousAsync(CancellationToken cancellationToken = default)
    {
        if (!_playbackQueue.TryMovePrevious(out var trackId))
        {
            return;
        }

        await LoadAndPlayAsync(trackId, cancellationToken).ConfigureAwait(false);
    }

    public void Seek(TimeSpan position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(position, TimeSpan.Zero);

        _audioEngine.Seek(position);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _audioEngine.PlaybackEnded -= OnPlaybackEnded;
        _disposed = true;
    }

    private async Task LoadAndPlayAsync(Guid trackId, CancellationToken cancellationToken)
    {
        var track = await _dbContext.Tracks
            .FirstOrDefaultAsync(item => item.Id == trackId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Track '{trackId}' was not found.");

        if (!File.Exists(track.Source.Path))
        {
            throw new FileNotFoundException(
                "O arquivo de audio da faixa nao foi encontrado.",
                track.Source.Path);
        }

        await _audioEngine.LoadAsync(new Uri(track.Source.Path)).ConfigureAwait(false);
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
