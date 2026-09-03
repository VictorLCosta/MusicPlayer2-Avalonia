using MusicPlayer2_Avalonia.Application.Common;

using PlaylistEntity = MusicPlayer2_Avalonia.Domain.Entities.Playlist;

using Microsoft.EntityFrameworkCore;

namespace MusicPlayer2_Avalonia.Application.Playlist;

public sealed class PlaylistService(IMusicPlayerDbContext dbContext)
{
    public async Task<PlaylistEntity> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var playlist = PlaylistEntity.Create(name);

        dbContext.Add(playlist);
        await dbContext.SaveChangesAsync(cancellationToken);

        return playlist;
    }

    public async Task<IReadOnlyList<PlaylistEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext
            .Playlists
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PlaylistEntity?> GetByIdAsync(Guid playlistId, CancellationToken cancellationToken = default)
    {
        return await dbContext
            .Playlists
            .Include(playlist => playlist.PlaylistItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(playlist => playlist.Id == playlistId, cancellationToken)
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
