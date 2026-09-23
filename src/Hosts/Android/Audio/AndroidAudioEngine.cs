using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Media;
using Avalonia.Threading;
using MusicPlayer2_Avalonia.Application.Common;
using NativePlayer = Android.Media.MediaPlayer;

namespace MusicPlayer2.Avalonia.Android.Audio;

// MediaPlayer and its callbacks are confined to the UI looper. Preparation remains asynchronous.
internal sealed class AndroidAudioEngine : Java.Lang.Object, IAudioEngine, IAudioSpectrumSource, AudioManager.IOnAudioFocusChangeListener
{
    [SuppressMessage("Usage", "CA2213", Justification = "Released by ReleaseSpectrum via StopAudio on the UI looper during Dispose.")]
    private global::Android.Media.Audiofx.Visualizer? _visualizer;
    private byte[] _fft = [];
    private bool _spectrumUnavailable;

    // Called by the UI spectrum timer, on the same looper as MediaPlayer.
    public void CopySpectrum(Span<float> destination)
    {
        destination.Clear();
        if (!Dispatcher.UIThread.CheckAccess() || _disposed || !_loaded || !_player.IsPlaying ||
            _spectrumUnavailable || _context.CheckSelfPermission(global::Android.Manifest.Permission.RecordAudio) !=
            global::Android.Content.PM.Permission.Granted) return;
        try
        {
            if (_visualizer is null)
            {
                _visualizer = new global::Android.Media.Audiofx.Visualizer(_player.AudioSessionId);
                var sizes = global::Android.Media.Audiofx.Visualizer.GetCaptureSizeRange()!;
                if (_visualizer.SetCaptureSize(sizes[1]) != (int)global::Android.Media.Audiofx.VisualizerStatus.Success)
                    throw new InvalidOperationException("Audio visualizer capture size is unavailable.");
                _fft = new byte[sizes[1]];
                if (_visualizer.SetEnabled(true) != global::Android.Media.Audiofx.VisualizerStatus.Success)
                    throw new InvalidOperationException("Audio visualizer could not be enabled.");
            }
            if (_visualizer.GetFft(_fft) != global::Android.Media.Audiofx.VisualizerStatus.Success) return;
            var bins = _fft.Length / 2;
            for (var bar = 0; bar < destination.Length; bar++)
            {
                var start = Math.Clamp((int)Math.Pow(bins, (double)bar / destination.Length), 1, bins - 1);
                var end = Math.Clamp((int)Math.Pow(bins, (double)(bar + 1) / destination.Length), start + 1, bins);
                double magnitude = 0;
                for (var bin = start; bin < end; bin++)
                {
                    var real = (sbyte)_fft[bin * 2];
                    var imaginary = (sbyte)_fft[bin * 2 + 1];
                    magnitude = Math.Max(magnitude, Math.Sqrt(real * real + imaginary * imaginary));
                }
                destination[bar] = (float)Math.Clamp(Math.Log10(1 + magnitude) / Math.Log10(182), 0, 1);
            }
        }
        catch (Exception ex) when (ex is Java.Lang.RuntimeException or InvalidOperationException)
        {
            ReleaseSpectrum();
            _spectrumUnavailable = true;
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void ReleaseSpectrum()
    {
        _visualizer?.Release();
        _visualizer?.Dispose();
        _visualizer = null;
        _fft = [];
    }

    private readonly NativePlayer _player = new();
    [SuppressMessage("Usage", "CA2213", Justification = "Application context is owned by Android.")]
    private readonly Context _context = global::Android.App.Application.Context;
    [SuppressMessage("Usage", "CA2213", Justification = "System service is owned by Android.")]
    private readonly AudioManager _audio;
    private readonly AudioFocusRequestClass? _focus;
    private TaskCompletionSource? _preparation;
    private bool _loaded;
    private bool _disposed;
    private bool _resumeAfterFocus;
    private double _volume = 1;
    public event EventHandler? PlaybackEnded;
    public event EventHandler? StateChanged;

    public AndroidAudioEngine()
    {
        _audio = (AudioManager)_context.GetSystemService(Context.AudioService)!;
        using var attributesBuilder = new AudioAttributes.Builder();
        using var attributes = attributesBuilder.SetUsage(AudioUsageKind.Media)!
            .SetContentType(AudioContentType.Music)!.Build()!;
        _player.SetAudioAttributes(attributes);
        _player.SetWakeMode(_context, global::Android.OS.WakeLockFlags.Partial);
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            using var focusBuilder = new AudioFocusRequestClass.Builder(AudioFocus.Gain);
            _focus = focusBuilder.SetAudioAttributes(attributes)!.SetOnAudioFocusChangeListener(this)!.Build();
        }
        _player.Prepared += Prepared;
        _player.Completion += Completed;
        _player.Error += PlaybackError;
    }
    private static T OnMain<T>(Func<T> action) => Dispatcher.UIThread.CheckAccess()
        ? action() : Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    private static void OnMain(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.InvokeAsync(action).GetAwaiter().GetResult();
    }
    public bool IsPlaying => OnMain(() => !_disposed && _loaded && _player.IsPlaying);
    public TimeSpan Position => OnMain(() => TimeSpan.FromMilliseconds(_loaded ? _player.CurrentPosition : 0));
    public TimeSpan Duration => OnMain(() => TimeSpan.FromMilliseconds(_loaded ? _player.Duration : 0));
    public double Volume
    {
        get => OnMain(() => _volume);
        set => OnMain(() => { _volume = Math.Clamp(value, 0, 1); _player.SetVolume((float)_volume, (float)_volume); });
    }
    public async Task LoadAsync(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsFile) throw new NotSupportedException("Importe a música para o aplicativo antes de reproduzir.");
        Task preparation = Task.CompletedTask;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _preparation?.TrySetCanceled();
            _preparation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            preparation = _preparation.Task;
            _loaded = false;
            ReleaseSpectrum();
            _spectrumUnavailable = false;
            _player.Reset();
            using var builder = new AudioAttributes.Builder();
            using var attributes = builder.SetUsage(AudioUsageKind.Media)!.SetContentType(AudioContentType.Music)!.Build();
            _player.SetAudioAttributes(attributes);
            _player.SetDataSource(source.LocalPath);
            _player.PrepareAsync();
        });
        await preparation.ConfigureAwait(false);
    }
    private void Prepared(object? sender, EventArgs e)
    {
        _loaded = true;
        _player.SetVolume((float)_volume, (float)_volume);
        _preparation?.TrySetResult();
        _preparation = null;
    }
    public void Play() => OnMain(() =>
    {
        if (!_loaded) throw new InvalidOperationException("Nenhuma faixa foi carregada.");
        var result = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? _audio.RequestAudioFocus(_focus!)
            : _audio.RequestAudioFocus(this, global::Android.Media.Stream.Music, AudioFocus.Gain);
        if (result != AudioFocusRequest.Granted) throw new InvalidOperationException("O áudio está sendo usado por outro aplicativo.");
        try
        {
            using var intent = new Intent(_context, typeof(PlaybackService));
            if (OperatingSystem.IsAndroidVersionAtLeast(26)) _context.StartForegroundService(intent);
            else _context.StartService(intent);
            _player.Start();
            MainActivity.RequestSpectrumPermission();
            _resumeAfterFocus = false;
        }
        catch { ReleaseFocus(); throw; }
        StateChanged?.Invoke(this, EventArgs.Empty);
    });
    public void Pause() => OnMain(() =>
    {
        if (_loaded && _player.IsPlaying) _player.Pause();
        _resumeAfterFocus = false;
        ReleaseFocus();
        StateChanged?.Invoke(this, EventArgs.Empty);
    });
    public void Seek(TimeSpan position) => OnMain(() =>
    {
        if (_loaded) _player.SeekTo((int)Math.Clamp(position.TotalMilliseconds, 0, _player.Duration));
        StateChanged?.Invoke(this, EventArgs.Empty);
    });
    public void StopAudio() => OnMain(() =>
    {
        _preparation?.TrySetCanceled();
        _preparation = null;
        ReleaseSpectrum();
        _player.Reset();
        _loaded = false;
        _resumeAfterFocus = false;
        ReleaseFocus();
        StateChanged?.Invoke(this, EventArgs.Empty);
        using var intent = new Intent(_context, typeof(PlaybackService));
        _context.StopService(intent);
    });
    public void OnAudioFocusChange(AudioFocus focusChange)
    {
        if (focusChange == AudioFocus.Gain)
        {
            if (_resumeAfterFocus)
                try { Play(); } catch (Exception) { Pause(); }
            return;
        }
        if (focusChange is AudioFocus.LossTransient or AudioFocus.LossTransientCanDuck)
        {
            _resumeAfterFocus = _loaded && _player.IsPlaying;
            if (_resumeAfterFocus) _player.Pause();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        else Pause();
    }
    private void ReleaseFocus()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) _audio.AbandonAudioFocusRequest(_focus!);
        else _audio.AbandonAudioFocus(this);
    }
    private void Completed(object? sender, EventArgs e)
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
    private void PlaybackError(object? sender, NativePlayer.ErrorEventArgs e)
    {
        e.Handled = true;
        _loaded = false;
        _preparation?.TrySetException(new InvalidOperationException("O dispositivo não conseguiu decodificar esta música."));
        _preparation = null;
        ReleaseFocus();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            OnMain(() =>
            {
                StopAudio();
                _disposed = true;
                _player.Prepared -= Prepared;
                _player.Completion -= Completed;
                _player.Error -= PlaybackError;
                _player.Release();
                _player.Dispose();
                _focus?.Dispose();
            });
        }
        base.Dispose(disposing);
    }
}
