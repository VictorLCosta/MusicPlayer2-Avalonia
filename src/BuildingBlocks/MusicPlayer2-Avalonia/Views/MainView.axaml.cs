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
    private bool _showPlaylist = true;
    private bool? _wasNarrow;

    public MainView()
    {
        InitializeComponent();
        TimeSlider.AddHandler(InputElement.PointerPressedEvent, SeekToPointer,
            RoutingStrategies.Tunnel, handledEventsToo: true);
        DataContextChanged += (_, _) => UpdatePlayer();
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        UpdateResponsiveLayout();
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
        if (TopLevel.GetTopLevel(this)?.DataContext is MainWindowViewModel vm)
            vm.NavigateToSettings();
    }

    private void FocusSearch(object? sender, RoutedEventArgs e)
    {
        _showPlaylist = true;
        UpdateResponsiveLayout();
        PlaylistPanel.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()?.Focus();
    }

    private void TogglePlaylist(object? sender, RoutedEventArgs e)
    {
        _showPlaylist = !_showPlaylist;
        UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        var narrow = Bounds.Width > 0 && Bounds.Width < 660;
        if (_wasNarrow != narrow)
        {
            _showPlaylist = !narrow;
            _wasNarrow = narrow;
        }
        if (narrow)
        {
            ContentGrid.ColumnDefinitions = new ColumnDefinitions("*,0");
            Grid.SetColumn(PlaylistPanel, 0);
            PlayerPanel.IsVisible = !_showPlaylist;
            PlaylistPanel.IsVisible = _showPlaylist;
        }
        else
        {
            Grid.SetColumn(PlaylistPanel, 1);
            ContentGrid.ColumnDefinitions = _showPlaylist
                ? new ColumnDefinitions("*,*")
                : new ColumnDefinitions("*,0");
            PlayerPanel.IsVisible = true;
            PlaylistPanel.IsVisible = _showPlaylist;
        }
    }

    private void PlayTrackDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.PlaySelectedCommand.CanExecute(null))
            vm.PlaySelectedCommand.Execute(null);
    }

    private void SeekToPointer(object? sender, PointerPressedEventArgs e)
    {
        var thumb = TimeSlider.GetVisualDescendants().OfType<Thumb>().FirstOrDefault();
        if (!e.GetCurrentPoint(TimeSlider).Properties.IsLeftButtonPressed ||
            thumb?.IsPointerOver == true ||
            DataContext is not MainViewModel { Player.CurrentTrack: not null } ||
            TimeSlider.Maximum <= TimeSlider.Minimum)
            return;

        var thumbWidth = thumb?.Bounds.Width ?? 0;
        var travelWidth = TimeSlider.Bounds.Width - thumbWidth;
        if (travelWidth <= 0) return;

        var fraction = Math.Clamp(
            (e.GetPosition(TimeSlider).X - thumbWidth / 2) / travelWidth, 0, 1);
        TimeSlider.SetCurrentValue(RangeBase.ValueProperty,
            TimeSlider.Minimum + fraction * (TimeSlider.Maximum - TimeSlider.Minimum));
        e.Handled = true;
    }

    private async void ImportFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = "Selecionar pasta de músicas", AllowMultiple = false });
        if (folders.Count > 0 && folders[0].Path.IsFile)
            await vm.ImportFolderAsync(folders[0].Path.LocalPath);
    }
}