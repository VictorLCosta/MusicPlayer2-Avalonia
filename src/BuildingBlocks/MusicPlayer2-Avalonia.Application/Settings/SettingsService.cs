using System.Text.Json;
using System.Text.Json.Serialization;

using MusicPlayer2_Avalonia.Application.Common.Storage;

using MusicPlayer2_Avalonia.Application.Settings.Models;

namespace MusicPlayer2_Avalonia.Application.Settings;

public sealed class SettingsService(IAppStorage storage) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _loaded;
    public AppSettings Current { get; private set; } = new();
    public event EventHandler? Changed;

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var changed = false;
        try
        {
            if (!_loaded)
            {
                var bytes = await storage.ReadBytesAsync(AppStorageFiles.Settings, cancellationToken).ConfigureAwait(false);
                Current = bytes is null ? new AppSettings()
                    : JsonSerializer.Deserialize(bytes, SettingsJsonContext.Default.AppSettings)
                      ?? throw new InvalidDataException("The settings document is null.");
                _loaded = true;
                changed = true;
            }
        }
        finally { _gate.Release(); }
        if (changed) Changed?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var snapshot = settings with { LibraryFolders = settings.LibraryFolders.ToArray() };
            await storage.WriteBytesAsync(AppStorageFiles.Settings,
                JsonSerializer.SerializeToUtf8Bytes(snapshot, SettingsJsonContext.Default.AppSettings), cancellationToken)
                .ConfigureAwait(false);
            Current = snapshot;
            _loaded = true;
        }
        finally { _gate.Release(); }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _gate.Dispose();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;