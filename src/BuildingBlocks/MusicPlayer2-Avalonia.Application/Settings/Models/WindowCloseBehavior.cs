using System.Text.Json.Serialization;

namespace MusicPlayer2_Avalonia.Application.Settings.Models;

[JsonConverter(typeof(JsonStringEnumConverter<WindowCloseBehavior>))]
public enum WindowCloseBehavior
{
    Exit = 0,
    MinimizeToTray = 1
}