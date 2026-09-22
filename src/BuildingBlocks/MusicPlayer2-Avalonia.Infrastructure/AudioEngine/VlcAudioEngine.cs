using LibVLCSharp.Shared;

using MusicPlayer2_Avalonia.Application.Common;

namespace MusicPlayer2_Avalonia.Infrastructure.AudioEngine;

public class VlcAudioEngine : IAudioEngine, IAudioOutputControl, IAudioSpectrumSource
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private readonly VlcSpectrumReader _spectrum;
    private Media? _media;
    private bool _hasStarted;
    private string? _requestedOutputDeviceId;

    private bool _disposed;

    public event EventHandler? PlaybackEnded;

    public VlcAudioEngine()
    {
        Core.Initialize();

        _libVlc = new LibVLC();
        _player = new MediaPlayer(_libVlc);
        _spectrum = new VlcSpectrumReader(_libVlc);
        if (OperatingSystem.IsWindows())
            _player.SetAudioOutput("mmdevice");

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
        _spectrum.Stop();
        _hasStarted = false;

        _media?.Dispose();
        _media = new Media(_libVlc, source.AbsoluteUri, FromType.FromLocation);

        return Task.CompletedTask;
    }

    public void Pause()
    {
        _player.SetPause(true);
        _spectrum.Pause();
    }

    public void CopySpectrum(Span<float> destination)
    {
        destination.Clear();
        if (!_disposed && IsPlaying) _spectrum.CopySpectrum(destination);
    }

    public IReadOnlyList<AudioOutputDevice> GetAudioOutputDevices()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var devices = _player.AudioOutputDeviceEnum;
        if (devices.Length == 0 && OperatingSystem.IsWindows())
            devices = _libVlc.AudioOutputDevices("mmdevice");

        return devices
            .Where(device => !string.IsNullOrWhiteSpace(device.DeviceIdentifier))
            .Select(device => new AudioOutputDevice(device.DeviceIdentifier, device.Description))
            .DistinctBy(device => device.Id)
            .ToArray();
    }

    public string? SelectAudioOutputDevice(string? deviceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _requestedOutputDeviceId = deviceId;
        return ApplyAudioOutputDevice();
    }

    private string? ApplyAudioOutputDevice()
    {
        var deviceId = _requestedOutputDeviceId;
        var missing = deviceId is not null &&
            !GetAudioOutputDevices().Any(device => string.Equals(device.Id, deviceId, StringComparison.Ordinal));
        // VLC uses an empty identifier to return to the default output.
        var effectiveDeviceId = missing ? string.Empty : deviceId ?? string.Empty;
        // Configure future Windows outputs too: the active output may not exist until playback starts.
        if (OperatingSystem.IsWindows())
            _player.SetOutputDevice(effectiveDeviceId, "mmdevice");
        _player.SetOutputDevice(effectiveDeviceId);
        return missing
            ? "A saída selecionada está indisponível. Usando a saída padrão do sistema; a preferência foi mantida."
            : null;
    }

    public void Play()
    {
        if (_media is null)
            throw new InvalidOperationException("Nenhuma faixa foi carregada.");

        ApplyAudioOutputDevice();

        if (_hasStarted)
        {
            _player.SetPause(false);
            _spectrum.Resume();
        }
        else
        {
            _player.Play(_media);
            _spectrum.Play(_media);
            _hasStarted = true;
        }
    }

    public void Seek(TimeSpan position)
    {
        _player.Time = (long)position.TotalMilliseconds;
        _spectrum.Seek((long)position.TotalMilliseconds);
    }

    public void PlayFrom(TimeSpan position)
    {
        if (_media is null) throw new InvalidOperationException("Nenhuma faixa foi carregada.");
        _media.AddOption(":start-time=" + position.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Play();
    }

    public void StopAudio()
    {
        _player.Stop();
        _spectrum.Stop();
        _hasStarted = false;
    }

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

        _spectrum.Dispose();
        _media?.Dispose();
        _player.EndReached -= OnPlaybackEnded;
        _player.Dispose();
        _libVlc.Dispose();

        _disposed = true;
    }
}