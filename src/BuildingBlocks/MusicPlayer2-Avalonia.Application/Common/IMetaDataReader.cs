namespace MusicPlayer2_Avalonia.Application.Common;

public interface IMetaDataReader
{
    Task<AudioMetadata> Read(string filePath, CancellationToken cancellationToken = default);
}

public sealed class MetadataReadException : Exception
{
    public MetadataReadException()
    {
    }

    public MetadataReadException(string? message)
        : base(message)
    {
    }

    public MetadataReadException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

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