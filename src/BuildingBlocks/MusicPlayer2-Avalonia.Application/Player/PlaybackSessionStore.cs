using System.Text.Json;
using System.Text.Json.Serialization;

using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Application.Player;

public sealed record PlaybackSession(Guid? TrackId, double PositionSeconds, Guid[] Queue);

public sealed class PlaybackSessionStore(IAppStorage storage) : IDisposable
{
    private const string FileName = "playback-session.json";
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<PlaybackSession?> LoadAsync()
    {
        var bytes = await storage.ReadBytesAsync(FileName).ConfigureAwait(false);
        return bytes is null ? null : JsonSerializer.Deserialize(bytes, PlaybackSessionJsonContext.Default.PlaybackSession);
    }

    public async Task SaveAsync(PlaybackSession? session)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (session is null)
                await storage.DeleteAsync(FileName).ConfigureAwait(false);
            else
                await storage.WriteBytesAsync(FileName,
                    JsonSerializer.SerializeToUtf8Bytes(session, PlaybackSessionJsonContext.Default.PlaybackSession))
                    .ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public void Dispose() => _gate.Dispose();
}

[JsonSerializable(typeof(PlaybackSession))]
internal sealed partial class PlaybackSessionJsonContext : JsonSerializerContext;