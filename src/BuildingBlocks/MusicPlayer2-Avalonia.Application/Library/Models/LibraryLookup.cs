using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Application.Library.Models;

public sealed record LibraryLookup(
    Dictionary<string, Artist> Artists,
    Dictionary<AlbumKey, Album> Albums
);