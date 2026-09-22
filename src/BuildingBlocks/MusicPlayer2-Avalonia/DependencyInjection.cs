using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.ViewModels;
using MusicPlayer2_Avalonia.ViewModels.Settings;
using MusicPlayer2_Avalonia.Views;

namespace MusicPlayer2_Avalonia;

public static class DependencyInjection
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        services.AddScoped<MainWindowViewModel>();
        services.AddScoped<MainViewModel>();
        services.AddScoped<PlayerViewModel>();
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<AppearanceSettingsViewModel>();
        services.AddScoped<GeneralSettingsViewModel>();
        services.AddScoped<PlaybackSettingsViewModel>();
        services.AddScoped<MediaLibrarySettingsViewModel>();

        services.AddScoped<MainView>();
        services.AddScoped<SettingsView>();

        return services;
    }
}