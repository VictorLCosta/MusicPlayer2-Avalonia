using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Infrastructure.Storage;

/// <summary>File implementation shared by native hosts and the browser's virtual filesystem.</summary>
public class LocalAppStorage : IAppStorage, IDisposable
{
    private readonly SemaphoreSlim _access = new(1, 1);
    private bool _faulted;

    public string RootDirectory { get; }

    public LocalAppStorage(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        if (!Path.IsPathFullyQualified(rootDirectory))
            throw new ArgumentException("The storage root must be absolute.", nameof(rootDirectory));

        RootDirectory = Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(RootDirectory);
    }

    public static LocalAppStorage CreateDefault()
    {
        if (OperatingSystem.IsBrowser())
            throw new PlatformNotSupportedException("Initialize BrowserAppStorage before starting the application.");

        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.Create);
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("The application's local data directory is unavailable.");

        return new LocalAppStorage(Path.Combine(root, "MusicPlayer2-Avalonia"));
    }

    public string GetLocalPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        // A single portable path syntax also prevents Windows drive/ADS paths on Unix/WASM.
        var parts = relativePath.Replace('\\', '/').Split('/');
        if (parts.Any(part => part is "" or "." or ".." || part.EndsWith('.') || part.EndsWith(' ')
            || part.IndexOfAny([':', '\0', '<', '>', '"', '|', '?', '*']) >= 0))
            throw new ArgumentException("Use a relative path without empty, dot or parent segments.", nameof(relativePath));
        var path = RootDirectory;
        foreach (var part in parts)
        {
            path = Path.Combine(path, part);
            if (Path.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Storage paths cannot follow symbolic links or junctions.");
        }
        return path;
    }

    public async Task<byte[]?> ReadBytesAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = GetLocalPath(relativePath);
        await _access.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureHealthy();
            try { return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }
        finally { _access.Release(); }
    }

    public async Task WriteBytesAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var path = GetLocalPath(relativePath);
        var key = relativePath.Replace('\\', '/');
        var copy = content.ToArray();

        await _access.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            EnsureHealthy();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await File.WriteAllBytesAsync(temporary, copy, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporary, path, overwrite: true);
            }
            finally { File.Delete(temporary); }

            try { await PersistWriteAsync(key, copy).ConfigureAwait(false); }
            catch { _faulted = true; throw; }
        }
        finally { _access.Release(); }
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = GetLocalPath(relativePath);
        await _access.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureHealthy();
            try { File.Delete(path); }
            catch (DirectoryNotFoundException) { }
            try { await PersistDeleteAsync(relativePath.Replace('\\', '/')).ConfigureAwait(false); }
            catch { _faulted = true; throw; }
        }
        finally { _access.Release(); }
    }

    protected virtual Task PersistWriteAsync(string relativePath, byte[] content) => Task.CompletedTask;
    protected virtual Task PersistDeleteAsync(string relativePath) => Task.CompletedTask;

    private void EnsureHealthy()
    {
        if (_faulted)
            throw new IOException("Storage persistence failed. Restart to restore the last saved data.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing) _access.Dispose();
    }
}