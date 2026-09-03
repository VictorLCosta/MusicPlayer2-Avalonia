namespace MusicPlayer2_Avalonia.Application.Common;

public interface IAudioEngine : IDisposable
{
    event EventHandler? PlaybackEnded;

    bool IsPlaying { get; }
    TimeSpan Position { get; }
    TimeSpan Duration { get; }
    double Volume { get; set; }

    Task LoadAsync(Uri source);
    void Play();
    void Pause();
    void StopAudio();
    void Seek(TimeSpan position);
}
