using Android.App;
using Android.Content.PM;

using Avalonia.Android;

namespace MusicPlayer2.Avalonia.Android;

[Activity(
    Label = "MusicPlayer2",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    private static WeakReference<MainActivity>? _current;
    private bool _spectrumPermissionRequested;

    internal static void RequestSpectrumPermission()
    {
        if (_current is null || !_current.TryGetTarget(out var activity) || activity._spectrumPermissionRequested ||
            activity.CheckSelfPermission(global::Android.Manifest.Permission.RecordAudio) == Permission.Granted) return;
        activity._spectrumPermissionRequested = true;
        activity.RequestPermissions([global::Android.Manifest.Permission.RecordAudio], 410);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213", Justification = "Android activity callback is unregistered and disposed in OnDestroy.")]
    private BackCallback? _back;

    protected override void OnCreate(global::Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _current = new WeakReference<MainActivity>(this);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            _back = new BackCallback(this);
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, _back);
        }
    }
    protected override void OnDestroy()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && _back is not null)
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(_back);
        _back?.Dispose();
        _back = null;
        base.OnDestroy();
    }
    [System.Runtime.Versioning.SupportedOSPlatform("android33.0")]
    private sealed class BackCallback(MainActivity activity) : Java.Lang.Object, global::Android.Window.IOnBackInvokedCallback
    {
        public void OnBackInvoked()
        {
            if (global::Avalonia.Application.Current is MusicPlayer2_Avalonia.App app && app.NavigateBack()) return;
            activity.MoveTaskToBack(true);
        }
    }
    protected override void OnPause()
    {
        if (global::Avalonia.Application.Current is MusicPlayer2_Avalonia.App app) _ = app.SavePlaybackAsync();
        base.OnPause();
    }

    public override void OnBackPressed()
    {
        if (global::Avalonia.Application.Current is MusicPlayer2_Avalonia.App app && app.NavigateBack()) return;
        if (!OperatingSystem.IsAndroidVersionAtLeast(33)) base.OnBackPressed();
        else MoveTaskToBack(true);
    }
}
