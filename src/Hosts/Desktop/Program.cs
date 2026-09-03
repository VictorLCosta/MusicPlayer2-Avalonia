using System;
using System.IO;

using Avalonia;
using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Infrastructure;
using MusicPlayer2_Avalonia.Infrastructure.Persistence;

namespace MusicPlayer2.Avalonia.Desktop;

sealed class Program
{
    public static void Main(string[] args)
    {
        using ServiceProvider services = new ServiceCollection()
            .AddInfrastructure(CreateConnectionString())
            .BuildServiceProvider();

        services.InitializeDatabaseAsync().GetAwaiter().GetResult();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static string CreateConnectionString()
    {
        string applicationDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MusicPlayer2-Avalonia");

        Directory.CreateDirectory(applicationDataDirectory);

        return $"Data Source={Path.Combine(applicationDataDirectory, "musicplayer.db")}";
    }
}
