using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2.Avalonia.ViewModels;
using MusicPlayer2.Avalonia.Views;

using MusicPlayer2_Avalonia;

using MusicPlayer2_Avalonia.Application;
using MusicPlayer2_Avalonia.Infrastructure;

namespace MusicPlayer2.Avalonia;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();

        collection.AddApplicationServices();
        collection.AddInfrastructureServices("");
        collection.AddUIServices();

        _serviceProvider = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = CreateMainView;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = CreateMainView();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainView CreateMainView()
    {
        return new MainView
        {
            DataContext = new MainViewModel()
        };
    }
}
