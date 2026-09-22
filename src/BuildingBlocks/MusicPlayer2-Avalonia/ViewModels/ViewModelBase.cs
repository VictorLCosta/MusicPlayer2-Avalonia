using CommunityToolkit.Mvvm.ComponentModel;

namespace MusicPlayer2_Avalonia.ViewModels;

internal abstract class ViewModelBase : ObservableValidator, IDisposable
{
    private bool _disposed;

    public virtual void Dispose()
    {
        if (_disposed) return;

        ClearErrors();
        ErrorsChanged -= null;

        _disposed = true;
    }
}