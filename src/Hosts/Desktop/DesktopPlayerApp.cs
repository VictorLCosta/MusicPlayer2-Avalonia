using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Infrastructure.AudioEngine;

namespace MusicPlayer2.Avalonia.Desktop;

internal sealed class DesktopPlayerApp : MusicPlayer2_Avalonia.App
{
    protected override void ConfigurePlatformServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAudioEngine, VlcAudioEngine>();
    }
}
