using Microsoft.EntityFrameworkCore;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Domain.Entities;

namespace MusicPlayer2_Avalonia.Infrastructure.Persistence;

public class MusicPlayerDbContext(DbContextOptions options)
    : BaseDbContext(options), IMusicPlayerDbContext
{
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistItem> PlaylistItems => Set<PlaylistItem>();

    IQueryable<Track> IMusicPlayerDbContext.Tracks => Tracks;
    IQueryable<Artist> IMusicPlayerDbContext.Artists => Artists;
    IQueryable<Album> IMusicPlayerDbContext.Albums => Albums;
    IQueryable<Playlist> IMusicPlayerDbContext.Playlists => Playlists;
    IQueryable<PlaylistItem> IMusicPlayerDbContext.PlaylistItems => PlaylistItems;

    void IMusicPlayerDbContext.Add<TEntity>(TEntity entity) => Add(entity);

    void IMusicPlayerDbContext.Remove<TEntity>(TEntity entity) => Remove(entity);
}
