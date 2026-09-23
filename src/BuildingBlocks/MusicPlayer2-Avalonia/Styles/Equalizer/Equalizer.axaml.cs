using Avalonia;
using Avalonia.Controls;

using MusicPlayer2_Avalonia.Models;

namespace MusicPlayer2_Avalonia.Styles.Equalizer;

public partial class Equalizer : UserControl
{
    public static readonly StyledProperty<IReadOnlyList<EqualizerBand>?> BandsProperty =
        AvaloniaProperty.Register<Equalizer, IReadOnlyList<EqualizerBand>?>(nameof(Bands), defaultValue: null);

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
