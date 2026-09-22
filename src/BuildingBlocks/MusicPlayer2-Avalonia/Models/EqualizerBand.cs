using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer2_Avalonia.Models;

public sealed partial class EqualizerBand(
    double frequencyHz,
    string label,
    double gain = 0) : ObservableObject
{
    private double _gain = NormalizeGain(gain);

    [ObservableProperty]
    public partial double FrequencyHz { get; set; } = frequencyHz;

    [ObservableProperty]
    public partial string Label { get; set; } = label;

    public double Gain
    {
        get => _gain;
        set => SetProperty(ref _gain, NormalizeGain(value));
    }

    private static double NormalizeGain(double value)
    {
        return double.IsFinite(value)
            ? Math.Clamp(value, -12, 12)
            : throw new ArgumentOutOfRangeException(nameof(value));
    }
}