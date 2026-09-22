using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Extensions;

using BaseWindow = Avalonia.Controls.Window;

namespace MusicPlayer2_Avalonia.Styles.Window;

[TemplatePart("PART_MaximizeButton", typeof(Button))]
[TemplatePart("PART_MinimizeButton", typeof(Button))]
[TemplatePart("PART_CloseButton", typeof(Button))]
[TemplatePart("PART_TitleBar", typeof(Border))]
public class CustomWindow : BaseWindow
{
    public static readonly StyledProperty<IImage?> BackdropImageProperty =
        AvaloniaProperty.Register<CustomWindow, IImage?>(nameof(BackdropImage));

    public IImage? BackdropImage
    {
        get => GetValue(BackdropImageProperty);
        set => SetValue(BackdropImageProperty, value);
    }

    private Button? _maximizeButton;
    private Button? _minimizeButton;
    private Button? _closeButton;
    private Border? _titleBar;
    private Border? _root;
    private WindowState _stateBeforeFullScreen = WindowState.Normal;
    private IDisposable? _statePersistence;
    private IAppStorage? _stateStorage;

    protected override Type StyleKeyOverride => typeof(CustomWindow);

    /// <summary>Configure before showing the window; persistence is opt-in via SaveWindowState.</summary>
    public IAppStorage? StateStorage
    {
        get => _stateStorage;
        set
        {
            if (ReferenceEquals(_stateStorage, value)) return;
            _stateStorage = value;
            UpdateStatePersistence();
        }
    }

    public static readonly StyledProperty<double> TitleFontSizeProperty =
        AvaloniaProperty.Register<CustomWindow, double>(nameof(TitleFontSize), 14);

    public double TitleFontSize
    {
        get => GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }

    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<CustomWindow, double>(nameof(TitleBarHeight), 42);

    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public static readonly StyledProperty<bool> ShowBottomBorderProperty =
        AvaloniaProperty.Register<CustomWindow, bool>(nameof(ShowBottomBorder), true);

    public bool ShowBottomBorder
    {
        get => GetValue(ShowBottomBorderProperty);
        set => SetValue(ShowBottomBorderProperty, value);
    }

    public static readonly StyledProperty<bool> IsTitleBarVisibleProperty =
        AvaloniaProperty.Register<CustomWindow, bool>(nameof(IsTitleBarVisible), true);

    public bool IsTitleBarVisible
    {
        get => GetValue(IsTitleBarVisibleProperty);
        set => SetValue(IsTitleBarVisibleProperty, value);
    }

    public static readonly StyledProperty<bool> ShowWindowControlsProperty =
        AvaloniaProperty.Register<CustomWindow, bool>(nameof(ShowWindowControls), true);

    public bool ShowWindowControls
    {
        get => GetValue(ShowWindowControlsProperty);
        set => SetValue(ShowWindowControlsProperty, value);
    }

    public static readonly StyledProperty<CornerRadius> RootCornerRadiusProperty =
        AvaloniaProperty.Register<CustomWindow, CornerRadius>(nameof(RootCornerRadius), new CornerRadius(10));

    public CornerRadius RootCornerRadius
    {
        get => GetValue(RootCornerRadiusProperty);
        set => SetValue(RootCornerRadiusProperty, value);
    }

    public static readonly StyledProperty<bool> CanMoveProperty =
        AvaloniaProperty.Register<CustomWindow, bool>(nameof(CanMove), true);

    public bool CanMove
    {
        get => GetValue(CanMoveProperty);
        set => SetValue(CanMoveProperty, value);
    }

    public static readonly StyledProperty<object?> RightWindowTitleBarContentProperty =
        AvaloniaProperty.Register<CustomWindow, object?>(nameof(RightWindowTitleBarContent), null);

    public object? RightWindowTitleBarContent
    {
        get => GetValue(RightWindowTitleBarContentProperty);
        set => SetValue(RightWindowTitleBarContentProperty, value);
    }

    public static readonly StyledProperty<bool> SaveWindowStateProperty =
        AvaloniaProperty.Register<CustomWindow, bool>(nameof(SaveWindowState), false);

