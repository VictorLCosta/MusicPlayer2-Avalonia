using Avalonia;



namespace MusicPlayer2.Avalonia.Desktop;

sealed class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<DesktopPlayerApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

}
