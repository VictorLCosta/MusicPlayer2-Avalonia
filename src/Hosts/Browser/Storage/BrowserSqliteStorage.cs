using Microsoft.Data.Sqlite;

using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Infrastructure.Persistence;

namespace MusicPlayer2_Avalonia.Browser.Storage;

/// <summary>Owns the lifetime and durable snapshots of the browser's SQLite database.</summary>
internal sealed class BrowserSqliteStorage : IDisposable
{
    private readonly SemaphoreSlim _writer = new(1, 1);
    private bool _faulted;
    private readonly IAppStorage _storage;

    public string ConnectionString { get; }
    private const string SnapshotPath = "/musicplayer-backup.db";

    private BrowserSqliteStorage(IAppStorage storage)
    {
        _storage = storage;
        ConnectionString = SqliteStorageConfiguration.GetConnectionString(storage);
    }

    public static async Task<BrowserSqliteStorage> OpenAsync(IAppStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        var snapshot = await storage.ReadBytesAsync(AppStorageFiles.DatabaseSnapshot);
        if (snapshot is not null)
            await File.WriteAllBytesAsync(storage.GetLocalPath(AppStorageFiles.Database), snapshot);
        return new BrowserSqliteStorage(storage);
    }

    public async Task<T> WriteAsync<T>(Func<Task<T>> write, CancellationToken cancellationToken = default)
    {
        await _writer.WaitAsync(cancellationToken);
        try
        {
            if (_faulted)
                throw new InvalidOperationException("A persistência falhou. Recarregue para recuperar a última versão salva.");

            var result = await write();
            try
            {
                // BackupDatabase includes committed WAL data and produces a standalone,
                // consistent database. Copying the live .db file alone would not do that.
                using (var source = new SqliteConnection(ConnectionString))
                using (var destination = new SqliteConnection($"Data Source={SnapshotPath};Pooling=False"))
                {
                    await source.OpenAsync(CancellationToken.None);
                    await destination.OpenAsync(CancellationToken.None);
                    source.BackupDatabase(destination);
                }

                // Once SQLite commits, finish persistence even if the caller cancels.
                var bytes = await File.ReadAllBytesAsync(SnapshotPath, CancellationToken.None);
                await _storage.WriteBytesAsync(AppStorageFiles.DatabaseSnapshot, bytes, CancellationToken.None);
            }
            catch
            {
                // Do not allow a later save to silently acknowledge an earlier failed save.
                _faulted = true;
                throw;
            }
            return result;
        }
        finally
        {
            _writer.Release();
        }
    }

    public void Dispose() => _writer.Dispose();
}