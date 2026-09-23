using Avalonia.Controls;
using Avalonia.Interactivity;
namespace MusicPlayer2_Avalonia.Views;
public partial class EqualizerEditor : UserControl
{
    public event EventHandler? CloseRequested;
    public event EventHandler? CloseWithoutSavingRequested;
    public EqualizerEditor() => InitializeComponent();
    private void CloseClicked(object? sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);
    private void CloseWithoutSaving(object? sender, RoutedEventArgs e) => CloseWithoutSavingRequested?.Invoke(this, EventArgs.Empty);
}
