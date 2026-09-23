using AVFoundation;
using Foundation;
using MediaPlayer;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Player;
using UIKit;
using App = MusicPlayer2_Avalonia.App;

namespace MusicPlayer2.Avalonia.iOS.Audio;

internal sealed class IosAudioEngine : IAudioEngine
{
    private readonly object _gate = new();
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213", Justification = "Shared AVAudioSession is owned by the OS; this engine only activates/deactivates it.")]
    private readonly AVAudioSession _session = AVAudioSession.SharedInstance();
    private readonly List<(MPRemoteCommand Command, NSObject Token)> _commands = [];
    private readonly NSObject _interruptions;
    private readonly NSObject _routes;
    private readonly NSObject _background;
    private readonly Timer _timer;
    private AVAudioPlayer? _player;
    private double _volume = 1;
    private bool _disposed;
    private bool _resumeAfterInterruption;
    private bool _saving;
    public event EventHandler? PlaybackEnded;

    public IosAudioEngine()
    {
        _session.SetCategory(AVAudioSessionCategory.Playback, (AVAudioSessionCategoryOptions)0, out var error);
        using var categoryError = error;
        if (error is not null) throw new InvalidOperationException(error.LocalizedDescription);
        _interruptions = AVAudioSession.Notifications.ObserveInterruption((_, notification) =>
        {
            var type = notification.InterruptionType;
            if (type == AVAudioSessionInterruptionType.Began)
            {
                var resume = IsPlaying;
                Pause();
                _resumeAfterInterruption = resume;
            }
            else if (_resumeAfterInterruption)
            {
                _resumeAfterInterruption = false;
                var options = notification.Option;
                if ((options & AVAudioSessionInterruptionOptions.ShouldResume) != 0)
                    try { Play(); } catch (Exception) { /* Wait for an explicit user retry. */ }
            }
        });
        _routes = AVAudioSession.Notifications.ObserveRouteChange((_, notification) =>
        {
            var reason = notification.Reason;
            if (reason == AVAudioSessionRouteChangeReason.OldDeviceUnavailable) Pause();
        });
        _background = UIApplication.Notifications.ObserveDidEnterBackground((_, _) => SaveInBackground());
        var center = MPRemoteCommandCenter.Shared;
        Register(center.PlayCommand, () => { CurrentPlayer()?.Resume(); return Task.CompletedTask; });
        Register(center.PauseCommand, () => { CurrentPlayer()?.Pause(); return Task.CompletedTask; });
        Register(center.NextTrackCommand, () => CurrentPlayer()?.NextAsync() ?? Task.CompletedTask);
        Register(center.PreviousTrackCommand, () => CurrentPlayer()?.PreviousAsync() ?? Task.CompletedTask);
        Register(center.StopCommand, async () =>
        {
            if (CurrentPlayer() is { } player) { await player.SaveSessionAsync(); player.Stop(); }
        });
        center.ChangePlaybackPositionCommand.Enabled = true;
        _commands.Add((center.ChangePlaybackPositionCommand, center.ChangePlaybackPositionCommand.AddTarget(e =>
        {
            if (e is not MPChangePlaybackPositionCommandEvent position) return MPRemoteCommandHandlerStatus.CommandFailed;
            Seek(TimeSpan.FromSeconds(position.PositionTime));
            return MPRemoteCommandHandlerStatus.Success;
        })));
        _timer = new Timer(_ => global::Avalonia.Threading.Dispatcher.UIThread.Post(Checkpoint), null,
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    private static async void SaveInBackground()
    {
        var application = UIApplication.SharedApplication;
        var identifier = UIApplication.BackgroundTaskInvalid;
        identifier = application.BeginBackgroundTask("Save playback", () =>
        {
            if (identifier == UIApplication.BackgroundTaskInvalid) return;
            application.EndBackgroundTask(identifier);
            identifier = UIApplication.BackgroundTaskInvalid;
        });
        try
        {
            if (global::Avalonia.Application.Current is App app) await app.SavePlaybackAsync();
        }
        finally
        {
            if (identifier != UIApplication.BackgroundTaskInvalid) application.EndBackgroundTask(identifier);
        }
    }
    private static PlayerService? CurrentPlayer() => (global::Avalonia.Application.Current as App)?.Services.GetRequiredService<PlayerService>();
    private void Register(MPRemoteCommand command, Func<Task> action)
    {
        command.Enabled = true;
        _commands.Add((command, command.AddTarget(commandEvent =>
        {
            if (CurrentPlayer()?.CurrentTrack is null) return MPRemoteCommandHandlerStatus.NoSuchContent;
            _ = ExecuteAsync(action);
            return MPRemoteCommandHandlerStatus.Success;
        })));
    }
    private async Task ExecuteAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception) { Pause(); }
    }
    private async void Checkpoint()
    {
        if (_disposed) return;
        UpdateNowPlaying();
        if (_saving || CurrentPlayer() is not { } player) return;
        _saving = true;
        try { await player.SaveSessionAsync(); }
        catch (Exception) { /* Retry at the next checkpoint. */ }
        finally { _saving = false; }
    }
    public bool IsPlaying { get { lock (_gate) return _player?.Playing == true; } }
    public TimeSpan Position { get { lock (_gate) return TimeSpan.FromSeconds(_player?.CurrentTime ?? 0); } }
    public TimeSpan Duration { get { lock (_gate) return TimeSpan.FromSeconds(_player?.Duration ?? 0); } }
    public double Volume
    {
        get => _volume;
        set { lock (_gate) { _volume = Math.Clamp(value, 0, 1); if (_player is not null) _player.Volume = (float)_volume; } }
    }
    public async Task LoadAsync(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsFile) throw new NotSupportedException("Importe a música para o aplicativo antes de reproduzir.");
        await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                ReleasePlayer();
                using var url = NSUrl.FromFilename(source.LocalPath);
                var preparedPlayer = AVAudioPlayer.FromUrl(url, out var error);
                using (error)
                    _player = preparedPlayer ?? throw new InvalidOperationException(error?.LocalizedDescription ?? "Formato de áudio não suportado neste dispositivo.");
                _player.Volume = (float)_volume;
                _player.FinishedPlaying += Finished;
                if (!_player.PrepareToPlay()) { ReleasePlayer(); throw new InvalidOperationException("Não foi possível preparar o áudio."); }
            }
        });
    }
    public void Play()
    {
        lock (_gate)
        {
            if (_player is null) throw new InvalidOperationException("Nenhuma faixa foi carregada.");
            _session.SetActive(true, out var error);
            using var activationError = error;
            if (error is not null) throw new InvalidOperationException(error.LocalizedDescription);
            if (!_player.Play()) throw new InvalidOperationException("Não foi possível iniciar a reprodução.");
            _resumeAfterInterruption = false;
        }
        NotifyNowPlaying();
    }
    public void Pause()
    {
        lock (_gate) { _player?.Pause(); _resumeAfterInterruption = false; }
        DeactivateSession();
        NotifyNowPlaying();
    }
    public void Seek(TimeSpan position)
    {
        lock (_gate) if (_player is not null) _player.CurrentTime = Math.Clamp(position.TotalSeconds, 0, _player.Duration);
        NotifyNowPlaying();
    }
    public void StopAudio()
    {
        lock (_gate) { _resumeAfterInterruption = false; _player?.Stop(); if (_player is not null) _player.CurrentTime = 0; }
        DeactivateSession();
        NotifyNowPlaying();
    }
    private void DeactivateSession()
    {
        _session.SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out var error);
        error?.Dispose();
    }
    private void Finished(object? sender, AVStatusEventArgs e)
    {
        NotifyNowPlaying();
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
    private void NotifyNowPlaying() => global::Avalonia.Threading.Dispatcher.UIThread.Post(UpdateNowPlaying);
    private void UpdateNowPlaying()
    {
        if (_disposed) return;
        var track = CurrentPlayer()?.CurrentTrack;
        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = track is null ? new MPNowPlayingInfo() : new MPNowPlayingInfo
        {
            Title = track.Title, Artist = track.Artist?.Name, AlbumTitle = track.Album?.Title,
            PlaybackDuration = Duration.TotalSeconds, ElapsedPlaybackTime = Position.TotalSeconds,
            PlaybackRate = IsPlaying ? 1 : 0
        };
    }
    private void ReleasePlayer()
    {
        if (_player is null) return;
        _player.FinishedPlaying -= Finished;
        _player.Stop();
        _player.Dispose();
        _player = null;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Dispose();
        _interruptions.Dispose();
        _routes.Dispose();
        _background.Dispose();
        foreach (var (command, token) in _commands) { command.RemoveTarget(token); token.Dispose(); }
        lock (_gate) ReleasePlayer();
        DeactivateSession();
        MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = new MPNowPlayingInfo();
    }
}





