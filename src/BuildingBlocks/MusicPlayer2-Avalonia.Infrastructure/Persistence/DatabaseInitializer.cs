namespace MusicPlayer2_Avalonia.Infrastructure.Persistence;

public sealed class DatabaseInitializer(MusicPlayerDbContext context)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }
}