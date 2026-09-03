using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Application.Common;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<MusicPlayerDbContext>(opt =>
        {
            opt.UseSqlite(connectionString);
        });
        
        services.AddScoped<IMusicPlayerDbContext>(provider => provider.GetRequiredService<MusicPlayerDbContext>());
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
