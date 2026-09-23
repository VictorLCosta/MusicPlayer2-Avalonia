using Microsoft.Extensions.DependencyInjection;
using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2.Avalonia.iOS.Audio;
namespace MusicPlayer2.Avalonia.iOS;
internal sealed class IosPlayerApp : MusicPlayer2_Avalonia.App
{
    protected override void ConfigurePlatformServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAudioEngine, IosAudioEngine>();
    }
}

