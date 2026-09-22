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
        [new(null, "Padrão do sistema")];

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
            AvailableAudioDevices = [new(null, "Padrão do sistema"), .. audioOutput.GetDevices()];
            AudioDeviceMessage = CanSelectAudioOutput ? audioOutput.StatusMessage
                : "A seleção de saída é controlada pelo sistema nesta plataforma.";
        }
        catch (Exception)
        {
            AudioDeviceMessage = "Não foi possível listar as saídas de áudio. Tente atualizar novamente.";
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
                selected = new AudioOutputDevice(AudioOutputDeviceId, "Dispositivo salvo (indisponível)");
                AvailableAudioDevices = [.. AvailableAudioDevices, selected];
                AudioDeviceMessage = "O dispositivo salvo está indisponível. O player usará a saída padrão.";
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
        AudioDeviceMessage = audioOutput.StatusMessage;
    }

    /// <summary>Null selects the system default audio output device.</summary>
    [ObservableProperty]
    public partial string? AudioOutputDeviceId { get; set; }

    [ObservableProperty]
    public partial bool RememberPlaybackPosition { get; set; } = true;

    [ObservableProperty]
    public partial bool ContinuePlaybackOnPlaylistChange { get; set; } = true;
}