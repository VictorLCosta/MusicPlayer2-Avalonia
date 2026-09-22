namespace MusicPlayer2_Avalonia.Application.Playlist.Models;

public sealed record PlaylistDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<ListTrackDto> Tracks);