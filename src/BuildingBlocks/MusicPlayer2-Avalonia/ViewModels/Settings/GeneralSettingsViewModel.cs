using CommunityToolkit.Mvvm.ComponentModel;

using MusicPlayer2_Avalonia.Application.Settings.Models;

namespace MusicPlayer2_Avalonia.ViewModels.Settings;

internal sealed partial class GeneralSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial WindowCloseBehavior CloseBehavior { get; set; } = WindowCloseBehavior.Exit;
}