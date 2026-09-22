namespace MusicPlayer2_Avalonia.Application.Common.Storage;

/// <summary>Application-owned files. Use these operations for durable writes on every host.</summary>
public interface IAppStorage
{
    /// <summary>Native directory on installed hosts; virtual WASM directory in the browser.</summary>
    string RootDirectory { get; }

    /// <summary>For APIs requiring a filename. Direct File/Directory writes bypass browser persistence.</summary>
    string GetLocalPath(string relativePath);

    Task<byte[]?> ReadBytesAsync(string relativePath, CancellationToken cancellationToken = default);
    Task WriteBytesAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}