using System.Diagnostics.CodeAnalysis;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Player;
using App = MusicPlayer2_Avalonia.App;

namespace MusicPlayer2.Avalonia.Android.Audio;

[SuppressMessage("Usage", "CA2213", Justification = "Android calls OnDestroy for service resources; engine and player belong to application DI.")]
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public sealed class PlaybackService : Service
{
    private const string Channel = "music-playback";
    private const int NotificationId = 41;
    private AndroidAudioEngine? _engine;
    private PlayerService? _player;
    private MediaSession? _session;
    private SessionCallbacks? _callbacks;
    private NoisyReceiver? _noisy;
    private Timer? _timer;
    private bool _saving;
    private volatile bool _destroyed;

    public override void OnCreate()
    {
        base.OnCreate();
        if (global::Avalonia.Application.Current is not App app) { StopSelf(); return; }
        _engine = app.Services.GetRequiredService<IAudioEngine>() as AndroidAudioEngine;
        _player = app.Services.GetRequiredService<PlayerService>();
        _session = new MediaSession(this, "MusicPlayer2");
        _callbacks = new SessionCallbacks(this);
        _session.SetCallback(_callbacks);
        _session.Active = true;
        if (_engine is not null) _engine.StateChanged += StateChanged;
        _noisy = new NoisyReceiver(() => _engine?.Pause());
        using var filter = new IntentFilter(AudioManager.ActionAudioBecomingNoisy);
        if (OperatingSystem.IsAndroidVersionAtLeast(33)) RegisterReceiver(_noisy, filter, ReceiverFlags.NotExported);
        else RegisterReceiver(_noisy, filter);
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var manager = (NotificationManager)GetSystemService(NotificationService)!;
            using var channel = new NotificationChannel(Channel, "Reprodução de música", NotificationImportance.Low);
            manager.CreateNotificationChannel(channel);
        }
        _timer = new Timer(_ => global::Avalonia.Threading.Dispatcher.UIThread.Post(UpdateAndSave), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    public override IBinder? OnBind(Intent? intent) => null;
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        UpdateNotification();
        if (intent?.Action is { } action) _ = HandleAsync(action);
        return StartCommandResult.NotSticky;
    }
    private void StateChanged(object? sender, EventArgs e) => global::Avalonia.Threading.Dispatcher.UIThread.Post(UpdateNotification);
    private async void UpdateAndSave()
    {
        UpdateNotification();
        if (_destroyed || _saving || _player is null) return;
        _saving = true;
        try { await _player.SaveSessionAsync(); }
        catch (Exception) { /* Retry at the next checkpoint. */ }
        finally { _saving = false; }
    }
    private async Task HandleAsync(string action)
    {
        try
        {
            if (_player is null) return;
            switch (action)
            {
                case "play": _player.Resume(); break;
                case "pause": _player.Pause(); break;
                case "next": await _player.NextAsync(); break;
                case "previous": await _player.PreviousAsync(); break;
                case "stop": await _player.SaveSessionAsync(); _player.Stop(); break;
            }
        }
        catch (Exception) { _engine?.Pause(); }
        UpdateNotification();
    }
    private PendingIntent ActionIntent(string action)
    {
        using var intent = new Intent(this, typeof(PlaybackService));
        intent.SetAction(action);
        return PendingIntent.GetService(this, action.GetHashCode(StringComparison.Ordinal), intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
    }
    [SuppressMessage("Interoperability", "CA1422", Justification = "Legacy notification action overload is supported on the full Android 24+ target range.")]
    private void UpdateNotification()
    {
        if (_destroyed || _engine is null || _session is null) return;
        var playing = _engine.IsPlaying;
        var title = _player?.CurrentTrack?.Title ?? "MusicPlayer2";
        using var stateBuilder = new PlaybackState.Builder();
        using var state = stateBuilder
            .SetActions(PlaybackState.ActionPlay | PlaybackState.ActionPause | PlaybackState.ActionSkipToNext |
                PlaybackState.ActionSkipToPrevious | PlaybackState.ActionSeekTo | PlaybackState.ActionStop)!
            .SetState(playing ? PlaybackStateCode.Playing : PlaybackStateCode.Paused,
                (long)_engine.Position.TotalMilliseconds, playing ? 1 : 0)!.Build();
        _session.SetPlaybackState(state);
        using var metadataBuilder = new MediaMetadata.Builder();
        using var metadata = metadataBuilder.PutString(MediaMetadata.MetadataKeyTitle, title)!
            .PutString(MediaMetadata.MetadataKeyArtist, _player?.CurrentTrack?.Artist?.Name ?? "")!
            .PutLong(MediaMetadata.MetadataKeyDuration, (long)_engine.Duration.TotalMilliseconds)!.Build();
        _session.SetMetadata(metadata);
        using var openIntent = new Intent(this, typeof(MainActivity));
        using var open = PendingIntent.GetActivity(this, 0, openIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        using var builder = OperatingSystem.IsAndroidVersionAtLeast(26) ? new Notification.Builder(this, Channel) : new Notification.Builder(this);
        using var styleBuilder = new Notification.MediaStyle();
        var style = styleBuilder.SetMediaSession(_session.SessionToken)!.SetShowActionsInCompactView(0, 1, 2);
        using var previous = ActionIntent("previous");
        using var toggle = ActionIntent(playing ? "pause" : "play");
        using var next = ActionIntent("next");
        using var notification = builder.SetContentTitle(title)!.SetContentText(_player?.CurrentTrack?.Artist?.Name ?? "MusicPlayer2")!
            .SetSmallIcon(global::Android.Resource.Drawable.IcMediaPlay)!.SetContentIntent(open)!.SetOngoing(playing)!
            .SetOnlyAlertOnce(true)!.SetStyle(style)!
            .AddAction(global::Android.Resource.Drawable.IcMediaPrevious, "Anterior", previous)!
            .AddAction(playing ? global::Android.Resource.Drawable.IcMediaPause : global::Android.Resource.Drawable.IcMediaPlay,
                playing ? "Pausar" : "Reproduzir", toggle)!
            .AddAction(global::Android.Resource.Drawable.IcMediaNext, "Próxima", next)!.Build();
        StartForeground(NotificationId, notification);
    }
    public override void OnDestroy()
    {
        _destroyed = true;
        _timer?.Dispose();

        if (_engine is not null)
        {
            _engine.StateChanged -= StateChanged;
            _engine.Pause();
        }
        if (_noisy is not null) { UnregisterReceiver(_noisy); _noisy.Dispose(); }
        _session?.Release();
        _session?.Dispose();
        _session = null;
        _callbacks?.Dispose();
        if (global::Avalonia.Application.Current is App app) _ = app.SavePlaybackAsync();

        base.OnDestroy();
    }
    private sealed class SessionCallbacks(PlaybackService service) : MediaSession.Callback
    {
        public override void OnPlay() => _ = service.HandleAsync("play");
        public override void OnPause() => _ = service.HandleAsync("pause");
        public override void OnStop() => _ = service.HandleAsync("stop");
        public override void OnSkipToNext() => _ = service.HandleAsync("next");
        public override void OnSkipToPrevious() => _ = service.HandleAsync("previous");
        public override void OnSeekTo(long pos) => service._engine?.Seek(TimeSpan.FromMilliseconds(pos));
    }
    private sealed class NoisyReceiver(Action pause) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent) => pause();
    }
}



