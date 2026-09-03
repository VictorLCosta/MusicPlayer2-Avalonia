using LibVLCSharp.Shared;

using MusicPlayer2_Avalonia.Application.Common;

namespace MusicPlayer2_Avalonia.Infrastructure.AudioEngine;

public class VlcAudioEngine : IAudioEngine
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private Media? _media;
    
    private bool _disposed;

    public event EventHandler? PlaybackEnded;

    public VlcAudioEngine()
    {
        Core.Initialize();
        _libVlc = new LibVLC();
        _player = new MediaPlayer(_libVlc);
        _player.EndReached += OnPlaybackEnded;
    }

    public bool IsPlaying => _player.IsPlaying;

    public TimeSpan Position => TimeSpan.FromMilliseconds(_player.Time);

    public TimeSpan Duration => TimeSpan.FromMilliseconds(_player.Length);

    public double Volume 
    { 
        get => _player.Volume / 100d; 
        set => _player.Volume = (int)Math.Clamp(value * 100, 0, 100); 
    }

    public Task LoadAsync(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _player.Stop();

        _media?.Dispose();
        _media = new Media(_libVlc, source.AbsoluteUri, FromType.FromLocation);

        return Task.CompletedTask;
    }

    public void Pause() => _player.SetPause(true);

    public void Play()
    {
        if (_media is null)
            throw new InvalidOperationException("Nenhuma faixa foi carregada.");

        _player.Play(_media);
    }

    public void Seek(TimeSpan position)
    {
        _player.Time = (long)position.TotalMilliseconds;
    }

    public void StopAudio() => _player.Stop();

    private void OnPlaybackEnded(object? sender, EventArgs eventArgs) =>
        PlaybackEnded?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
            return;

        _media?.Dispose();
        _player.EndReached -= OnPlaybackEnded;
        _player.Dispose();
        _libVlc.Dispose();

        _disposed = true;
    }
}
