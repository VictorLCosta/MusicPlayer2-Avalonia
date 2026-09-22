using MusicPlayer2_Avalonia.Application.Common;

namespace MusicPlayer2_Avalonia.Infrastructure.Metadata;

public sealed class AlbumArtworkReader : IAlbumArtworkReader
{
    public Task<byte[]?> ReadAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Task.Run(() =>
        {
            using var file = TagLib.File.Create(path);
            var picture = file.Tag.Pictures.FirstOrDefault(image => image.Type == TagLib.PictureType.FrontCover)
                ?? file.Tag.Pictures.FirstOrDefault();
            return picture?.Data.Data;
        });
    }
}