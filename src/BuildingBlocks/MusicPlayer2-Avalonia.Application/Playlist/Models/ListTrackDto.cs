namespace MusicPlayer2_Avalonia.Application.Playlist.Models;

public sealed record ListTrackDto(
    Guid PlaylistItemId,
    Guid TrackId,
    int Position,
    string Title,
    string? ArtistName,
    string? AlbumTitle,
    TimeSpan Duration,
    long FileSizeBytes = 0);