using Avalonia.Controls;
using Avalonia.VisualTree;
using MusicPlayer2_Avalonia.ViewModels;

namespace MusicPlayer2_Avalonia.Views;

public partial class MobileShell : UserControl
{
    private Func<Task>? _closeDialog;
    private TaskCompletionSource? _dialogCompletion;
    public MobileShell() => InitializeComponent();

    internal static MainWindowViewModel? NavigationFor(Control view) =>
        view.GetVisualAncestors().OfType<MobileShell>().FirstOrDefault()?.DataContext as MainWindowViewModel
        ?? TopLevel.GetTopLevel(view)?.DataContext as MainWindowViewModel;

    internal async Task ShowEqualizerAsync(EqualizerViewModel model)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dialogCompletion = completion;
        var editor = new EqualizerEditor { DataContext = model };
        var closing = false;
        async Task CloseAsync(bool save)
        {
            if (closing) return;
            closing = true;
            try
            {
                if (save && !await model.SaveAsync()) return;
                completion.TrySetResult();
            }
            finally { closing = false; }
        }
        _closeDialog = () => CloseAsync(true);
        editor.CloseRequested += async (_, _) => await CloseAsync(true);
        editor.CloseWithoutSavingRequested += async (_, _) => await CloseAsync(false);
        DialogContent.Content = editor;
        PageContent.IsVisible = false;
        DialogOverlay.IsVisible = true;
        try { await completion.Task; }
        finally
        {
            _closeDialog = null;
            _dialogCompletion = null;
            DialogContent.Content = null;
            DialogOverlay.IsVisible = false;
            PageContent.IsVisible = true;
        }
    }

    public bool NavigateBack()
    {
        if (_closeDialog is not null) { _ = _closeDialog(); return true; }
        if (DataContext is MainWindowViewModel vm && vm.CurrentPage != vm.Main)
        {
            vm.NavigateToMain();
            return true;
        }
        return false;
    }

    protected override async void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var completion = _dialogCompletion;
        try { if (_closeDialog is { } close) await close(); }
        finally { completion?.TrySetResult(); }
    }
}
