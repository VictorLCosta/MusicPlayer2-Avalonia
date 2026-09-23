using System.Reflection;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;
using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Infrastructure.AudioEngine;

if (args.Length != 1) throw new ArgumentException("Pass the directory containing native LibVLC libraries.");
Core.Initialize(Path.GetFullPath(args[0]));
using var engine = new VlcAudioEngine();
var storage = new MemoryStorage();
var service = new EqualizerService(engine, storage);
await service.InitializeAsync();
Check(service.Frequencies.Count == 10, "VLC exposes ten bands");
var gains = new float[service.Frequencies.Count];
gains[5] = -12;
service.Apply(new(true, -3, gains));
await service.SaveAsync();
var restored = new EqualizerService(engine, storage);
await restored.InitializeAsync();
Check(restored.Current.Enabled && restored.Current.Preamp == -3 && restored.Current.Gains[5] == -12,
    "enabled state, preamp and bands survive a new service instance");
var saved = restored.Current;
saved.Gains[5] = 12;
Check(restored.Current.Gains[5] == -12, "settings snapshots cannot mutate active state");
try { service.Apply(new(true, float.NaN, gains)); throw new Exception("Invalid gain accepted"); }
catch (ArgumentException) { Console.WriteLine("PASS: non-finite gain rejected"); }
storage.Bytes = "{ broken json"u8.ToArray();
var invalid = new EqualizerService(engine, storage);
await invalid.InitializeAsync();
Check(!invalid.Current.Enabled && invalid.Current.Gains.All(gain => gain == 0) && invalid.LoadError is not null,
    "corrupt persisted data falls back to flat, disabled settings");

// Capture the production player's PCM output without sending a test tone to speakers.
var player = (MediaPlayer)typeof(VlcAudioEngine).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(engine)!;
var gate = new object();
double squareSum = 0;
long sampleCount = 0;
player.SetAudioCallbacks((_, samples, count, _) =>
{
    var buffer = new short[checked((int)count)];
    Marshal.Copy(samples, buffer, 0, buffer.Length);
    lock (gate)
    {
        foreach (var sample in buffer) squareSum += (double)sample * sample;
        sampleCount += buffer.Length;
    }
    Thread.Sleep(Math.Max(1, (int)(count * 1000 / 44100)));
}, (_, _) => { }, (_, _) => { }, (_, _) => { }, _ => { });
player.SetAudioFormat("S16N", 44100, 1);
var wave = Path.Combine(Path.GetTempPath(), "equalizer-check-" + Guid.NewGuid() + ".wav");
try
{
    using (var writer = new BinaryWriter(File.Create(wave)))
    {
        const int count = 44100 * 30;
        writer.Write("RIFF"u8); writer.Write(36 + count * 2); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(44100);
        writer.Write(88200); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(count * 2);
        for (var i = 0; i < count; i++) writer.Write((short)(4000 * Math.Sin(2 * Math.PI * 1000 * i / 44100)));
    }
    engine.ApplyEqualizer(false, 0, new float[10]);
    await engine.LoadAsync(new Uri(wave));
    engine.Play();
    var flat = await Measure();
    engine.ApplyEqualizer(true, -12, new float[10]);
    var preamp = await Measure();
    Check(preamp / flat < 0.4, $"live preamp attenuates PCM (ratio {preamp / flat:F3})");
    engine.ApplyEqualizer(true, 0, gains);
    var cut = await Measure();
    Check(cut / flat < 0.8, $"live 1 kHz band attenuates a 1 kHz tone (ratio {cut / flat:F3})");
    engine.ApplyEqualizer(false, 0, gains);
    var bypass = await Measure();
    Check(Math.Abs(bypass / flat - 1) < 0.15, $"bypass restores unfiltered audio (ratio {bypass / flat:F3})");
    engine.ApplyEqualizer(true, -12, new float[10]);
    await engine.LoadAsync(new Uri(wave));
    engine.Play();
    var next = await Measure();
    Check(next / flat < 0.4, "equalization persists when the next track is loaded");
}
finally { engine.StopAudio(); File.Delete(wave); }
Console.WriteLine("All equalizer checks passed.");

async Task<double> Measure()
{
    await Task.Delay(700);
    lock (gate) { squareSum = 0; sampleCount = 0; }
    await Task.Delay(700);
    lock (gate)
    {
        if (sampleCount == 0) throw new Exception("No PCM received from VLC");
        return Math.Sqrt(squareSum / sampleCount);
    }
}
static void Check(bool passed, string description)
{
    if (!passed) throw new Exception("FAIL: " + description);
    Console.WriteLine("PASS: " + description);
}
sealed class MemoryStorage : IAppStorage
{
    public byte[]? Bytes { get; set; }
    public string RootDirectory => throw new NotSupportedException();
    public string GetLocalPath(string relativePath) => throw new NotSupportedException();
    public Task<byte[]?> ReadBytesAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult(Bytes);
    public Task WriteBytesAsync(string relativePath, byte[] content, CancellationToken cancellationToken = default) { Bytes = content; return Task.CompletedTask; }
    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) { Bytes = null; return Task.CompletedTask; }
}
