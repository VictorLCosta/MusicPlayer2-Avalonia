using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

using Microsoft.Extensions.DependencyInjection;

using MusicPlayer2_Avalonia.Application;
using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Application.Settings;
using MusicPlayer2_Avalonia.Application.Settings.Models;
using MusicPlayer2_Avalonia.Infrastructure;
using MusicPlayer2_Avalonia.ViewModels;
using MusicPlayer2_Avalonia.Views;

using AvaloniaApplication = Avalonia.Application;

namespace MusicPlayer2_Avalonia;

public partial class App : AvaloniaApplication
{
    private IServiceProvider _serviceProvider = null!;
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly IServiceProvider? _providedServices;
    private SettingsService? _settings;
    private MobileShell? _mobileShell;

    public IServiceProvider Services => _serviceProvider;
    public static bool IsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
    public static bool IsDesktop => !IsMobile && !OperatingSystem.IsBrowser();

    protected virtual void ConfigurePlatformServices(IServiceCollection services) { }

    public bool NavigateBack() => _mobileShell?.NavigateBack() == true;

    public async Task SavePlaybackAsync()
    {
        try { await _serviceProvider.GetRequiredService<PlayerService>().SaveSessionAsync(); }
        catch (Exception) { /* A lifecycle callback must not crash the host on storage failure. */ }
    }

    public App()
    {
    }

    public App(Action<IServiceCollection> configureServices)
    {
        _configureServices = configureServices;
    }

    public App(IServiceProvider services)
    {
        _providedServices = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (_providedServices is not null)
        {
            _serviceProvider = _providedServices;
        }
        else
        {
            var collection = new ServiceCollection();

            collection.AddUIServices();
            collection.AddApplicationServices();
            collection.AddInfrastructureServices();
            ConfigurePlatformServices(collection);

            _configureServices?.Invoke(collection);

            _serviceProvider = collection.BuildServiceProvider();

            if (!Avalonia.Controls.Design.IsDesignMode)
                _serviceProvider.InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            _ = _serviceProvider.GetRequiredService<AudioOutputService>().InitializeAsync();
            _settings = _serviceProvider.GetRequiredService<SettingsService>();
            _settings.Changed += SettingsChanged;
            _ = InitializeAppearanceAsync();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>(),
                StateStorage = _serviceProvider.GetRequiredService<IAppStorage>(),
                SaveWindowState = true,
                Settings = _settings,
                Player = _serviceProvider.GetRequiredService<PlayerService>(),
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = CreateMobileShell();
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = CreateMobileShell;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MobileShell CreateMobileShell() => _mobileShell = new MobileShell
    {
        DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>()
    };

    private async Task InitializeAppearanceAsync()
    {
        try
        {
            if (_settings is not null) await _settings.LoadAsync();
            ApplyAppearance();
        }
        catch (Exception)
        {
            // The settings page and library display load failures; keep the default appearance usable.
            RequestedThemeVariant = ThemeVariant.Default;
        }
    }

    private void SettingsChanged(object? sender, EventArgs e) => Dispatcher.UIThread.Post(ApplyAppearance);

    private void ApplyAppearance()
    {
        var settings = _settings?.Current ?? new AppSettings();
        RequestedThemeVariant = settings.Theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        // Only an explicit user accent overrides the HEX resources in the theme.
        Resources.Remove("ThemeAccentColor");
        Resources.Remove("ThemeAccentBrush");
        Resources.Remove("Theme.Brush.Accent");
        if (settings.AccentColor is { } text && Color.TryParse(text, out var accent))
        {
            Resources["ThemeAccentColor"] = accent;
            Resources["ThemeAccentBrush"] = new SolidColorBrush(accent);
            Resources["Theme.Brush.Accent"] = new SolidColorBrush(accent);
        }
    }
}
