using Android.App;
using Android.Content.PM;

using Avalonia.Android;

namespace MusicPlayer2.Avalonia.Android;

[Activity(
    Label = "MusicPlayer2_Avalonia.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}