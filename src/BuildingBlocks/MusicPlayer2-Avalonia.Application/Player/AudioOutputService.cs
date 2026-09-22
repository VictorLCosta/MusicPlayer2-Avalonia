using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Settings;

namespace MusicPlayer2_Avalonia.Application.Player;

public sealed class AudioOutputService(IAudioEngine engine, SettingsService settingsService)
{
    private readonly object _initializationLock = new();
    private Task? _initialization;

    public bool IsSupported => engine is IAudioOutputControl;
    public string? StatusMessage { get; private set; }

    public IReadOnlyList<AudioOutputDevice> GetDevices() =>
        engine is IAudioOutputControl control ? control.GetAudioOutputDevices() : [];

    public Task InitializeAsync()
    {
        lock (_initializationLock)
            return _initialization ??= InitializeCoreAsync();
    }

    private async Task InitializeCoreAsync()
    {
        if (!IsSupported) return;
        try
        {
            var settings = await settingsService.LoadAsync().ConfigureAwait(false);
            Apply(settings.AudioOutputDeviceId);
        }
        catch (Exception)
        {
            StatusMessage = "Não foi possível carregar a saída salva. O player usará a saída padrão.";
        }
    }

    public void Apply(string? deviceId)
    {
        StatusMessage = engine is IAudioOutputControl control
            ? control.SelectAudioOutputDevice(deviceId)
            : "A seleção de saída é controlada pelo sistema nesta plataforma.";
    }
}