namespace MusicPlayer2_Avalonia.Domain.Entities;

public class Album : Entity
{
    public string Title { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }

    public Artist? Artist { get; set; }
    public Guid? AlbumArtistId { get; set; }
}
