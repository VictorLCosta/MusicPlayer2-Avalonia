using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Application.Library;
using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Application.Playlist;
using MusicPlayer2_Avalonia.Application.Settings;

namespace MusicPlayer2_Avalonia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<LibraryService>();
        services.AddSingleton<LibraryMaintenanceService>();
        services.AddScoped<PlaylistService>();
        services.AddScoped<PlaybackQueue>();
        services.AddScoped<PlayerService>();

        services.AddSingleton<SettingsService>();
        services.AddSingleton<AudioOutputService>();
        services.AddSingleton<PlaybackSessionStore>();

        return services;
    }
}