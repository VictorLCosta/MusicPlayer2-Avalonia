using System.Text.Json;

using MusicPlayer2_Avalonia.Application.Settings.Models;

namespace MusicPlayer2_Avalonia.Application.Settings;

public sealed class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MusicPlayer2",
        "settings.json");

    private static readonly JsonSerializerOptions JsonSerializerOptions = new () { 
        WriteIndented = true 
    };

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return new AppSettings();

        var json = File.ReadAllText(SettingsPath);
        return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(settings, JsonSerializerOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
