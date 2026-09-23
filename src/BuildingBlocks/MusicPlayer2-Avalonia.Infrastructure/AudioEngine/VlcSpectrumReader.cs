using System.Runtime.InteropServices;

using LibVLCSharp.Shared;

namespace MusicPlayer2_Avalonia.Infrastructure.AudioEngine;

// A separate silent decoder keeps the normal device output owned by VLC.
internal sealed class VlcSpectrumReader : IDisposable
{
    private readonly MediaPlayer _player;
    private readonly LibVLC _vlc;
    private readonly SpectrumAnalyzer _analyzer = new();
    private readonly object _gate = new();
    private readonly Queue<(long Timestamp, float[] Bands)> _frames = new();
    private readonly float[] _window = new float[SpectrumAnalyzer.Size];
    private short[] _scratch = new short[4096];
    private float[] _latest = new float[SpectrumAnalyzer.BandCount];
    private int _filled;
    private long _lastTimestamp;

    public VlcSpectrumReader(LibVLC vlc)
    {
        _vlc = vlc;
        _player = new MediaPlayer(vlc);
        _player.SetAudioCallbacks(OnAudio, (_, _) => Clear(), (_, _) => Clear(), (_, _) => Clear(), _ => { });
        // VLC 3's amem output supports signed 16-bit native-endian PCM.
        _player.SetAudioFormat("S16N", SpectrumAnalyzer.SampleRate, 1);
    }

    public void Play(Media media) => _player.Play(media);
    public void ApplyEqualizer(bool enabled, Equalizer equalizer)
    {
        if (!(enabled ? _player.SetEqualizer(equalizer) : _player.UnsetEqualizer()))
            throw new InvalidOperationException("Could not apply spectrum equalizer.");
        Clear();
    }
    public void Pause() => _player.SetPause(true);
    public void Resume() => _player.SetPause(false);
    public void Seek(long milliseconds) { _player.Time = milliseconds; Clear(); }
    public void Stop() { _player.Stop(); Clear(); }

    private void Clear()
    {
        lock (_gate)
        {
            _frames.Clear();
            Array.Clear(_latest);
            _filled = 0;
            _lastTimestamp = 0;
        }
    }

    private void OnAudio(IntPtr data, IntPtr samples, uint count, long pts)
    {
        // Never propagate managed exceptions across a native callback boundary.
        try
        {
            lock (_gate)
            {
                int length = checked((int)count);
                if (_scratch.Length < length) _scratch = new short[length];
                Marshal.Copy(samples, _scratch, 0, length);
                for (int i = 0; i < length; i++)
                {
                    _window[_filled++] = _scratch[i] / 32768f;
                    if (_filled != _window.Length) continue;
                    long timestamp = pts + (long)i * 1_000_000 / SpectrumAnalyzer.SampleRate;
                    _frames.Enqueue((timestamp, _analyzer.Analyze(_window)));
                    // Bound memory even when the visual is not attached.
                    while (_frames.Count > 120) _frames.Dequeue();
                    _filled = 0;
                }
            }
        }
        catch (Exception) { Clear(); }
    }

    public void CopySpectrum(Span<float> destination)
    {
        lock (_gate)
        {
            long now = _vlc.Clock;
            while (_frames.TryPeek(out var frame) && frame.Timestamp <= now)
            {
                _frames.Dequeue();
                _latest = frame.Bands;
                _lastTimestamp = frame.Timestamp;
            }
            destination.Clear();
            if (now - _lastTimestamp < 250_000)
                _latest.AsSpan(0, Math.Min(destination.Length, _latest.Length)).CopyTo(destination);
        }
    }

    public void Dispose() => _player.Dispose();
}
