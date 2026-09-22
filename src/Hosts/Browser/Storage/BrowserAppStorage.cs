using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;

using MusicPlayer2_Avalonia.Infrastructure.Storage;

namespace MusicPlayer2_Avalonia.Browser.Storage;

internal sealed class BrowserAppStorage : LocalAppStorage
{
    private BrowserAppStorage() : base("/musicplayer") { }

    public static async Task<BrowserAppStorage> OpenAsync(string databaseName = "musicplayer-sqlite")
    {
        await JSHost.ImportAsync("app-storage", "./app-storage.js");
        await BrowserAppStorageInterop.InitializeAsync(databaseName);
        var json = await BrowserAppStorageInterop.RestoreAsync();
        var files = JsonSerializer.Deserialize(json, StorageJsonContext.Default.DictionaryStringString)
            ?? throw new InvalidDataException("Invalid persisted storage index.");
        var storage = new BrowserAppStorage();
        try
        {
            foreach (var (key, base64) in files)
            {
                var path = storage.GetLocalPath(key);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllBytesAsync(path, Convert.FromBase64String(base64));
            }
            return storage;
        }
        catch { storage.Dispose(); throw; }
    }

    protected override Task PersistWriteAsync(string relativePath, byte[] content) =>
        BrowserAppStorageInterop.PersistAsync(relativePath, Convert.ToBase64String(content));

    protected override Task PersistDeleteAsync(string relativePath) =>
        BrowserAppStorageInterop.RemoveAsync(relativePath);
}

[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class StorageJsonContext : JsonSerializerContext;

internal static partial class BrowserAppStorageInterop
{
    [JSImport("initialize", "app-storage")]
    internal static partial Task InitializeAsync(string name);
    [JSImport("restore", "app-storage")]
    internal static partial Task<string> RestoreAsync();
    [JSImport("persist", "app-storage")]
    internal static partial Task PersistAsync(string path, string base64);
    [JSImport("remove", "app-storage")]
    internal static partial Task RemoveAsync(string path);
}