using MusicPlayer2_Avalonia.Domain.Entities;

using PlaylistEntity = MusicPlayer2_Avalonia.Domain.Entities.Playlist;

namespace MusicPlayer2_Avalonia.Application.Common;

public interface IMusicPlayerDbContext
{
    IQueryable<Track> Tracks { get; }
    IQueryable<Artist> Artists { get; }
    IQueryable<Album> Albums { get; }
    IQueryable<PlaylistEntity> Playlists { get; }
    IQueryable<PlaylistItem> PlaylistItems { get; }

    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    void Remove<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}