namespace MusicPlayer2_Avalonia.Domain.Entities;

public sealed class Playlist : Entity
{
    public string Name { get; set; } = string.Empty;

    private readonly List<PlaylistItem> _playlistItems = [];

    public IReadOnlyCollection<PlaylistItem> PlaylistItems => _playlistItems;

    public static Playlist Create(string name)
    {
        return new Playlist
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
        };
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void AddTrack(Guid trackId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(trackId, Guid.Empty);

        _playlistItems.Add(new PlaylistItem
        {
            Id = Guid.NewGuid(),
            TrackId = trackId,
            Position = _playlistItems.Count,
            AddedAt = DateTimeOffset.UtcNow,
        });
    }

    public bool RemoveItem(Guid playlistItemId)
    {
        var item = _playlistItems.SingleOrDefault(item => item.Id == playlistItemId);

        if (item is null)
        {
            return false;
        }

        _playlistItems.Remove(item);
        UpdatePositions();

        return true;
    }

    public void MoveItem(Guid playlistItemId, int targetPosition)
    {
        var item = _playlistItems.SingleOrDefault(item => item.Id == playlistItemId)
            ?? throw new InvalidOperationException("The playlist item does not exist.");

        if (targetPosition < 0 || targetPosition >= _playlistItems.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPosition));
        }

        _playlistItems.Remove(item);
        _playlistItems.Insert(targetPosition, item);
        UpdatePositions();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A playlist name is required.", nameof(name));
        }

        return name.Trim();
    }

    private void UpdatePositions()
    {
        for (var index = 0; index < _playlistItems.Count; index++)
        {
            _playlistItems[index].Position = index;
        }
    }
}
