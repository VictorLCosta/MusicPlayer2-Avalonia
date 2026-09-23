using System.Collections;
using System.Globalization;
using System.Resources;

namespace MusicPlayer2_Avalonia.Localization;

/// <summary>ResX lookup with an explicit UI culture and English fallback.</summary>
public static class Strings
{
    private static readonly ResourceManager Manager = new(
        "MusicPlayer2_Avalonia.Localization.Strings", typeof(Strings).Assembly);
    private static readonly CultureInfo SystemCulture = CultureInfo.CurrentUICulture;
    private static CultureInfo _culture = ResolveCulture(null);

    public static event EventHandler? Changed;
    public static string CultureName => _culture.Name;

    public static CultureInfo ResolveCulture(string? language) =>
        CultureInfo.GetCultureInfo((language ?? SystemCulture.Name) switch
        {
            "pt" or "pt-BR" => "pt-BR",
            "zh" or "zh-CN" or "zh-SG" or "zh-Hans" or "zh-Hans-CN" or "zh-Hans-SG" => "zh-Hans",
            _ => "en"
        });

    public static string Get(string key) => Manager.GetString(key, _culture) ?? key;

    public static string Format(string key, params object[] values) =>
        string.Format(_culture, Get(key), values);

    public static void Apply(string? language)
    {
        var culture = ResolveCulture(language);
        var changed = _culture.Name != culture.Name;
        _culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        // Keep the user's number/date formatting culture independent of the UI language.
        if (Avalonia.Application.Current is { } app)
            foreach (DictionaryEntry entry in Manager.GetResourceSet(CultureInfo.InvariantCulture, true, true)!)
                if (entry.Key is string key) app.Resources["Text." + key] = Get(key);
        if (changed) Changed?.Invoke(null, EventArgs.Empty);
    }
}
