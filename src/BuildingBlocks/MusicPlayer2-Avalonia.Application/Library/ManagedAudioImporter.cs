using System.Security.Cryptography;
using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Application.Library;

/// <summary>Copies provider-owned streams into app storage so playback survives expired URI permissions.</summary>
public sealed class ManagedAudioImporter(IAppStorage storage)
{
    public async Task<string> ImportAsync(string name, Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(source);
        var extension = Path.GetExtension(name).ToUpperInvariant();
        if (!LibraryService.IsSupportedAudioExtension(extension))
            throw new NotSupportedException("Formato de áudio não suportado: " + extension);
        var directory = storage.GetLocalPath("imports");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var output = File.Create(temporary))
                await source.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            byte[] hash;
            await using (var input = File.OpenRead(temporary))
                hash = await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false);
            var targetDirectory = Path.Combine(directory, Convert.ToHexString(hash));
            Directory.CreateDirectory(targetDirectory);
            // Keep a useful fallback track title while excluding every storage-path special character.
            var cleanName = new string(Path.GetFileNameWithoutExtension(name)
                .Select(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c, StringComparison.Ordinal) ? '_' : c).ToArray()).Trim(' ', '.');
            if (string.IsNullOrEmpty(cleanName)) cleanName = "Audio";
            cleanName = cleanName[..Math.Min(cleanName.Length, 100)];
            var target = Path.Combine(targetDirectory, cleanName + extension);
            if (!File.Exists(target)) File.Move(temporary, target);
            return target;
        }
        finally { File.Delete(temporary); }
    }
}

