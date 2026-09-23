using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Infrastructure.Metadata;
using MusicPlayer2_Avalonia.Infrastructure.Persistence;
using MusicPlayer2_Avalonia.Infrastructure.Storage;

namespace MusicPlayer2_Avalonia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string? connectionString = null)
    {
        services.TryAddSingleton<IAppStorage>(_ => LocalAppStorage.CreateDefault());
        services.AddPersistence(connectionString);
        services.AddScoped<IMetaDataReader, MetadataReader>();
        services.AddSingleton<IAlbumArtworkReader, AlbumArtworkReader>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        await LegacySettingsMigration.ImportAsync(scope.ServiceProvider.GetRequiredService<IAppStorage>(), cancellationToken)
            .ConfigureAwait(false);
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }
}
