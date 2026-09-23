using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DynamicData;
using DynamicData.Binding;

using MusicPlayer2_Avalonia.Application.Library;
using MusicPlayer2_Avalonia.Application.Playlist.Models;
using MusicPlayer2_Avalonia.Application.Settings;
using MusicPlayer2_Avalonia.Application.Settings.Models;
using MusicPlayer2_Avalonia.Models;

namespace MusicPlayer2_Avalonia.ViewModels;

internal sealed partial class MainViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _libraryScopes;
    private readonly ManagedAudioImporter _importer;
    private readonly SettingsService _settings;
    private readonly LibraryMaintenanceService _maintenance;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private AppSettings? _appliedLibrarySettings;
    private bool _startupUpdated;

    public PlayerViewModel Player { get; }

    private readonly SourceList<ListTrackDto> _source = new();
    private readonly ReadOnlyObservableCollection<ListTrackDto> _filtered;
    private readonly IDisposable _tracksSubscription;

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    public ReadOnlyObservableCollection<ListTrackDto> FilteredTracks => _filtered;

    [ObservableProperty]
    public partial string? LibraryError { get; private set; }

    [ObservableProperty]
    public partial bool HasTracks { get; private set; }
    [ObservableProperty]
    public partial bool IsImporting { get; set; }

    private bool _loaded;

    [ObservableProperty]
    public partial ListTrackDto? SelectedTrack { get; set; }

    public ObservableCollection<LibraryNode> LibraryNodes { get; } =
    [
        new() { Name = Strings.Get("AllTracks"), Kind = LibraryNodeKind.AllTracks },
        new() { Name = Strings.Get("Folders"), Kind = LibraryNodeKind.Group }
    ];

    [ObservableProperty]
    public partial LibraryNode? SelectedNode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrackCountText))]
    public partial int TrackCount { get; private set; }

    public string TrackCountText => Strings.Format("TrackCount", TrackCount);

    private void LanguageChanged(object? sender, EventArgs e)
    {
        LibraryNodes[0].Name = Strings.Get("AllTracks");
        LibraryNodes[1].Name = Strings.Get("Folders");
        OnPropertyChanged(nameof(TrackCountText));
    }

    [ObservableProperty]
    public partial TimeSpan TotalDuration { get; private set; }

    [ObservableProperty]
    public partial long TotalSizeBytes { get; private set; }

    public MainViewModel(IServiceScopeFactory libraryScopes, PlayerViewModel player,
        SettingsService settings, LibraryMaintenanceService maintenance, ManagedAudioImporter importer)
    {
        _importer = importer;
        _libraryScopes = libraryScopes;
        Player = player;
        _settings = settings;
        _maintenance = maintenance;
        _settings.Changed += SettingsChanged;
        Strings.Changed += LanguageChanged;

        SelectedNode = LibraryNodes[0];

        var filterPredicate = this.WhenPropertyChanged(x => x.SearchText)
            .Select(x => CreateFilter(x.Value));

        _tracksSubscription = _source.Connect()
            .Filter(filterPredicate)
            .Sort(SortExpressionComparer<ListTrackDto>.Ascending(x => x.Title))
            .Bind(out _filtered)
            .Subscribe(_ => UpdateTotals());
    }

    partial void OnSelectedTrackChanged(ListTrackDto? value)
    {
        _ = PlaySelectedAsync();
    }

    [RelayCommand]
    private async Task PlaySelectedAsync()
    {
        if (SelectedTrack is { } track)
            await Player.PlayTrackAsync(track, FilteredTracks);
    }

    public async Task LoadTracksAsync()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            var settings = await _settings.LoadAsync();
            UpdateLibraryFolders(settings);
            string? warning = null;
            if (!_startupUpdated)
            {
                _startupUpdated = true;
                if (settings.UpdateLibraryOnStartup)
                    warning = await _maintenance.UpdateAsync(settings);
                await Player.InitializeAsync();
            }
            await using var scope = _libraryScopes.CreateAsyncScope();
            var tracks = await scope.ServiceProvider.GetRequiredService<LibraryService>().GetTracksAsync();
            _source.Edit(list =>
            {
                list.Clear();
                list.AddRange(tracks);
            });
            LibraryError = warning;
        }
        catch (Exception ex)
        {
            LibraryError = ex.Message;
            _loaded = false;
        }
    }

    public async Task ImportFolderAsync(string path)
    {
        try
        {
            await using var scope = _libraryScopes.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LibraryService>().ScanDirectoryAsync(path);
            _loaded = false;
            await LoadTracksAsync();
        }
        catch (Exception ex)
        {
            LibraryError = ex.Message;
        }
    }

    public async Task ImportFileAsync(string name, Stream source)
    {
        var path = await _importer.ImportAsync(name, source);
        await using var scope = _libraryScopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LibraryService>().ScanLibrary(path);
        _loaded = false;
        await LoadTracksAsync();
    }

    public void ReportImportError(string message) => LibraryError = message;

    private void UpdateLibraryFolders(AppSettings settings)
    {
        LibraryNodes[1].Children.Clear();
        foreach (var folder in settings.LibraryFolders)
            LibraryNodes[1].Children.Add(new LibraryNode
            {
                Name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)),
                Kind = LibraryNodeKind.Folder,
                FolderPath = folder
            });
        _appliedLibrarySettings = settings;
    }

    private void SettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.UIThread.Post(() => { if (_startupUpdated) _ = RefreshLibrarySettingsAsync(); });

    private async Task RefreshLibrarySettingsAsync()
    {
        await _refreshGate.WaitAsync();
        try
        {
            var settings = _settings.Current;
            var needsUpdate = _appliedLibrarySettings is null ||
                !_appliedLibrarySettings.LibraryFolders.SequenceEqual(settings.LibraryFolders) ||
                (!_appliedLibrarySettings.RemoveMissingFilesFromLibrary && settings.RemoveMissingFilesFromLibrary);
            UpdateLibraryFolders(settings);
            if (!needsUpdate) return;
            var warning = await _maintenance.UpdateAsync(settings);
            await using var scope = _libraryScopes.CreateAsyncScope();
            var tracks = await scope.ServiceProvider.GetRequiredService<LibraryService>().GetTracksAsync();
            _source.Edit(list => { list.Clear(); list.AddRange(tracks); });
            LibraryError = warning;
        }
        catch (Exception ex) { LibraryError = ex.Message; }
        finally { _refreshGate.Release(); }
    }

    private void UpdateTotals()
    {
        TrackCount = _filtered.Count;
        HasTracks = TrackCount > 0;
        TotalDuration = TimeSpan.FromTicks(_filtered.Sum(track => track.Duration.Ticks));
        TotalSizeBytes = _filtered.Sum(track => track.FileSizeBytes);
    }

    private static Func<ListTrackDto, bool> CreateFilter(string? searchText)
    {
        if (string.IsNullOrEmpty(searchText))
            return _ => true;

        return person =>
            person.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    public override void Dispose()
    {
        _settings.Changed -= SettingsChanged;
        Strings.Changed -= LanguageChanged;
        base.Dispose();

        _tracksSubscription.Dispose();
        _source.Dispose();
        _refreshGate.Dispose();
    }
}
