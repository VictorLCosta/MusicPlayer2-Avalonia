# Interface localization

- `Strings.resx`: English and fallback resources.
- `Strings.pt-BR.resx`: Brazilian Portuguese.
- `Strings.zh-Hans.resx`: Simplified Chinese.

Choose the language in **Settings → General → Language**, then apply. Cancel restores the saved selection. The default follows the system language, with English as the fallback for unsupported locales. The preference is stored in `AppSettings.Language`; existing settings files remain compatible.

`Strings.Apply` reads the ResX resources and updates the application's `Text.*` dynamic resources. XAML uses `{DynamicResource Text.Key}` so existing controls update without recreating views. Code uses `Strings.Get("Key")` or `Strings.Format("Key", values)`; view models with cached labels subscribe to `Strings.Changed` and unsubscribe when disposed. Number/date formatting remains independent of the interface language.

Add the same key to all three resource files. Keep placeholders such as `{0}` consistent. Song metadata, filenames, device names, and diagnostic messages originating from external libraries are not translated.

Validation:

```powershell
dotnet run --project tests/LocalizationChecks
```

The checks cover translation completeness, satellite-resource loading, live updates in an existing settings view, fallback, applying/canceling changes, and persistence through the real settings serializer.

Based on [Avalonia's ResX localization guide](https://docs.avaloniaui.net/docs/app-development/localizing).
