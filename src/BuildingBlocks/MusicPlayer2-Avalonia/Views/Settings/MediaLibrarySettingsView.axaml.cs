using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using MusicPlayer2_Avalonia.ViewModels.Settings;

namespace MusicPlayer2_Avalonia.Views.Settings;

public partial class MediaLibrarySettingsView : UserControl
{
    public MediaLibrarySettingsView()
    {
        InitializeComponent();
    }

    private async void AddFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MediaLibrarySettingsViewModel vm ||
            TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        FolderError.IsVisible = false;
        try
        {
            if (!topLevel.StorageProvider.CanPickFolder)
            {
                FolderError.Text = Strings.Get("FolderPickerUnavailable");
                FolderError.IsVisible = true;
                return;
            }

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = Strings.Get("SelectMusicFolders"), AllowMultiple = true });
            foreach (var folder in folders)
            {
                using (folder)
                {
                    var path = folder.TryGetLocalPath();
                    if (path is null)
                    {
                        FolderError.Text = Strings.Get("SelectLocalFolder");
                        FolderError.IsVisible = true;
                        continue;
                    }

                    var comparison = OperatingSystem.IsWindows()
                        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    if (!vm.LibraryFolders.Any(existing => string.Equals(existing, path, comparison)))
                        vm.LibraryFolders.Add(path);
                }
            }
        }
        catch (Exception)
        {
            FolderError.Text = Strings.Get("FolderOpenFailed");
            FolderError.IsVisible = true;
        }
    }

    private void RemoveFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MediaLibrarySettingsViewModel vm && FoldersList.SelectedItem is string folder)
            vm.LibraryFolders.Remove(folder);
    }
}
