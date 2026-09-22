using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

using MusicPlayer2_Avalonia.Application.Common;

namespace MusicPlayer2_Avalonia.Browser.Audio;

[SupportedOSPlatform("browser")]
internal sealed class BrowserAudioEngine : IAudioEngine
{
    private const string ModuleName = "audio-engine";
    private static Task<JSObject>? _moduleTask;

    private JSObject? _audio;
    private bool _disposed;
    private double _volume = 1;

    public event EventHandler? PlaybackEnded;

    public bool IsPlaying => _audio is not null && BrowserAudioInterop.IsPlaying(_audio);

    public TimeSpan Position => ToTimeSpan(
        _audio is null ? 0 : BrowserAudioInterop.GetCurrentTime(_audio));

    public TimeSpan Duration => ToTimeSpan(
        _audio is null ? 0 : BrowserAudioInterop.GetDuration(_audio));

    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 1);

            if (_audio is not null)
            {
                BrowserAudioInterop.SetVolume(_audio, _volume);
            }
        }
    }

    public async Task LoadAsync(Uri source)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);

        if (!source.IsAbsoluteUri || source.Scheme == Uri.UriSchemeFile)
        {
            throw new NotSupportedException(
                "No navegador, a faixa precisa ser uma URL HTTP(S) ou Blob URL.");
        }

        await EnsureAudioCreatedAsync().ConfigureAwait(false);
        await BrowserAudioInterop.LoadAsync(_audio!, source.AbsoluteUri).ConfigureAwait(false);
    }

    public void Play() => BrowserAudioInterop.Play(GetRequiredAudio());

    public void Pause()
    {
        if (_audio is not null)
        {
            BrowserAudioInterop.Pause(_audio);
        }
    }

    public void StopAudio()
    {
        if (_audio is not null)
        {
            BrowserAudioInterop.Stop(_audio);
        }
    }

    public void Seek(TimeSpan position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(position, TimeSpan.Zero);

        if (_audio is not null)
        {
            BrowserAudioInterop.SetCurrentTime(_audio, position.TotalSeconds);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_audio is not null)
        {
            BrowserAudioInterop.Destroy(_audio);
            _audio.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task EnsureAudioCreatedAsync()
    {
        _moduleTask ??= JSHost.ImportAsync(ModuleName, "/audio-engine.js");
        await _moduleTask.ConfigureAwait(false);

        if (_audio is null)
        {
            _audio = BrowserAudioInterop.Create(OnPlaybackEnded);
            BrowserAudioInterop.SetVolume(_audio, _volume);
        }
    }

    private JSObject GetRequiredAudio() => _audio
        ?? throw new InvalidOperationException("Nenhuma faixa foi carregada.");

    private void OnPlaybackEnded() => PlaybackEnded?.Invoke(this, EventArgs.Empty);

    private static TimeSpan ToTimeSpan(double seconds) =>
        double.IsFinite(seconds) && seconds >= 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.Zero;
}

[SupportedOSPlatform("browser")]
internal static partial class BrowserAudioInterop
{
    [JSImport("create", "audio-engine")]
    internal static partial JSObject Create([JSMarshalAs<JSType.Function>] Action onEnded);

    [JSImport("load", "audio-engine")]
    internal static partial Task LoadAsync(JSObject audio, string source);

    [JSImport("play", "audio-engine")]
    internal static partial void Play(JSObject audio);

    [JSImport("pause", "audio-engine")]
    internal static partial void Pause(JSObject audio);

    [JSImport("stop", "audio-engine")]
    internal static partial void Stop(JSObject audio);

    [JSImport("isPlaying", "audio-engine")]
    internal static partial bool IsPlaying(JSObject audio);

    [JSImport("getCurrentTime", "audio-engine")]
    internal static partial double GetCurrentTime(JSObject audio);

    [JSImport("setCurrentTime", "audio-engine")]
    internal static partial void SetCurrentTime(JSObject audio, double seconds);

    [JSImport("getDuration", "audio-engine")]
    internal static partial double GetDuration(JSObject audio);

    [JSImport("getVolume", "audio-engine")]
    internal static partial double GetVolume(JSObject audio);

    [JSImport("setVolume", "audio-engine")]
    internal static partial void SetVolume(JSObject audio, double volume);

    [JSImport("destroy", "audio-engine")]
    internal static partial void Destroy(JSObject audio);
}