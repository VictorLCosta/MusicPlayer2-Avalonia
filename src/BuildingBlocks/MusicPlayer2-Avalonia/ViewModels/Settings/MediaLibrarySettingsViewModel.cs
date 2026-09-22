using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer2_Avalonia.ViewModels.Settings;

internal sealed partial class MediaLibrarySettingsViewModel : ViewModelBase
{
    public ObservableCollection<string> LibraryFolders { get; } = [];

    [ObservableProperty]
    public partial bool UpdateLibraryOnStartup { get; set; } = true;

    /// <summary>Removes library entries for missing files; does not delete files from disk.</summary>
    [ObservableProperty]
    public partial bool RemoveMissingFilesFromLibrary { get; set; } = true;
}