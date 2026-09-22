using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Avalonia;
using Avalonia.Controls;

using MusicPlayer2_Avalonia.Application.Common.Storage;

namespace MusicPlayer2_Avalonia.Extensions;

public static class WindowExtensions
{
    /// <summary>Restores desktop geometry before Show and saves it on closing. Dispose to stop tracking.</summary>
    public static IDisposable ManageWindowState(this Window window, IAppStorage storage, string key)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new WindowStatePersistence(window, storage, $"windows/{key}.json");
    }

    private sealed class WindowStatePersistence : IDisposable
    {
        private readonly Window _window;
        private readonly IAppStorage _storage;
        private readonly string _path;
        private SavedWindowState _normal = new();
        private bool _maximized;

        public WindowStatePersistence(Window window, IAppStorage storage, string path)
        {
            _window = window;
            _storage = storage;
            _path = path;
            Restore();
            Capture();
            window.PropertyChanged += Changed;
            window.PositionChanged += PositionChanged;
            window.Closing += Closing;
            window.Opened += Opened;
            window.Closed += Closed;
        }

        private void Restore()
        {
            try
            {
                // Only desktop windows use this service. Native storage never captures the UI context.
                var bytes = _storage.ReadBytesAsync(_path).GetAwaiter().GetResult();
                if (bytes is null) return;
                var saved = JsonSerializer.Deserialize(bytes, WindowStateJsonContext.Default.SavedWindowState);
                if (saved is null || !double.IsFinite(saved.Width) || !double.IsFinite(saved.Height) ||
                    saved.Width <= 0 || saved.Height <= 0) return;
                var position = new PixelPoint(saved.X, saved.Y);
                var screen = _window.Screens.ScreenFromPoint(position) ?? _window.Screens.Primary;
                if (screen is null) return;
                var area = screen.WorkingArea;
                var scale = screen.Scaling;
                _window.Width = Math.Clamp(saved.Width, _window.MinWidth,
                    Math.Max(_window.MinWidth, Math.Min(_window.MaxWidth, area.Width / scale)));
                _window.Height = Math.Clamp(saved.Height, _window.MinHeight,
                    Math.Max(_window.MinHeight, Math.Min(_window.MaxHeight, area.Height / scale)));
                _window.WindowStartupLocation = WindowStartupLocation.Manual;
                _window.Position = new PixelPoint(
                    Math.Clamp(saved.X, area.X, Math.Max(area.X, area.Right - (int)(_window.Width * scale))),
                    Math.Clamp(saved.Y, area.Y, Math.Max(area.Y, area.Bottom - (int)(_window.Height * scale))));
                _normal = new SavedWindowState
                {
                    X = _window.Position.X,
                    Y = _window.Position.Y,
                    Width = _window.Width,
                    Height = _window.Height
                };
                if (saved.Maximized && _window.CanMaximize && _window.CanResize)
                    _window.WindowState = WindowState.Maximized;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                Trace.TraceWarning("Unable to restore window state: {0}", ex.Message);
            }
        }

        private void Capture()
        {
            if (_window.WindowState == WindowState.Normal)
            {
                if (!_window.IsVisible) return;
                var width = _window.ClientSize.Width > 0 ? _window.ClientSize.Width : _window.Width;
                var height = _window.ClientSize.Height > 0 ? _window.ClientSize.Height : _window.Height;
                if (double.IsFinite(width) && double.IsFinite(height) && width > 0 && height > 0)
                    _normal = new SavedWindowState
                    {
                        X = _window.Position.X,
                        Y = _window.Position.Y,
                        Width = width,
                        Height = height
                    };
                _maximized = false;
            }
            else if (_window.WindowState == WindowState.Maximized) _maximized = true;
        }

        private void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Window.WindowStateProperty || e.Property == TopLevel.ClientSizeProperty) Capture();
        }

        private void PositionChanged(object? sender, PixelPointEventArgs e) => Capture();
        private void Opened(object? sender, EventArgs e) => Capture();

        private void Closing(object? sender, WindowClosingEventArgs e)
        {
            if (e.Cancel || _normal.Width <= 0 || _normal.Height <= 0) return;
            try
            {
                _normal.Maximized = _maximized;
                // Complete the small native write before the desktop lifetime exits.
                _storage.WriteBytesAsync(_path,
                    JsonSerializer.SerializeToUtf8Bytes(_normal, WindowStateJsonContext.Default.SavedWindowState))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning("Unable to save window state: {0}", ex.Message);
            }
        }

        private void Closed(object? sender, EventArgs e) => Dispose();

        public void Dispose()
        {
            _window.PropertyChanged -= Changed;
            _window.PositionChanged -= PositionChanged;
            _window.Closing -= Closing;
            _window.Opened -= Opened;
            _window.Closed -= Closed;
        }
    }
}

internal sealed class SavedWindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool Maximized { get; set; }
}

[JsonSerializable(typeof(SavedWindowState))]
internal sealed partial class WindowStateJsonContext : JsonSerializerContext;