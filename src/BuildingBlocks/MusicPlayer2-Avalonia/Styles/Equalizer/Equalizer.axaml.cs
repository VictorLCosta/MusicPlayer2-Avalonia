using Avalonia;
using Avalonia.Controls;

using MusicPlayer2_Avalonia.Models;

namespace MusicPlayer2_Avalonia.Styles.Equalizer;

public partial class Equalizer : UserControl
{
    private static readonly IReadOnlyList<EqualizerBand> initialValues = [
        new(60,    "60 Hz",   gain: 3),
        new(230,   "230 Hz",  gain: 1),
        new(910,   "910 Hz"),
        new(3600,  "3.6 kHz", gain: 2),
        new(14000, "14 kHz",  gain: 4)
    ];

    public static readonly StyledProperty<IReadOnlyList<EqualizerBand>?> BandsProperty =
        AvaloniaProperty.Register<Equalizer, IReadOnlyList<EqualizerBand>?>(nameof(Bands), defaultValue: initialValues);

    public IReadOnlyList<EqualizerBand>? Bands
    {
        get => GetValue(BandsProperty);
        set => SetValue(BandsProperty, value);
    }

    public Equalizer()
    {
        InitializeComponent();
    }
}