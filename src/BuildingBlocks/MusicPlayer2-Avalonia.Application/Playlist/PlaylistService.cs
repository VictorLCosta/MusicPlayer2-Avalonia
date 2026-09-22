using Microsoft.EntityFrameworkCore;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Playlist.Models;

using PlaylistEntity = MusicPlayer2_Avalonia.Domain.Entities.Playlist;

namespace MusicPlayer2_Avalonia.Application.Playlist;

public sealed class PlaylistService(IMusicPlayerDbContext dbContext)
{
    public async Task<PlaylistDto> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var playlist = PlaylistEntity.Create(name);

        dbContext.Add(playlist);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PlaylistDto(playlist.Id, playlist.Name);
    }

    public async Task<IReadOnlyList<PlaylistDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext
            .Playlists
            .AsNoTracking()
            .Select(playlist => new PlaylistDto(playlist.Id, playlist.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<PlaylistDetailsDto?> GetByIdAsync(Guid playlistId, CancellationToken cancellationToken = default)
    {
        return await dbContext
            .Playlists
            .AsNoTracking()
            .Where(playlist => playlist.Id == playlistId)
            .Select(playlist => new PlaylistDetailsDto(
                playlist.Id,
                playlist.Name,
                (from item in playlist.PlaylistItems
                 join track in dbContext.Tracks on item.TrackId equals track.Id
                 orderby item.Position
                 select new ListTrackDto(
                     item.Id,
                     track.Id,
                     item.Position,
                     track.Title,
                     track.Artist == null ? null : track.Artist.Name,
                     track.Album == null ? null : track.Album.Title,
                     track.Duration,
                     track.SourceFileSizeBytes ?? 0)).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RenameAsync(
        Guid playlistId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var playlist = await GetRequiredPlaylistAsync(playlistId, cancellationToken);

        playlist.Rename(name);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTrackAsync(
        Guid playlistId,
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Tracks.AnyAsync(x => x.Id == trackId, cancellationToken))
        {
            throw new KeyNotFoundException($"Track '{trackId}' was not found.");
        }

        var playlist = await GetRequiredPlaylistAsync(playlistId, cancellationToken);

        playlist.AddTrack(trackId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveItemAsync(
        Guid playlistId,
        Guid playlistItemId,
        CancellationToken cancellationToken = default)
    {
        var playlist = await GetRequiredPlaylistAsync(playlistId, cancellationToken);

        if (!playlist.RemoveItem(playlistItemId))
        {
            throw new KeyNotFoundException($"Playlist item '{playlistItemId}' was not found.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveItemAsync(
        Guid playlistId,
        Guid playlistItemId,
        int targetPosition,
        CancellationToken cancellationToken = default)
    {
        var playlist = await GetRequiredPlaylistAsync(playlistId, cancellationToken);

        playlist.MoveItem(playlistItemId, targetPosition);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid playlistId, CancellationToken cancellationToken = default)
    {
        var playlist = await GetRequiredPlaylistAsync(playlistId, cancellationToken);

        dbContext.Remove(playlist);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<PlaylistEntity> GetRequiredPlaylistAsync(
        Guid playlistId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Playlists
            .Include(playlist => playlist.PlaylistItems)
            .FirstOrDefaultAsync(playlist => playlist.Id == playlistId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Playlist '{playlistId}' was not found.");
    }
}