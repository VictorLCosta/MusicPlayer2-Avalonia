namespace MusicPlayer2_Avalonia.Application.Common;

public interface IMetaDataReader
{
    Task<AudioMetadata> Read(string filePath, CancellationToken cancellationToken = default);
}

public sealed class MetadataReadException(string filePath, Exception innerException)
    : Exception($"Nao foi possivel ler os metadados de '{filePath}'.", innerException);

public sealed record AudioMetadata(
    string Title,
    string? Artist,
    string? Album,
    string? Genre,
    int? ReleaseYear,
    TimeSpan Duration,
    int BitrateKbps,
    int SampleRateHz,
    int Channels);
