using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Player;

namespace MusicPlayer2_Avalonia.ViewModels.Settings;

internal sealed partial class PlaybackSettingsViewModel(AudioOutputService audioOutput) : ViewModelBase
{
    private bool _updatingDevices;

    public bool CanSelectAudioOutput => audioOutput.IsSupported;

    [ObservableProperty]
    public partial IReadOnlyList<AudioOutputDevice> AvailableAudioDevices { get; private set; } =
        [new(null, Strings.Get("SystemOutput"))];

    [ObservableProperty]
    public partial AudioOutputDevice? SelectedAudioDevice { get; set; }

    [ObservableProperty]
    public partial string? AudioDeviceMessage { get; private set; }

    partial void OnSelectedAudioDeviceChanged(AudioOutputDevice? value)
    {
        if (!_updatingDevices && value is not null)
            AudioOutputDeviceId = value.Id;
    }

    partial void OnAudioOutputDeviceIdChanged(string? value) => SynchronizeSelection();

    [RelayCommand]
    public void RefreshAudioDevices()
    {
        try
        {
            _updatingDevices = true;
            AvailableAudioDevices = [new(null, Strings.Get("SystemOutput")), .. audioOutput.GetDevices()];
            AudioDeviceMessage = CanSelectAudioOutput ? LocalizedOutputStatus
                : Strings.Get("OutputManagedBySystem");
        }
        catch (Exception)
        {
            AudioDeviceMessage = Strings.Get("OutputListFailed");
        }
        finally
        {
            _updatingDevices = false;
        }
        SynchronizeSelection();
    }

    private void SynchronizeSelection()
    {
        _updatingDevices = true;
        try
        {
            var selected = AvailableAudioDevices.FirstOrDefault(device =>
                string.Equals(device.Id, AudioOutputDeviceId, StringComparison.Ordinal));
            if (selected is null)
            {
                selected = new AudioOutputDevice(AudioOutputDeviceId, Strings.Get("UnavailableOutput"));
                AvailableAudioDevices = [.. AvailableAudioDevices, selected];
                AudioDeviceMessage = Strings.Get("OutputUnavailable");
            }
            SelectedAudioDevice = selected;
        }
        finally
        {
            _updatingDevices = false;
        }
    }

    public void ApplyAudioOutput()
    {
        audioOutput.Apply(AudioOutputDeviceId);
        AudioDeviceMessage = LocalizedOutputStatus;
    }

    private string? LocalizedOutputStatus => audioOutput.StatusMessage is null ? null
        : Strings.Get(CanSelectAudioOutput ? "OutputRestoreFailed" : "OutputManagedBySystem");

    /// <summary>Null selects the system default audio output device.</summary>
    [ObservableProperty]
    public partial string? AudioOutputDeviceId { get; set; }

    [ObservableProperty]
    public partial bool RememberPlaybackPosition { get; set; } = true;

    [ObservableProperty]
    public partial bool ContinuePlaybackOnPlaylistChange { get; set; } = true;
}
