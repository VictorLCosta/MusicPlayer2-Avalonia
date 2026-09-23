namespace MusicPlayer2_Avalonia.Application.Common;

public interface IAudioEqualizer
{
    IReadOnlyList<float> EqualizerFrequencies { get; }
    void ApplyEqualizer(bool enabled, float preamp, IReadOnlyList<float> gains);
}
