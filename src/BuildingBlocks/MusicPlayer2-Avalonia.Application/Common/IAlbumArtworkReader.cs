namespace MusicPlayer2_Avalonia.Application.Common;

public interface IAlbumArtworkReader
{
    Task<byte[]?> ReadAsync(string path);
}