using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Application.Settings;
using MusicPlayer2_Avalonia.Application.Settings.Models;
using MusicPlayer2_Avalonia.Styles.Window;
using MusicPlayer2_Avalonia.ViewModels;

namespace MusicPlayer2_Avalonia.Views;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The tray icon is disposed in OnClosed, following the window lifecycle.")]
public partial class MainWindow : CustomWindow
{
    private TrayIcon? _trayIcon;
    private bool _forceExit;
    private bool _exitApproved;
    private bool _savingBeforeExit;
    public SettingsService? Settings { get; set; }
    public PlayerService? Player { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        Strings.Changed += LanguageChanged;
        ActualThemeVariantChanged += (_, _) => UpdateThemeIcon();
        UpdateThemeIcon();
    }

    private void LanguageChanged(object? sender, EventArgs e)
    {
        UpdateThemeIcon();
        if (_trayIcon?.Menu is { } menu)
        {
            if (menu.Items[0] is NativeMenuItem show) show.Header = Strings.Get("OpenPlayer");
            if (menu.Items[2] is NativeMenuItem exit) exit.Header = Strings.Get("Exit");
        }
    }

    private void UpdateThemeIcon()
    {
        var isLight = ActualThemeVariant == ThemeVariant.Light;
        LightThemeIcon.IsVisible = !isLight;
        DarkThemeIcon.IsVisible = isLight;
        var label = Strings.Get(isLight ? "EnableDarkTheme" : "EnableLightTheme");
        ToolTip.SetTip(ThemeToggleButton, label);
        Avalonia.Automation.AutomationProperties.SetName(ThemeToggleButton, label);
    }

    private async void ToggleThemeClicked(object? sender, RoutedEventArgs e)
    {
        if (Avalonia.Application.Current is not { } app) return;
        var theme = ActualThemeVariant == ThemeVariant.Light ? AppTheme.Dark : AppTheme.Light;
        ThemeToggleButton.SetCurrentValue(IsEnabledProperty, false);
        try
        {
            if (Settings is { } settings)
            {
                var current = await settings.LoadAsync();
                await settings.SaveAsync(current with { Theme = theme });
            }
            if (DataContext is MainWindowViewModel vm)
                vm.Settings.UpdateTheme(theme);
            app.RequestedThemeVariant = theme == AppTheme.Light ? ThemeVariant.Light : ThemeVariant.Dark;
        }
        catch (Exception)
        {
            ToolTip.SetTip(ThemeToggleButton, Strings.Get("ThemeSaveFailed"));
        }
        finally
        {
            ThemeToggleButton.SetCurrentValue(IsEnabledProperty, true);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (_trayIcon is not null) return;
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS()) return;
        var show = new NativeMenuItem(Strings.Get("OpenPlayer"));
        show.Click += (_, _) => RestoreWindow();
        var exit = new NativeMenuItem(Strings.Get("Exit"));
        exit.Click += (_, _) => { _forceExit = true; Close(); };
        var menu = new NativeMenu();
        menu.Items.Add(show);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exit);
        _trayIcon = new TrayIcon { Icon = Icon, ToolTipText = "MusicPlayer2", Menu = menu, IsVisible = false };
        _trayIcon.Clicked += (_, _) => RestoreWindow();
        if (Avalonia.Application.Current is { } app)
            TrayIcon.SetIcons(app, new TrayIcons { _trayIcon });
    }

    private void RestoreWindow()
    {
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        if (_trayIcon is not null) _trayIcon.IsVisible = false;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnClosing(e);
        if (e.Cancel || _exitApproved || e.CloseReason != WindowCloseReason.WindowClosing) return;
        if (!_forceExit && Settings?.Current.CloseBehavior == WindowCloseBehavior.MinimizeToTray)
        {
            e.Cancel = true;
            if (_trayIcon is not null)
            {
                _trayIcon.IsVisible = true;
                Hide();
            }
            else WindowState = WindowState.Minimized;
            return;
        }
        e.Cancel = true;
        if (!_savingBeforeExit) _ = SaveAndCloseAsync();
    }

    private async Task SaveAndCloseAsync()
    {
        _savingBeforeExit = true;
        try { if (Player is not null) await Player.SaveSessionAsync(); }
        catch (Exception) { /* Periodic checkpoints remain available if the final write fails. */ }
        finally
        {
            _exitApproved = true;
            Avalonia.Threading.Dispatcher.UIThread.Post(Close);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        Strings.Changed -= LanguageChanged;
        _trayIcon?.Dispose();
        Player?.Stop();
        base.OnClosed(e);
    }
}
