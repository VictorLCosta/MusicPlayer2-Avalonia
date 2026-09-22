using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace MusicPlayer2_Avalonia.Extensions;

public static class ApplicationExtensions
{
    public static TopLevel? GetTopLevel(this Avalonia.Application? app)
    {
        if (app is null) return null;

        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) return desktop.MainWindow;
        if (app.ApplicationLifetime is ISingleViewApplicationLifetime viewApp && viewApp.MainView is { } mainView)
        {
            return TopLevel.GetTopLevel(mainView);
        }

        return null;
    }

}