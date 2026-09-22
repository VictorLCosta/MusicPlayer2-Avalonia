using Avalonia;
using Avalonia.Controls;

using MusicPlayer2_Avalonia.ViewModels.Settings;

namespace MusicPlayer2_Avalonia.Views.Settings;

public partial class PlaybackSettingsView : UserControl
{
    public PlaybackSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is PlaybackSettingsViewModel vm)
            vm.RefreshAudioDevices();
    }
}