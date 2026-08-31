using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer2.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";
}
