using System.Text.Json.Serialization.Metadata;

namespace MusicPlayer2_Avalonia.Browser.Storage;

internal interface IIndexedDbService
{
    Task PutAsync<T>(
        string storeName,
        string key,
        T value,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(
        string storeName,
        string key,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync<T>(
        string storeName,
        JsonTypeInfo<List<T>> jsonTypeInfo,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storeName,
        string key,
        CancellationToken cancellationToken = default);
}