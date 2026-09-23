using System.ComponentModel;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

using MusicPlayer2_Avalonia.Application.Playlist.Models;
using MusicPlayer2_Avalonia.ViewModels;

namespace MusicPlayer2_Avalonia.Views;

public partial class MainView : UserControl
{
    private PlayerViewModel? _activePlayer;
    private bool _equalizerOpen;

    public MainView()
    {
        InitializeComponent();
        TimeSlider.AddHandler(InputElement.PointerPressedEvent, SeekToPointer,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        MobileTimeSlider.AddHandler(InputElement.PointerPressedEvent, SeekToPointer,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        DataContextChanged += (_, _) => UpdatePlayer();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        UpdatePlayer();

        if (DataContext is MainViewModel vm)
            _ = vm.LoadTracksAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_activePlayer is not null)
        {
            _activePlayer.PropertyChanged -= PlaybackChanged;
            _activePlayer.StopUpdating();
        }
        _activePlayer = null;

        base.OnDetachedFromVisualTree(e);
    }

    private void UpdatePlayer()
    {
        if (_activePlayer is not null) _activePlayer.PropertyChanged -= PlaybackChanged;
        _activePlayer?.StopUpdating();
        _activePlayer = TopLevel.GetTopLevel(this) is not null && DataContext is MainViewModel vm ? vm.Player : null;
        if (_activePlayer is not null) _activePlayer.PropertyChanged += PlaybackChanged;
        _activePlayer?.StartUpdating();
        EqualizerButton.IsEnabled = MobileEqualizerButton.IsEnabled =
            _activePlayer?.Equalizer.IsSupported == true;
        UpdateTrackIndicators();
    }

    private void TrackContainerPrepared(object? sender, ContainerPreparedEventArgs e) =>
        UpdateTrackIndicator(e.Container, e.Index);

    private void TrackContainerIndexChanged(object? sender, ContainerIndexChangedEventArgs e) =>
        UpdateTrackIndicator(e.Container, e.NewIndex);

    private void PlaybackChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PlayerViewModel.CurrentTrack) or nameof(PlayerViewModel.IsPlaying))
            UpdateTrackIndicators();
    }

    private void UpdateTrackIndicators()
    {
        foreach (var container in TrackList.GetRealizedContainers())
            UpdateTrackIndicator(container, TrackList.IndexFromContainer(container));
    }

    private void UpdateTrackIndicator(Control container, int index)
    {
        var current = container.DataContext is ListTrackDto track && track.TrackId == _activePlayer?.CurrentTrack?.TrackId;
        container.Classes.Set("current-track", current);
        container.SetValue(TagProperty, current ? (_activePlayer!.IsPlaying ? "▥" : "Ⅱ") : (object)(index + 1));
    }

    private void OpenSettings(object? sender, RoutedEventArgs e)
    {
        if (MobileShell.NavigationFor(this) is { } vm)
            vm.NavigateToSettings();
    }

    private async void OpenEqualizer(object? sender, RoutedEventArgs e)
    {
        if (_equalizerOpen || DataContext is not MainViewModel vm ||
            !vm.Player.Equalizer.IsSupported) return;
        _equalizerOpen = true;
        using var equalizer = new EqualizerViewModel(vm.Player.Equalizer);
        try
        {
            await equalizer.InitializeAsync();
            if (TopLevel.GetTopLevel(this) is Window owner)
                await new EqualizerDialog { DataContext = equalizer }.ShowDialog(owner);
            else if (this.GetVisualAncestors().OfType<MobileShell>().FirstOrDefault() is { } shell)
                await shell.ShowEqualizerAsync(equalizer);
        }
        finally { _equalizerOpen = false; }
    }

    private void FocusSearch(object? sender, RoutedEventArgs e)
    {
        SetPlaylistOpen(true);

        PlaylistPanel.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()?.Focus();
    }

    private void TogglePlaylist(object? sender, RoutedEventArgs e)
    {
        SetPlaylistOpen(!PlaylistPanel.IsVisible);
    }

    private void SetPlaylistOpen(bool open)
    {
        PlayerLayout.Classes.Set("playlist-open", open);
        PlayerLayout.Classes.Set("playlist-closed", !open);
    }

    private void PlayTrackDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.PlaySelectedCommand.CanExecute(null))
            vm.PlaySelectedCommand.Execute(null);
    }

    private void SeekToPointer(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Slider slider) return;
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().FirstOrDefault();
        if (!e.GetCurrentPoint(slider).Properties.IsLeftButtonPressed ||
            thumb?.IsPointerOver == true ||
            DataContext is not MainViewModel { Player.CurrentTrack: not null } ||
            slider.Maximum <= slider.Minimum)
            return;

        var thumbWidth = thumb?.Bounds.Width ?? 0;
        var travelWidth = slider.Bounds.Width - thumbWidth;
        if (travelWidth <= 0) return;

        var fraction = Math.Clamp(
            (e.GetPosition(slider).X - thumbWidth / 2) / travelWidth, 0, 1);
        slider.SetCurrentValue(RangeBase.ValueProperty,
            slider.Minimum + fraction * (slider.Maximum - slider.Minimum));
        e.Handled = true;
    }

    private async void ImportFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.IsImporting || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        vm.IsImporting = true;
        try
        {
            if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS() || !topLevel.StorageProvider.CanPickFolder)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Strings.Get("ImportTitle"), AllowMultiple = true,
                    FileTypeFilter = [new FilePickerFileType(Strings.Get("Audio"))
                    {
                        MimeTypes = ["audio/*"], AppleUniformTypeIdentifiers = ["public.audio"],
                        Patterns = ["*.mp3", "*.m4a", "*.aac", "*.flac", "*.wav", "*.ogg", "*.opus", "*.aiff"]
                    }]
                });
                var errors = new List<string>();
                foreach (var file in files)
                {
                    using (file)
                    {
                        try
                        {
                            await using var stream = await file.OpenReadAsync();
                            await vm.ImportFileAsync(file.Name, stream);
                        }
                        catch (Exception ex) { errors.Add(file.Name + ": " + ex.Message); }
                    }
                }
                if (errors.Count > 0) vm.ReportImportError(string.Join(Environment.NewLine, errors));
            }
            else
            {
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = Strings.Get("SelectMusicFolder"), AllowMultiple = false });
                foreach (var folder in folders)
                    using (folder)
                        if (folder.TryGetLocalPath() is { } path) await vm.ImportFolderAsync(path);
                        else vm.ReportImportError(Strings.Get("SelectLocalFolder"));
            }
        }
        catch (Exception ex) { vm.ReportImportError(Strings.Get("ImportFailed") + ex.Message); }
        finally { vm.IsImporting = false; }
    }
}
