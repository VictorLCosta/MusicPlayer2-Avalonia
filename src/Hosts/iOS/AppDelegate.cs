using Avalonia;
using Avalonia.iOS;

#if HOTAVALONIA_ENABLE
using HotAvalonia;
#endif

using MusicPlayer2_Avalonia;

namespace MusicPlayer2.Avalonia.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
internal sealed partial class AppDelegate : AvaloniaAppDelegate<IosPlayerApp>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        var configuredBuilder = base.CustomizeAppBuilder(builder)
            .WithInterFont();

#if HOTAVALONIA_ENABLE
        configuredBuilder = configuredBuilder.UseHotReload();
#endif

        return configuredBuilder;
    }
}
