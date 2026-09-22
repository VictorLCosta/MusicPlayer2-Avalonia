namespace MusicPlayer2_Avalonia.Domain.Entities;

public sealed class PlaylistItem : Entity
{
    public Guid TrackId { get; set; }
    public int Position { get; set; }

    public DateTimeOffset AddedAt { get; set; }
}