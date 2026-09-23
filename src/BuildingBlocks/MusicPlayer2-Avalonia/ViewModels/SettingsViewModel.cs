using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Application.Settings;
using MusicPlayer2_Avalonia.Application.Settings.Models;
using MusicPlayer2_Avalonia.Models;
using MusicPlayer2_Avalonia.ViewModels.Settings;

namespace MusicPlayer2_Avalonia.ViewModels;

internal sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private AppSettings? _savedSettings;
    private readonly AudioOutputService _audioOutput;

    public SettingsViewModel(
        AppearanceSettingsViewModel appearance,
        GeneralSettingsViewModel general,
        PlaybackSettingsViewModel playback,
        MediaLibrarySettingsViewModel mediaLibrary,
        SettingsService settingsService,
        AudioOutputService audioOutput)
    {
        _settingsService = settingsService;
        _audioOutput = audioOutput;

        Appearance = appearance;
        General = general;
        Playback = playback;
        MediaLibrary = mediaLibrary;

        SettingsMenuItems = [
            new(Strings.Get("General"), "Settings", () => General),
            new(Strings.Get("Playback"), "Play", () => Playback),
            new(Strings.Get("MediaLibrary"), "Library", () => MediaLibrary),
            new(Strings.Get("Appearance"), "Palette", () => Appearance)
        ];

        SelectedMenuItem = SettingsMenuItems[0];
        Strings.Changed += LanguageChanged;
    }

    public AppearanceSettingsViewModel Appearance { get; }
    public GeneralSettingsViewModel General { get; }
    public PlaybackSettingsViewModel Playback { get; }
    public MediaLibrarySettingsViewModel MediaLibrary { get; }

    public IReadOnlyList<MenuItem> SettingsMenuItems { get; private set; }

    private void LanguageChanged(object? sender, EventArgs e)
    {
        var selected = SelectedMenuItem?.Icon;
        SettingsMenuItems = [
            new(Strings.Get("General"), "Settings", () => General),
            new(Strings.Get("Playback"), "Play", () => Playback),
            new(Strings.Get("MediaLibrary"), "Library", () => MediaLibrary),
            new(Strings.Get("Appearance"), "Palette", () => Appearance)
        ];
        OnPropertyChanged(nameof(SettingsMenuItems));
        SelectedMenuItem = SettingsMenuItems.FirstOrDefault(item => item.Icon == selected) ?? SettingsMenuItems[0];
        Playback.RefreshAudioDevices();
    }

    public override void Dispose()
    {
        Strings.Changed -= LanguageChanged;
        base.Dispose();
    }

    public ViewModelBase? CurrentPage => SelectedMenuItem?.CreatePage();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentPage))]
    public partial MenuItem? SelectedMenuItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(LoadSettingsCommand), nameof(SaveSettingsCommand), nameof(CancelSettingsCommand))]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyCanExecuteChangedFor(nameof(LoadSettingsCommand), nameof(SaveSettingsCommand), nameof(CancelSettingsCommand))]
    public partial bool IsLoaded { get; private set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; private set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; private set; }

    public bool CanEdit => IsLoaded && !IsBusy;

    internal void UpdateTheme(AppTheme theme)
    {
        Appearance.Theme = theme;
        if (_savedSettings is not null)
            _savedSettings = _savedSettings with { Theme = theme };
    }

    private bool CanLoad => !IsLoaded && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    public async Task LoadSettingsAsync()
    {
        if (!CanLoad) return;

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            await _audioOutput.InitializeAsync();
            var settings = await _settingsService.LoadAsync();
            RestoreSettings(settings);
            Playback.RefreshAudioDevices();
            _savedSettings = settings;
            IsLoaded = true;
        }
        catch (Exception)
        {
            ErrorMessage = Strings.Get("SettingsLoadFailed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private async Task SaveSettingsAsync()
    {
        if (!CanEdit || _savedSettings is null) return;

        ErrorMessage = null;
        StatusMessage = null;
        var accentColor = NormalizeOptionalText(Appearance.AccentColor);
        if (accentColor is not null && (accentColor.Length != 7 || accentColor[0] != '#' ||
            !accentColor.Skip(1).All(char.IsAsciiHexDigit)))
        {
            ErrorMessage = Strings.Get("InvalidAccent");
            return;
        }

        var settings = _savedSettings with
        {
            CloseBehavior = General.CloseBehavior,
            Language = General.Language == "system" ? null : General.Language,
            AudioOutputDeviceId = NormalizeOptionalText(Playback.AudioOutputDeviceId),
            RememberPlaybackPosition = Playback.RememberPlaybackPosition,
            ContinuePlaybackOnPlaylistChange = Playback.ContinuePlaybackOnPlaylistChange,
            LibraryFolders = MediaLibrary.LibraryFolders.ToArray(),
            UpdateLibraryOnStartup = MediaLibrary.UpdateLibraryOnStartup,
            RemoveMissingFilesFromLibrary = MediaLibrary.RemoveMissingFilesFromLibrary,
            Theme = Appearance.Theme,
            AccentColor = accentColor,
            ShowAlbumCover = Appearance.ShowAlbumCover
        };

        IsBusy = true;
        try
        {
            await _settingsService.SaveAsync(settings);
            Strings.Apply(settings.Language);
            _savedSettings = settings;
            RestoreSettings(settings);
            StatusMessage = Strings.Get("SettingsSaved");
            try
            {
                Playback.ApplyAudioOutput();
            }
            catch (Exception)
            {
                ErrorMessage = Strings.Get("OutputApplyFailed");
            }
        }
        catch (Exception)
        {
            ErrorMessage = Strings.Get("SettingsSaveFailed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void CancelSettings()
    {
        if (!CanEdit || _savedSettings is null) return;

        RestoreSettings(_savedSettings);
        ErrorMessage = null;
        StatusMessage = Strings.Get("ChangesDiscarded");
    }

    private void RestoreSettings(AppSettings settings)
    {
        General.CloseBehavior = settings.CloseBehavior;
        General.Language = settings.Language is null ? "system" : Strings.ResolveCulture(settings.Language).Name;
        Playback.AudioOutputDeviceId = settings.AudioOutputDeviceId;
        Playback.RememberPlaybackPosition = settings.RememberPlaybackPosition;
        Playback.ContinuePlaybackOnPlaylistChange = settings.ContinuePlaybackOnPlaylistChange;
        MediaLibrary.LibraryFolders.Clear();
        foreach (var folder in settings.LibraryFolders)
            MediaLibrary.LibraryFolders.Add(folder);
        MediaLibrary.UpdateLibraryOnStartup = settings.UpdateLibraryOnStartup;
        MediaLibrary.RemoveMissingFilesFromLibrary = settings.RemoveMissingFilesFromLibrary;
        Appearance.Theme = settings.Theme;
        Appearance.AccentColor = settings.AccentColor;
        Appearance.ShowAlbumCover = settings.ShowAlbumCover;
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
