using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MusicPlayer2_Avalonia.ViewModels;

internal sealed partial class MainWindowViewModel(
    MainViewModel mainViewModel,
    SettingsViewModel settings) : ViewModelBase
{
    public MainViewModel Main => mainViewModel;
    public SettingsViewModel Settings => settings;

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; } = mainViewModel;

    [RelayCommand]
    public void NavigateToSettings() =>
        CurrentPage = Settings;

    [RelayCommand]
    public void NavigateToMain() =>
        CurrentPage = Main;

    public override void Dispose()
    {
        CurrentPage.Dispose();

        base.Dispose();
    }
}