    public bool SaveWindowState
    {
        get => GetValue(SaveWindowStateProperty);
        set => SetValue(SaveWindowStateProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        DetachButtons();

        base.OnApplyTemplate(e);

        _maximizeButton = e.NameScope.Find<Button>("PART_MaximizeButton");
        _minimizeButton = e.NameScope.Find<Button>("PART_MinimizeButton");
        _closeButton = e.NameScope.Find<Button>("PART_CloseButton");
        _titleBar = e.NameScope.Find<Border>("PART_TitleBar");
        _root = e.NameScope.Find<Border>("PART_Root");

        _maximizeButton?.Click += MaximizeClicked;
        _minimizeButton?.Click += MinimizeClicked;
        _closeButton?.Click += CloseClicked;

        UpdateChrome();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);

        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty &&
            change.GetNewValue<WindowState>() == WindowState.FullScreen)
            _stateBeforeFullScreen = change.GetOldValue<WindowState>() == WindowState.Maximized
                ? WindowState.Maximized : WindowState.Normal;

        if (change.Property == SaveWindowStateProperty) UpdateStatePersistence();
        if (change.Property == WindowStateProperty || change.Property == CanMaximizeProperty ||
            change.Property == CanMinimizeProperty || change.Property == CanResizeProperty ||
            change.Property == CanMoveProperty || change.Property == RootCornerRadiusProperty ||
            change.Property == IsTitleBarVisibleProperty || change.Property == ShowBottomBorderProperty ||
            change.Property == TitleBarHeightProperty)
            UpdateChrome();
    }

    protected override void OnClosed(EventArgs e)
    {
        _statePersistence?.Dispose();
        _statePersistence = null;
        DetachButtons();
        base.OnClosed(e);
    }

    private void UpdateStatePersistence()
    {
        _statePersistence?.Dispose();
        _statePersistence = SaveWindowState && StateStorage is not null
            ? this.ManageWindowState(StateStorage, GetType().Name) : null;
    }

    private void UpdateChrome()
    {
        var fullScreen = WindowState == WindowState.FullScreen;
        if (_root is not null)
            _root.CornerRadius = WindowState == WindowState.Normal ? RootCornerRadius : default;
        if (_titleBar is not null)
        {
            _titleBar.IsVisible = IsTitleBarVisible && !fullScreen;
            _titleBar.BorderThickness = new Thickness(0, 0, 0, ShowBottomBorder ? 1 : 0);
            WindowDecorationProperties.SetElementRole(_titleBar,
                CanMove && !fullScreen ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.User);
        }
        SetCurrentValue(ExtendClientAreaTitleBarHeightHintProperty,
            IsTitleBarVisible && !fullScreen ? TitleBarHeight : 0);
        if (_maximizeButton is not null)
        {
            _maximizeButton.IsVisible = CanMaximize && !fullScreen;
            _maximizeButton.IsEnabled = CanMaximize && CanResize;
            _maximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
            WindowDecorationProperties.SetElementRole(_maximizeButton,
                CanMaximize && CanResize ? WindowDecorationsElementRole.MaximizeButton : WindowDecorationsElementRole.User);
        }
        if (_minimizeButton is not null) _minimizeButton.IsVisible = CanMinimize && !fullScreen;
    }

    private void DetachButtons()
    {
        if (_maximizeButton is not null) _maximizeButton.Click -= MaximizeClicked;
        if (_minimizeButton is not null) _minimizeButton.Click -= MinimizeClicked;
        if (_closeButton is not null) _closeButton.Click -= CloseClicked;
    }

    private void MaximizeClicked(object? sender, RoutedEventArgs e)
    {
        if (CanMaximize && CanResize && WindowState != WindowState.FullScreen)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void MinimizeClicked(object? sender, RoutedEventArgs e)
    {
        if (CanMinimize) WindowState = WindowState.Minimized;
    }

    private void CloseClicked(object? sender, RoutedEventArgs e) => Close();

    protected void ExitFullScreen()
    {
        if (WindowState == WindowState.FullScreen) WindowState = _stateBeforeFullScreen;
    }
}