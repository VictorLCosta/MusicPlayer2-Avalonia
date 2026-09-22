using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string? connectionString = null)
    {
        if (connectionString is not null) ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<MusicPlayerDbContext>((provider, opt) =>
        {
            opt.UseSqlite(connectionString ?? SqliteStorageConfiguration.GetConnectionString(
                provider.GetRequiredService<IAppStorage>()));
        });

        services.AddScoped<IMusicPlayerDbContext>(provider => provider.GetRequiredService<MusicPlayerDbContext>());
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}