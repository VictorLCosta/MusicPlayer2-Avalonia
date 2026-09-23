using Android.Runtime;

using Avalonia;
using Avalonia.Android;

#if HOTAVALONIA_ENABLE
using HotAvalonia;
#endif

using MusicPlayer2_Avalonia;

namespace MusicPlayer2.Avalonia.Android;

[Application]
public sealed class AndroidApp(IntPtr javaReference, JniHandleOwnership transfer)
    : AvaloniaAndroidApplication<AndroidPlayerApp>(javaReference, transfer)
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        var configuredBuilder = base.CustomizeAppBuilder(builder);

#if HOTAVALONIA_ENABLE
        configuredBuilder = configuredBuilder.UseHotReload();
#endif

        return configuredBuilder;
    }
}
