using Microsoft.EntityFrameworkCore;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Library.Models;
using MusicPlayer2_Avalonia.Application.Playlist.Models;
using MusicPlayer2_Avalonia.Domain.Entities;
using MusicPlayer2_Avalonia.Domain.ValueObjects;

namespace MusicPlayer2_Avalonia.Application.Library;

public sealed class LibraryService(
    IMusicPlayerDbContext dbContext,
    IMetaDataReader metaDataReader)
{
    public async Task<IReadOnlyList<ListTrackDto>> GetTracksAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Tracks
            .AsNoTracking()
            .OrderBy(track => track.Title)
            .Select(track => new ListTrackDto(
                Guid.Empty,
                track.Id,
                0,
                track.Title,
                track.Artist != null ? track.Artist.Name : null,
                track.Album != null ? track.Album.Title : null,
                track.Duration,
                track.SourceFileSizeBytes ?? 0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    private const int SaveBatchSize = 100;
    public static bool IsSupportedAudioExtension(string extension) => SupportedAudioExtensions.Contains(extension);

    private static readonly HashSet<string> SupportedAudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".aac", ".aiff", ".ape", ".dsf", ".flac", ".m4a", ".m4b", ".mp3", ".ogg", ".opus",
        ".wav", ".wma", ".wv"
    };

    public async Task ScanLibrary(string filePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = ValidateAudioFile(filePath);
        var fileInfo = new FileInfo(absolutePath);
        var existingTrack = await dbContext.Tracks
            .FirstOrDefaultAsync(track => track.Source.Path == absolutePath, cancellationToken)
            .ConfigureAwait(false);

        if (existingTrack is not null && !HasFileChanged(existingTrack, fileInfo))
        {
            return;
        }

        await ImportOrUpdateAsync(
                existingTrack,
                fileInfo,
                lookup: null,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<LibraryScanResult> ScanDirectoryAsync(
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        var absoluteDirectoryPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(absoluteDirectoryPath))
        {
            throw new DirectoryNotFoundException($"A pasta '{absoluteDirectoryPath}' nao foi encontrada.");
        }

        var indexedTracks = await dbContext.Tracks
            .AsNoTracking()
            .Select(track => new IndexedTrack(
                track.Id,
                track.Source.Path,
                track.SourceFileSizeBytes,
                track.SourceLastWriteTimeUtc))
            .ToDictionaryAsync(track => track.Path, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        var lookup = await CreateLookupAsync(cancellationToken).ConfigureAwait(false);

        int added = 0;
        int updated = 0;
        int unchanged = 0;
        int failed = 0;
        int pendingChanges = 0;

        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        foreach (var filePath in Directory.EnumerateFiles(absoluteDirectoryPath, "*", enumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!SupportedAudioExtensions.Contains(Path.GetExtension(filePath)))
            {
                continue;
            }

            var fileInfo = new FileInfo(filePath);
            if (indexedTracks.TryGetValue(filePath, out var indexedTrack) && !HasFileChanged(indexedTrack, fileInfo))
            {
                unchanged++;
                continue;
            }

            Track? existingTrack = null;
            if (indexedTrack is not null)
            {
                existingTrack = await dbContext.Tracks
                    .FirstAsync(track => track.Id == indexedTrack.Id, cancellationToken)
                    .ConfigureAwait(false);
            }

            try
            {
                await ImportOrUpdateAsync(existingTrack, fileInfo, lookup, cancellationToken).ConfigureAwait(false);
                pendingChanges++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (MetadataReadException)
            {
                failed++;
                continue;
            }
            catch (IOException)
            {
                failed++;
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                failed++;
                continue;
            }

            if (existingTrack is null)
            {
                added++;
            }
            else
            {
                updated++;
            }

            if (pendingChanges == SaveBatchSize)
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                pendingChanges = 0;
            }
        }

        if (pendingChanges > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new LibraryScanResult(added, updated, unchanged, failed);
    }

    public async Task<List<Track>> SearchLibrary(string searchTerm, CancellationToken cancellationToken = default)
    {
        var tracks = await dbContext
            .Tracks
            .AsNoTracking()
            .Where(x => x.Title.Contains(searchTerm))
            .OrderByDescending(x => x.Title)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return tracks;
    }

    public async Task<int> RemoveMissingTracksAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directoryPath)) + Path.DirectorySeparatorChar;
        // An unavailable drive or root must never be treated as a deleted collection.
        if (!Directory.Exists(root)) return 0;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var tracks = await dbContext.Tracks.ToListAsync(cancellationToken).ConfigureAwait(false);
        var missing = new List<Track>();
        foreach (var track in tracks.Where(track => track.Source.Path.StartsWith(root, comparison)))
        {
            try { _ = File.GetAttributes(track.Source.Path); }
            catch (FileNotFoundException) { missing.Add(track); }
            catch (DirectoryNotFoundException) { missing.Add(track); }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
        if (missing.Count == 0) return 0;
        var ids = missing.Select(track => track.Id).ToArray();
        var playlistItems = await dbContext.PlaylistItems.Where(item => ids.Contains(item.TrackId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var item in playlistItems) dbContext.Remove(item);
        foreach (var track in missing) dbContext.Remove(track);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return missing.Count;
    }

    private async Task ImportOrUpdateAsync(
        Track? track,
        FileInfo fileInfo,
        LibraryLookup? lookup,
        CancellationToken cancellationToken)
    {
        var metadata = await metaDataReader.Read(fileInfo.FullName, cancellationToken).ConfigureAwait(false);
        var artist = await GetOrCreateArtistAsync(metadata.Artist, lookup, cancellationToken).ConfigureAwait(false);
        var album = await GetOrCreateAlbumAsync(metadata, artist, lookup, cancellationToken).ConfigureAwait(false);

        if (track is null)
        {
            track = new Track
            {
                Id = Guid.NewGuid(),
                Source = LocalAudioFile.Create(fileInfo.FullName),
                Position = TrackPosition.Create(1, 1)
            };
            dbContext.Add(track);
        }

        track.Title = metadata.Title;
        track.Duration = metadata.Duration;
        track.ReleaseYear = metadata.ReleaseYear;
        track.Genre = metadata.Genre;
        track.Artist = artist;
        track.ArtistId = artist?.Id;
        track.Album = album;
        track.AlbumId = album?.Id;
        track.SourceFileSizeBytes = fileInfo.Length;
        track.SourceLastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
        track.AudioProperties = new AudioProperties(
            metadata.Duration,
            metadata.BitrateKbps,
            metadata.SampleRateHz,
            metadata.Channels,
            BitDepth: 0);
    }

    private static string ValidateAudioFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var absolutePath = Path.GetFullPath(filePath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("O arquivo de audio nao foi encontrado.", absolutePath);
        }

        if (!SupportedAudioExtensions.Contains(Path.GetExtension(absolutePath)))
        {
            throw new NotSupportedException("O arquivo informado nao e um formato de audio suportado.");
        }

        return absolutePath;
    }

    private static bool HasFileChanged(Track track, FileInfo fileInfo)
    {
        return track.SourceFileSizeBytes != fileInfo.Length
            || track.SourceLastWriteTimeUtc != fileInfo.LastWriteTimeUtc;
    }

    private static bool HasFileChanged(IndexedTrack track, FileInfo fileInfo)
    {
        return track.FileSizeBytes != fileInfo.Length
            || track.LastWriteTimeUtc != fileInfo.LastWriteTimeUtc;
    }

    private async Task<Artist?> GetOrCreateArtistAsync(
        string? artistName,
        LibraryLookup? lookup,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            return null;
        }

        if (lookup is not null && lookup.Artists.TryGetValue(artistName, out var cachedArtist))
        {
            return cachedArtist;
        }

        var artist = await dbContext.Artists
            .FirstOrDefaultAsync(entity => entity.Name == artistName, cancellationToken)
            .ConfigureAwait(false);

        if (artist is not null)
        {
            return artist;
        }

        artist = new Artist { Id = Guid.NewGuid(), Name = artistName };
        dbContext.Add(artist);
        lookup?.Artists.TryAdd(artistName, artist);

        return artist;
    }

    private async Task<Album?> GetOrCreateAlbumAsync(
        AudioMetadata metadata,
        Artist? artist,
        LibraryLookup? lookup,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(metadata.Album))
        {
            return null;
        }

        Guid? artistId = artist?.Id;
        var albumKey = new AlbumKey(metadata.Album, artistId);
        if (lookup is not null && lookup.Albums.TryGetValue(albumKey, out var cachedAlbum))
        {
            return cachedAlbum;
        }

        var album = await dbContext.Albums
            .FirstOrDefaultAsync(
                entity => entity.Title == metadata.Album && entity.AlbumArtistId == artistId,
                cancellationToken)
            .ConfigureAwait(false);

        if (album is not null)
        {
            return album;
        }

        album = new Album
        {
            Id = Guid.NewGuid(),
            Title = metadata.Album,
            ReleaseYear = metadata.ReleaseYear,
            Artist = artist,
            AlbumArtistId = artistId
        };
        dbContext.Add(album);
        lookup?.Albums.TryAdd(albumKey, album);

        return album;
    }

    private async Task<LibraryLookup> CreateLookupAsync(CancellationToken cancellationToken)
    {
        var artists = await dbContext.Artists.ToListAsync(cancellationToken).ConfigureAwait(false);
        var albums = await dbContext.Albums.ToListAsync(cancellationToken).ConfigureAwait(false);
        var artistLookup = new Dictionary<string, Artist>(StringComparer.OrdinalIgnoreCase);
        var albumLookup = new Dictionary<AlbumKey, Album>();

        foreach (var artist in artists)
        {
            artistLookup.TryAdd(artist.Name, artist);
        }

        foreach (var album in albums)
        {
            albumLookup.TryAdd(new AlbumKey(album.Title, album.AlbumArtistId), album);
        }

        return new LibraryLookup(artistLookup, albumLookup);
    }

}
