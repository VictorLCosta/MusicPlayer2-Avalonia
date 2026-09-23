using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Player;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MusicPlayer2_Avalonia.Application;
using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Application.Library;
using MusicPlayer2_Avalonia.Infrastructure;
using MusicPlayer2_Avalonia.Infrastructure.Persistence;
using MusicPlayer2_Avalonia.Infrastructure.Storage;

var root = Path.Combine(Path.GetTempPath(), "MusicPlayer2-MobileChecks-" + Guid.NewGuid().ToString("N"));
var collection = new ServiceCollection();
collection.AddSingleton<IAppStorage>(new LocalAppStorage(root));
collection.AddApplicationServices();
collection.AddInfrastructureServices();
var audioEngine = new DelayedAudioEngine();
collection.AddSingleton<IAudioEngine>(audioEngine);
await using var services = collection.BuildServiceProvider();
var db = services.GetRequiredService<MusicPlayerDbContext>();
await db.Database.EnsureCreatedAsync();
var importer = services.GetRequiredService<ManagedAudioImporter>();
var library = services.GetRequiredService<LibraryService>();
var audio = Wave(1000);
string first;
using (var source = new ProviderStream(audio)) first = await importer.ImportAsync("Music.wav", source);
Check(File.ReadAllBytes(first).SequenceEqual(audio), "non-seekable provider stream is copied without corruption");
await library.ScanLibrary(first);
using (var source = new ProviderStream(audio))
{
    var duplicate = await importer.ImportAsync("Music.wav", source);
    Check(duplicate == first, "selecting the same file again reuses the managed copy");
    await library.ScanLibrary(duplicate);
}
Check((await library.GetTracksAsync()).Count == 1, "reimport does not duplicate the library entry");
using (var source = new ProviderStream(Wave(4000)))
{
    var other = await importer.ImportAsync("Music.wav", source);
    Check(other != first && File.ReadAllBytes(first).SequenceEqual(audio), "different content with the same name preserves the first song");
    await library.ScanLibrary(other);
}
using (var source = new ProviderStream(audio))
{
    var safe = await importer.ImportAsync("../../unsafe?.wav", source);
    Check(Path.GetFullPath(safe).StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal),
        "provider filename cannot escape app storage");
}
using (var source = new ProviderStream(audio))
{
    try { await importer.ImportAsync("bad.exe", source); throw new Exception("Unsupported format accepted"); }
    catch (NotSupportedException) { Console.WriteLine("PASS: unsupported extension rejected"); }
}
using (var source = new ProviderStream(audio))
using (var cancellation = new CancellationTokenSource())
{
    cancellation.Cancel();
    try { await importer.ImportAsync("cancel.wav", source, cancellation.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { Console.WriteLine("PASS: cancelled import is not published"); }
}
Check(Directory.GetFiles(Path.Combine(root, "imports"), "*.tmp").Length == 0, "temporary files are removed after cancellation");
Check((await library.GetTracksAsync()).Count == 2, "copied songs remain indexed after provider streams close");
var tracks = await library.GetTracksAsync();
services.GetRequiredService<PlaybackQueue>().Replace(tracks.Select(track => track.TrackId));
var player = services.GetRequiredService<PlayerService>();
await Task.WhenAll(player.PlayAsync(tracks[0].TrackId), player.PlayAsync(tracks[1].TrackId));
Check(audioEngine.MaximumConcurrentLoads == 1 && player.CurrentTrack?.Id == tracks[1].TrackId,
    "UI and remote playback commands serialize asynchronous track preparation");
Console.WriteLine("All mobile checks passed. Test storage: " + root);

static void Check(bool result, string name)
{
    if (!result) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}
static byte[] Wave(short amplitude)
{
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    const int samples = 4410;
    writer.Write("RIFF"u8); writer.Write(36 + samples * 2); writer.Write("WAVEfmt "u8);
    writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(44100);
    writer.Write(88200); writer.Write((short)2); writer.Write((short)16);
    writer.Write("data"u8); writer.Write(samples * 2);
    for (var i = 0; i < samples; i++) writer.Write((short)(amplitude * Math.Sin(i * 0.1)));
    return stream.ToArray();
}
sealed class ProviderStream(byte[] bytes) : Stream
{
    private readonly MemoryStream _inner = new(bytes, false);
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) => _inner.ReadAsync(buffer, token);
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    protected override void Dispose(bool disposing) { if (disposing) _inner.Dispose(); base.Dispose(disposing); }
}

sealed class DelayedAudioEngine : IAudioEngine
{
    private readonly object _gate = new();
    private int _loads;
    public int MaximumConcurrentLoads { get; private set; }
    public event EventHandler? PlaybackEnded { add { } remove { } }
    public bool IsPlaying { get; private set; }
    public TimeSpan Position { get; private set; }
    public TimeSpan Duration => TimeSpan.FromSeconds(1);
    public double Volume { get; set; } = 1;
    public async Task LoadAsync(Uri source)
    {
        lock (_gate) { _loads++; MaximumConcurrentLoads = Math.Max(MaximumConcurrentLoads, _loads); }
        try { await Task.Delay(80); }
        finally { lock (_gate) _loads--; }
    }
    public void Play() => IsPlaying = true;
    public void Pause() => IsPlaying = false;
    public void StopAudio() { IsPlaying = false; Position = TimeSpan.Zero; }
    public void Seek(TimeSpan position) => Position = position;
    public void Dispose() { }
}
