using CommunityToolkit.Mvvm.ComponentModel;

using MusicPlayer2_Avalonia.Application.Settings.Models;

namespace MusicPlayer2_Avalonia.ViewModels.Settings;

internal sealed partial class AppearanceSettingsViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>Optional accent color in #RRGGBB format; null uses the theme default.</summary>
    [ObservableProperty]
    public partial string? AccentColor { get; set; }

    [ObservableProperty]
    public partial bool ShowAlbumCover { get; set; } = true;
}