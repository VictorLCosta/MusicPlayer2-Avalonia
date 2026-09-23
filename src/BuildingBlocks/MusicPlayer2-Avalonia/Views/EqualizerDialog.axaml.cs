using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MusicPlayer2_Avalonia.ViewModels;

namespace MusicPlayer2_Avalonia.Views;

public partial class EqualizerDialog : Window
{
    private bool _saved;
    private bool _closing;
    public EqualizerDialog()
    {
        InitializeComponent();
        Closing += SaveBeforeClosing;
    }

    private void CloseClicked(object? sender, EventArgs e) => Close();
    private void CloseWithoutSaving(object? sender, EventArgs e)
    {
        _saved = true;
        Close();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) { e.Handled = true; Close(); }
    }

    private async void SaveBeforeClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_saved || DataContext is not EqualizerViewModel vm) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        try
        {
            if (await vm.SaveAsync())
            {
                _saved = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(Close);
            }
        }
        finally { _closing = false; }
    }
}

