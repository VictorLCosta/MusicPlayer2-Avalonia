using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MusicPlayer2_Avalonia.ViewModels;
using MusicPlayer2_Avalonia.ViewModels.Settings;
using MusicPlayer2_Avalonia.Views;
using MusicPlayer2_Avalonia.Views.Settings;

namespace MusicPlayer2_Avalonia;

// Explicit references survive mobile linking/AOT; views are recreated while DI owns the models.
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        Control? view = param switch
        {
            MainViewModel => new MainView(),
            SettingsViewModel => new SettingsView(),
            GeneralSettingsViewModel => new GeneralSettingsView(),
            PlaybackSettingsViewModel => new PlaybackSettingsView(),
            MediaLibrarySettingsViewModel => new MediaLibrarySettingsView(),
            AppearanceSettingsViewModel => new AppearanceSettingsView(),
            null => null,
            _ => new TextBlock { Text = "View not found: " + param.GetType().Name }
        };
        if (view is not null) view.DataContext = param;
        return view;
    }
    public bool Match(object? data) => data is ViewModelBase;
}
