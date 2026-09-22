namespace MusicPlayer2_Avalonia.Application.Settings.Models;

public sealed record AppSettings
{
    /// <summary>Null selects the system default audio output device.</summary>
    public string? AudioOutputDeviceId { get; init; }

    public bool RememberPlaybackPosition { get; init; } = true;

    public WindowCloseBehavior CloseBehavior { get; init; } = WindowCloseBehavior.Exit;

    public bool ContinuePlaybackOnPlaylistChange { get; init; } = true;

    public IReadOnlyList<string> LibraryFolders { get; init; } = [];

    public bool UpdateLibraryOnStartup { get; init; } = true;

    /// <summary>Removes library entries for missing files; does not delete files from disk.</summary>
    public bool RemoveMissingFilesFromLibrary { get; init; } = true;

    public AppTheme Theme { get; init; } = AppTheme.System;

    /// <summary>Optional accent color in #RRGGBB format; null uses the theme default.</summary>
    public string? AccentColor { get; init; }

    public bool ShowAlbumCover { get; init; } = true;
}