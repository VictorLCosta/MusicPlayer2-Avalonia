using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using MusicPlayer2_Avalonia.Infrastructure.Storage;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence;

public class MusicPlayerDbContextFactory : IDesignTimeDbContextFactory<MusicPlayerDbContext>
{
    public MusicPlayerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MusicPlayerDbContext>();
        using var storage = LocalAppStorage.CreateDefault();
        var connectionString = SqliteStorageConfiguration.GetConnectionString(storage);

        optionsBuilder.UseSqlite(connectionString);

        return new MusicPlayerDbContext(optionsBuilder.Options);
    }
}