using Microsoft.EntityFrameworkCore;

using MusicPlayer2_Avalonia.Infrastructure.Persistence;

namespace MusicPlayer2_Avalonia.Browser.Storage;

internal sealed class BrowserMusicPlayerDbContext(
    DbContextOptions<BrowserMusicPlayerDbContext> options,
    BrowserSqliteStorage storage) : MusicPlayerDbContext(options)
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("No Browser, use SaveChangesAsync para aguardar a persistência.");

    public override int SaveChanges() => SaveChanges(true);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
            throw new NotSupportedException("Use uma única chamada SaveChangesAsync para a operação atômica no Browser.");

        return storage.WriteAsync(async () =>
        {
            // EF tracking is updated when the local SQL transaction commits. If the
            // durable snapshot fails, storage is faulted and subsequent writes fail.
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }, cancellationToken);
    }
}