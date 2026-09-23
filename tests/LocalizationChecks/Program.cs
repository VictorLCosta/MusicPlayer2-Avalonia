using System.Collections;
using System.Globalization;
using System.Resources;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using MusicPlayer2_Avalonia.Application.Common;
using MusicPlayer2_Avalonia.Application.Common.Storage;
using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Application.Settings;
using MusicPlayer2_Avalonia.Localization;
using MusicPlayer2_Avalonia.ViewModels;
using MusicPlayer2_Avalonia.ViewModels.Settings;
using MusicPlayer2_Avalonia.Views.Settings;

AppBuilder.Configure<Avalonia.Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
var manager = new ResourceManager("MusicPlayer2_Avalonia.Localization.Strings", typeof(Strings).Assembly);
var neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
foreach (var culture in new[] { "pt-BR", "zh-Hans" })
{
    var translated = manager.GetResourceSet(CultureInfo.GetCultureInfo(culture), true, false)!;
    foreach (DictionaryEntry entry in neutral)
        Check(!string.IsNullOrWhiteSpace(translated.GetString((string)entry.Key)), $"{culture}: {entry.Key}");
}
Console.WriteLine("PASS: every resource has Portuguese and Simplified Chinese translations.");
Strings.Apply("en");
var general = new GeneralSettingsViewModel();
var view = new GeneralSettingsView { DataContext = general };
var window = new Window { Content = view };
window.Show();
var heading = view.GetLogicalDescendants().OfType<TextBlock>().First(control => control.Text == "General");
foreach (var (culture, expected) in new[] { ("pt-BR", "Geral"), ("zh-Hans", "常规"), ("en", "General") })
{
    Strings.Apply(culture);
    Dispatcher.UIThread.RunJobs();
    Check(heading.Text == expected, $"existing view updates to {culture}");
}
Console.WriteLine("PASS: an existing settings view changes language without recreation.");
Strings.Apply("xx-invalid");
Check(Strings.Get("General") == "General", "unsupported culture falls back to English");
Check(Strings.ResolveCulture("zh-CN").Name == "zh-Hans", "Chinese mainland locale mapping");
Check(Strings.ResolveCulture("pt-BR").Name == "pt-BR", "Brazilian Portuguese locale mapping");

var storage = new MemoryStorage();
using var settings = new SettingsService(storage);
var output = new AudioOutputService(new SilentEngine(), settings);
using var model = new SettingsViewModel(new AppearanceSettingsViewModel(), general,
    new PlaybackSettingsViewModel(output), new MediaLibrarySettingsViewModel(), settings, output);
model.LoadSettingsAsync().GetAwaiter().GetResult();
general.Language = "zh-Hans";
model.CancelSettingsCommand.Execute(null);
Check(general.Language == "system" && settings.Current.Language is null, "cancel keeps saved language");
general.Language = "zh-Hans";
model.SaveSettingsCommand.ExecuteAsync(null).GetAwaiter().GetResult();
Check(Strings.CultureName == "zh-Hans", "apply updates localization");
Check(model.SettingsMenuItems[0].Title == "常规", "settings menu updates");
Check(model.StatusMessage == "设置已保存。", "save confirmation uses new language");
using var reloaded = new SettingsService(storage);
Check(reloaded.LoadAsync().GetAwaiter().GetResult().Language == "zh-Hans", "language survives settings reload");
Console.WriteLine("PASS: apply, cancel, menu refresh, and persisted language.");
window.Close();
Console.WriteLine("All localization checks passed.");

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class MemoryStorage : IAppStorage
{
    private readonly Dictionary<string, byte[]> _data = [];
    public string RootDirectory => "";
    public string GetLocalPath(string path) => path;
    public Task<byte[]?> ReadBytesAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(_data.GetValueOrDefault(path));
    public Task WriteBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default) { _data[path] = bytes; return Task.CompletedTask; }
    public Task DeleteAsync(string path, CancellationToken cancellationToken = default) { _data.Remove(path); return Task.CompletedTask; }
}

sealed class SilentEngine : IAudioEngine
{
    public event EventHandler? PlaybackEnded { add { } remove { } }
    public bool IsPlaying => false;
    public TimeSpan Position => TimeSpan.Zero;
    public TimeSpan Duration => TimeSpan.Zero;
    public double Volume { get; set; }
    public Task LoadAsync(Uri source) => Task.CompletedTask;
    public void Play() { }
    public void Pause() { }
    public void StopAudio() { }
    public void Seek(TimeSpan position) { }
    public void Dispose() { }
}
