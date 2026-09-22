using MusicPlayer2_Avalonia.ViewModels;

namespace MusicPlayer2_Avalonia.Models;

internal sealed record MenuItem(string Title, string Icon, Func<ViewModelBase> CreatePage);