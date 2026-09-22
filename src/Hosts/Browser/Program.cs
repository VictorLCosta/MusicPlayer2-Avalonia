using Avalonia;
using Avalonia.Browser;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia;
using MusicPlayer2_Avalonia.Application;
using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Browser.Audio;
using MusicPlayer2_Avalonia.Browser.Storage;
using MusicPlayer2_Avalonia.Infrastructure.Metadata;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        using var appStorage = await BrowserAppStorage.OpenAsync();
        using var storage = await BrowserSqliteStorage.OpenAsync(appStorage);

        var collection = new ServiceCollection();

        collection.AddUIServices();
        collection.AddApplicationServices();
        collection.AddSingleton(storage);
        collection.AddSingleton<IAppStorage>(appStorage);
        collection.AddSingleton<IAudioEngine, BrowserAudioEngine>();
        collection.AddScoped<IMetaDataReader, MetadataReader>();
        collection.AddSingleton<IIndexedDbService, IndexedDbService>();
        collection.AddDbContext<BrowserMusicPlayerDbContext>(options =>
            options.UseSqlite(storage.ConnectionString));
        collection.AddScoped<IMusicPlayerDbContext>(services =>
            services.GetRequiredService<BrowserMusicPlayerDbContext>());

        var services = collection.BuildServiceProvider();

        await using (var scope = services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BrowserMusicPlayerDbContext>();
            await storage.WriteAsync(() => context.Database.EnsureCreatedAsync());
        }

        await AppBuilder.Configure(() => new App(services))
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }
}