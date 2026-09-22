using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using MusicPlayer2_Avalonia.ViewModels;

namespace MusicPlayer2_Avalonia.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => LoadSettings();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (TopLevel.GetTopLevel(this) is not null && DataContext is SettingsViewModel vm)
            _ = vm.LoadSettingsAsync();
    }

    private void BackToPlayerClicked(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.DataContext is MainWindowViewModel vm)
            vm.NavigateToMain();
    }
}