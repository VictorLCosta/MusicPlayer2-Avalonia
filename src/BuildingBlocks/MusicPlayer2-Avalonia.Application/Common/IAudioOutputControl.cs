namespace MusicPlayer2_Avalonia.Application.Common;

/// <summary>Optional output selection capability, implemented by supported audio engines.</summary>
public interface IAudioOutputControl
{
    IReadOnlyList<AudioOutputDevice> GetAudioOutputDevices();
    /// <summary>Selects an output, or the system default for null. Returns a fallback warning if needed.</summary>
    string? SelectAudioOutputDevice(string? deviceId);
}