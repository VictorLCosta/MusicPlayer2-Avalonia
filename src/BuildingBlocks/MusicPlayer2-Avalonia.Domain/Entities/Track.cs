using MusicPlayer2_Avalonia.Domain.ValueObjects;

namespace MusicPlayer2_Avalonia.Domain.Entities;

public sealed class Track : Entity
{
    public string Title { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Genre { get; set; }

    public Guid? ArtistId { get; set; }
    public Artist? Artist { get; set; }

    public Guid? AlbumId { get; set; }
    public Album? Album { get; set; }

    public LocalAudioFile Source { get; set; } = default!;
    public long? SourceFileSizeBytes { get; set; }
    public DateTime? SourceLastWriteTimeUtc { get; set; }
    public TrackPosition Position { get; set; }
    public AudioProperties AudioProperties { get; set; } = default!;
}
