using System.Text;

namespace MusicPlayer2_Avalonia.Application.Common.Storage;

public static class AppStorageExtensions
{
    public static async Task<string?> ReadTextAsync(this IAppStorage storage, string relativePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);

        var bytes = await storage.ReadBytesAsync(relativePath, cancellationToken).ConfigureAwait(false);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    public static Task WriteTextAsync(this IAppStorage storage, string relativePath, string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(content);

        return storage.WriteBytesAsync(relativePath, Encoding.UTF8.GetBytes(content), cancellationToken);
    }
}