using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MusicPlayer2_Avalonia.Browser.Storage;

[SupportedOSPlatform("browser")]
internal sealed class IndexedDbService : IIndexedDbService
{
    private const string ModuleName = "indexed-db";

    private static readonly HashSet<string> StoreNames = new(StringComparer.Ordinal)
    {
        "albums",
        "artists",
        "playlists",
        "tracks"
    };

    private static Task<JSObject>? _moduleTask;

    public async Task PutAsync<T>(
        string storeName,
        string key,
        T value,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ValidateStoreAndKey(storeName, key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureModuleAsync().ConfigureAwait(false);
        await IndexedDbInterop.PutAsync(
                storeName,
                key,
                JsonSerializer.Serialize(value, jsonTypeInfo))
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
    }

    public async Task<T?> GetAsync<T>(
        string storeName,
        string key,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ValidateStoreAndKey(storeName, key);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureModuleAsync().ConfigureAwait(false);
        var json = await IndexedDbInterop.GetAsync(storeName, key).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return json == "null" ? default : JsonSerializer.Deserialize(json, jsonTypeInfo);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(
        string storeName,
        JsonTypeInfo<List<T>> jsonTypeInfo,
        CancellationToken cancellationToken = default)
    {
        ValidateStoreName(storeName);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureModuleAsync().ConfigureAwait(false);
        var json = await IndexedDbInterop.GetAllAsync(storeName).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return JsonSerializer.Deserialize(json, jsonTypeInfo) ?? [];
    }

    public async Task DeleteAsync(
        string storeName,
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateStoreAndKey(storeName, key);
        cancellationToken.ThrowIfCancellationRequested();

        await EnsureModuleAsync().ConfigureAwait(false);
        await IndexedDbInterop.DeleteAsync(storeName, key).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static Task<JSObject> EnsureModuleAsync()
    {
        _moduleTask ??= JSHost.ImportAsync(ModuleName, "/indexed-db.js");

        return _moduleTask;
    }

    private static void ValidateStoreAndKey(string storeName, string key)
    {
        ValidateStoreName(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }

    private static void ValidateStoreName(string storeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);

        if (!StoreNames.Contains(storeName))
        {
            throw new ArgumentOutOfRangeException(nameof(storeName), "Store IndexedDB nao suportado.");
        }
    }
}

[SupportedOSPlatform("browser")]
internal static partial class IndexedDbInterop
{
    [JSImport("put", "indexed-db")]
    internal static partial Task PutAsync(string storeName, string key, string valueJson);

    [JSImport("get", "indexed-db")]
    internal static partial Task<string> GetAsync(string storeName, string key);

    [JSImport("getAll", "indexed-db")]
    internal static partial Task<string> GetAllAsync(string storeName);

    [JSImport("remove", "indexed-db")]
    internal static partial Task DeleteAsync(string storeName, string key);
}