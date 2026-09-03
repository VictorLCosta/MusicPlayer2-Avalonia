namespace MusicPlayer2_Avalonia.Domain.Entities;

public sealed class Artist : Entity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Track> Tracks { get; } = [];
}