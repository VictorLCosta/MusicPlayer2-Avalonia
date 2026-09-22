using System.Text.Json.Serialization;

namespace MusicPlayer2_Avalonia.Application.Settings.Models;

[JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
public enum AppTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}