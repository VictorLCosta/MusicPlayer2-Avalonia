using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Models;

namespace MusicPlayer2_Avalonia.ViewModels;

internal sealed partial class EqualizerViewModel(EqualizerService service) : ViewModelBase
{
    private bool _updating;
    public IReadOnlyList<EqualizerBand> Bands { get; private set; } = [];
    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial double Preamp { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; private set; }
    [ObservableProperty] public partial bool IsSaving { get; private set; }

    public async Task InitializeAsync()
    {
        await service.InitializeAsync();
        _updating = true;
        var saved = service.Current;
        Bands = service.Frequencies.Select((frequency, index) => new EqualizerBand(frequency,
            frequency >= 1000 ? (frequency / 1000).ToString("0.##", CultureInfo.InvariantCulture) + " kHz"
                : frequency.ToString("0", CultureInfo.InvariantCulture) + " Hz", saved.Gains[index])).ToArray();
        foreach (var band in Bands) band.PropertyChanged += BandChanged;
        IsEnabled = saved.Enabled;
        Preamp = saved.Preamp;
        ErrorMessage = service.LoadError is null ? null : Strings.Get("EqualizerRestoreFailed");
        _updating = false;
    }

    partial void OnIsEnabledChanged(bool value) => Apply();
    partial void OnPreampChanged(double value) => Apply();
    private void BandChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EqualizerBand.Gain)) Apply();
    }

    private bool Apply()
    {
        if (_updating) return true;
        try
        {
            service.Apply(new(IsEnabled, (float)Preamp, Bands.Select(band => (float)band.Gain).ToArray()));
            ErrorMessage = null;
            return true;
        }
        catch (Exception)
        {
            ErrorMessage = Strings.Get("EqualizerApplyFailed");
            return false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        _updating = true;
        foreach (var band in Bands) band.Gain = 0;
        Preamp = 0;
        _updating = false;
        Apply();
    }

    public async Task<bool> SaveAsync()
    {
        if (!Apply()) return false;
        IsSaving = true;
        try
        {
            await service.SaveAsync();
            return true;
        }
        catch (Exception)
        {
            ErrorMessage = Strings.Get("EqualizerSaveFailed");
            return false;
        }
        finally { IsSaving = false; }
    }

    public override void Dispose()
    {
        foreach (var band in Bands) band.PropertyChanged -= BandChanged;
        base.Dispose();
    }
}
