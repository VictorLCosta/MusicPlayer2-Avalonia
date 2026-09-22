using Microsoft.Data.Sqlite;

using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence;

public static class SqliteStorageConfiguration
{
    public static string GetConnectionString(IAppStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        return new SqliteConnectionStringBuilder
        {
            DataSource = storage.GetLocalPath(AppStorageFiles.Database),
            Pooling = !OperatingSystem.IsBrowser(),
        }.ToString();
    }
}