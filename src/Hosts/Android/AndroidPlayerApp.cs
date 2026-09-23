using Microsoft.Extensions.DependencyInjection;
using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2.Avalonia.Android.Audio;

namespace MusicPlayer2.Avalonia.Android;

public sealed class AndroidPlayerApp : MusicPlayer2_Avalonia.App
{
    protected override void ConfigurePlatformServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAudioEngine, AndroidAudioEngine>();
    }
}
