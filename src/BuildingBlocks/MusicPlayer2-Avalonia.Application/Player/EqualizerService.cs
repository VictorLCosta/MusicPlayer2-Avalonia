using System.Text.Json;
using System.Text.Json.Serialization;
using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Application.Player;

public sealed record EqualizerSettings(bool Enabled, float Preamp, float[] Gains);

public sealed class EqualizerService(IAudioEngine engine, IAppStorage storage)
{
    private readonly object _gate = new();
    private Task? _initialization;
    private EqualizerSettings _current = new(false, 0, []);
    public bool IsSupported => engine is IAudioEqualizer;
    public IReadOnlyList<float> Frequencies => (engine as IAudioEqualizer)?.EqualizerFrequencies ?? [];
    public EqualizerSettings Current => _current with { Gains = _current.Gains.ToArray() };
    public string? LoadError { get; private set; }

    public Task InitializeAsync()
    {
        lock (_gate) return _initialization ??= LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!IsSupported) return;
        try
        {
            var bytes = await storage.ReadBytesAsync("equalizer.json").ConfigureAwait(false);
            var saved = bytes is null ? null : JsonSerializer.Deserialize(bytes, EqualizerJsonContext.Default.EqualizerSettings);
            Apply(saved ?? new(false, 0, new float[Frequencies.Count]));
        }
        catch (Exception)
        {
            LoadError = "Não foi possível restaurar o equalizador. Ajuste os controles e feche para salvar novamente.";
            _current = new(false, 0, new float[Frequencies.Count]);
            try { Apply(_current); }
            catch (Exception) { /* Keep playback available if the native filter cannot be configured. */ }
        }
    }

    public void Apply(EqualizerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (engine is not IAudioEqualizer control) throw new NotSupportedException("Equalizador indisponível neste motor de áudio.");
        if (settings.Gains is null || settings.Gains.Length != Frequencies.Count ||
            !float.IsFinite(settings.Preamp) || settings.Gains.Any(gain => !float.IsFinite(gain)))
            throw new ArgumentException("Configuração de equalizador inválida.", nameof(settings));
        var normalized = settings with
        {
            Preamp = Math.Clamp(settings.Preamp, -12, 12),
            Gains = settings.Gains.Select(gain => Math.Clamp(gain, -12, 12)).ToArray()
        };
        control.ApplyEqualizer(normalized.Enabled, normalized.Preamp, normalized.Gains);
        _current = normalized;
    }

    public Task SaveAsync() => storage.WriteBytesAsync("equalizer.json",
        JsonSerializer.SerializeToUtf8Bytes(Current, EqualizerJsonContext.Default.EqualizerSettings));
}

[JsonSerializable(typeof(EqualizerSettings))]
internal sealed partial class EqualizerJsonContext : JsonSerializerContext;
