using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Infrastructure.Storage;

internal static class LegacySettingsMigration
{
    public static async Task ImportAsync(IAppStorage storage, CancellationToken cancellationToken)
    {
        if (OperatingSystem.IsBrowser()
            || await storage.ReadBytesAsync(AppStorageFiles.Settings, cancellationToken).ConfigureAwait(false) is not null)
            return;

        var oldRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrWhiteSpace(oldRoot)) return;

        var oldPath = Path.Combine(oldRoot, "MusicPlayer2", AppStorageFiles.Settings);
        byte[] bytes;

        try
        {
            bytes = await File.ReadAllBytesAsync(oldPath, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        catch (DirectoryNotFoundException)
        {
            return;
        }

        await storage.WriteBytesAsync(AppStorageFiles.Settings, bytes, cancellationToken).ConfigureAwait(false);
    }
}