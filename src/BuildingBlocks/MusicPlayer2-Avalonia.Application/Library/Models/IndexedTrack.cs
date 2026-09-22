namespace MusicPlayer2_Avalonia.Application.Library.Models;

public sealed record IndexedTrack(
    Guid Id,
    string Path,
    long? FileSizeBytes,
    DateTime? LastWriteTimeUtc
);