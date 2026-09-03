using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Infrastructure.AudioEngine;
using MusicPlayer2_Avalonia.Infrastructure.Metadata;
using MusicPlayer2_Avalonia.Infrastructure.Persistence;

namespace MusicPlayer2_Avalonia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
    {
        services.AddPersistence(connectionString);
        services.AddScoped<IMetaDataReader, MetadataReader>();
        services.AddSingleton<IAudioEngine, VlcAudioEngine>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
