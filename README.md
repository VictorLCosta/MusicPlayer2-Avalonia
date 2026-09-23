# MusicPlayer2 Avalonia

A C#/Avalonia reimplementation of [MusicPlayer2](https://github.com/zhongyang219/MusicPlayer2), the Windows music player created by **zhongyang219 and its contributors**, developed for the [Avalonia Port Challenge](https://avaloniaui.net/blog/avalonia-port-challenge).

The original project deserves all the credit for the player this work is based on: its local music workflow, feature set, and interface provide the reference for this migration. So, please visit the [upstream repository](https://github.com/zhongyang219/MusicPlayer2), read its [documentation](https://github.com/zhongyang219/MusicPlayer2/wiki), and support its maintainers. This repository is a separate project maintained by me, [Victor Lima Costa](https://github.com/VictorLCosta).

The current implementation includes desktop playback, native Android and iOS audio integrations, a responsive shared interface, and three interface languages. It does not yet reproduce the full upstream feature set. Mobile device validation and the browser workflow have remaining limitations described below.

## Original application and Avalonia version

The original is a Windows application written in C++ using MFC. Moving to Avalonia means rebuilding the interface and application services in C#, as well as replacing platform-dependent audio and storage integrations.

| Area | Original MusicPlayer2 | This Avalonia version |
| --- | --- | --- |
| Language and UI | C++, MFC, Windows UI and custom drawing | C#, .NET 10, Avalonia XAML and MVVM |
| Platform approach | Windows application | Shared UI and services with Desktop, Android, iOS, and Browser host projects; feature parity and runtime validation vary by target |
| Audio backend | BASS and FFmpeg playback kernels | Desktop LibVLCSharp, Android MediaPlayer, iOS AVAudioPlayer, and a separate JavaScript backend for the browser |
| Music library | Local music library and metadata features | Folder scanning, metadata indexing, search, and artist/album navigation backed by EF Core and SQLite |
| Playback | Local music playback with extensive player options | Play/pause, seek, volume, previous/next, shuffle/repeat, queue services, saved playback sessions, and horizontal artwork swipes in the mobile layout |
| Artwork | Cover display and online cover lookup/download | Embedded cover reading through TagLibSharp; no online lookup/download |
| Spectrum | Audio spectrum visualization | PCM capture, FFT analysis, and a custom Avalonia spectrum control |
| Appearance | Multiple layouts, theme colors, and XML-customizable interfaces | Avalonia views and styles, light/dark/system themes, and configurable accent color |
| Audio effects | Equalizer and reverb | Functional 10-band VLC equalizer with preamp, bypass, reset, and saved settings; no reverb implementation |
| Lyrics | Synchronized and karaoke lyrics, desktop lyrics, editing, and downloads | Outside the current implementation |

The upstream feature comparison is based on its [English README](https://github.com/zhongyang219/MusicPlayer2/blob/master/README_en-us.md) and source tree. Entries for this version describe the current code, rather than a claim that every target has passed end-to-end testing.

## What is outside the current scope

This migration covers a subset of the original player's everyday local playback workflow. The following upstream capabilities have not been carried over:

- Lyrics display, karaoke highlighting, lyric editing, online lyric downloads, desktop lyric overlays, and lyrics in the Windows taskbar search box.
- Online album artwork matching and downloading, and editing tags in audio files. The current metadata integration reads tags and embedded artwork.
- CUE sheet track splitting and audio format conversion.
- Reverb and equalization on browser/mobile hosts. The desktop VLC equalizer is functional.
- The original mini-player, XML skin/layout compatibility, and full reproduction of its alternative interfaces.
- Windows taskbar thumbnail controls and the original Windows-specific shell integrations.
- Switching between the original BASS and FFmpeg playback kernels.

Compatibility with the original application's configuration, playlists, and library files is not established. The class named `LegacySettingsMigration` imports an older JSON settings location; it is not a general importer for the upstream application's data.

These are the boundaries of the current implementation, not a promise that every omitted feature will be added later.

## Migration notes: the most involved engineering work

The areas below stand out as the most technically involved parts of the implementation. This assessment is based on the code and its integration requirements; the repository does not record time spent per feature, so it cannot establish an exact effort ranking or a measured migration cost.

### Rebuilding the Windows interface as a shared application

The original MFC dialogs and custom drawing could not simply become Avalonia views. The interface is expressed through XAML, reusable controls, styles, and observable view models, while playback, settings, and library operations live in application services. This changes how UI state, commands, notifications, and application lifetime fit together.

The substantial work here is preserving useful player behavior across that new structure: changes in the library, current track, theme, and settings must reach the correct controls without coupling the shared application to a Windows dialog. The [shared UI](src/BuildingBlocks/MusicPlayer2-Avalonia) and [application layer](src/BuildingBlocks/MusicPlayer2-Avalonia.Application) show that separation.

### Replacing the audio backend and synchronizing the spectrum

Replacing BASS/FFmpeg with LibVLCSharp requires more than forwarding play and pause. Device selection, media lifetime, seeking, end-of-track events, and saved-position playback all have to agree with the application queue.

Spectrum analysis adds a second timing problem. [VlcSpectrumReader](src/BuildingBlocks/MusicPlayer2-Avalonia.Infrastructure/AudioEngine/VlcSpectrumReader.cs) uses a separate silent decoder to capture PCM while VLC retains ownership of normal audio output. Samples are transformed with a 2,048-point FFT into 64 frequency bands, queued with timestamps, and matched to VLC's clock before rendering. Pause, seek, and stop clear or synchronize this state. Native callbacks also need exception containment and bounded buffering. The tradeoff is an additional decoder and synchronization work to keep visualization from taking over device output.

### Keeping playback state consistent across restarts and library changes

A music player has several related states: the indexed library, ordered queue, current track, playback position, and user preferences. [PlayerService](src/BuildingBlocks/MusicPlayer2-Avalonia.Application/Player/PlayerService.cs) and [PlaybackSessionStore](src/BuildingBlocks/MusicPlayer2-Avalonia.Application/Player/PlaybackSessionStore.cs) coordinate restoration of a saved session. Library scanning updates metadata, skips unchanged files, and saves imports in batches; maintenance can remove missing entries without deleting audio files.

The difficulty is making those operations agree when files disappear, the playlist changes during playback, or a stored output device is unavailable. For example, the VLC backend falls back to the default output while retaining the user's device preference.

### Integrating mobile playback and file access

Android and iOS select native audio engines through the shared application's platform service registration. The mobile shell handles settings navigation, while each host integrates audio interruptions, background playback, system media controls, and lifecycle checkpoints.

The Android toolbar spectrum reads FFT data from a `Visualizer` attached to the player's own audio session. Android requires the `RECORD_AUDIO` runtime permission for this API; the app requests it on playback and does not open the microphone or save recordings. If permission is denied or the device does not support the effect, playback continues without animated spectrum data.

Mobile file pickers can return streams instead of durable filesystem paths. Imports therefore copy content into private storage before indexing it, handle filename collisions, and clean up interrupted copies. Library operations use separate database scopes from playback, and asynchronous track changes are serialized so UI and system media commands do not prepare competing tracks at the same time.

### Adapting persistence and playback to the browser

The browser cannot use the native desktop audio and filesystem paths unchanged. Its host supplies a JavaScript audio engine and IndexedDB-backed storage behind shared interfaces.

[BrowserSqliteStorage](src/Hosts/Browser/Storage/BrowserSqliteStorage.cs) saves consistent SQLite backups to durable storage, including committed write-ahead-log data. It serializes writes, finishes persistence after a database commit even if cancellation is requested, and blocks subsequent saves after a persistence failure. These details prevent a later operation from silently masking an earlier failed save.

This adaptation is still incomplete at the application level: the browser audio engine rejects local file URIs, while parts of the shared library and session workflow rely on local paths. A browser host is therefore not equivalent to full desktop playback support.

## Platform status

### Interface language

The shared interface supports English, Brazilian Portuguese, and Simplified Chinese. Choose **Settings → General → Language** and click **Apply** to switch without restarting. The selection is saved; **System language** uses the device language when supported and falls back to English. Translation resources and contributor instructions are in [Localization](src/BuildingBlocks/MusicPlayer2-Avalonia/Localization/README.md).

The ResX resources cover player controls, settings, library labels, equalizer controls, file-picker prompts, and interface messages. Existing views update when the language changes. Song metadata, filenames, and device names retain their original values.

### Using the desktop equalizer

Open **Equalizer** from the sliders button beside Settings in the player toolbar. Enable it and adjust the ten frequency bands or preamp between -12 and +12 dB. Changes apply immediately, including during playback. Disable it to bypass processing without losing the curve, or use **Reset adjustments** to return the bands and preamp to 0 dB. Lower the preamp if boosted bands cause distortion.

Closing the dialog saves the settings in `equalizer.json` in the application storage directory. They are restored before playback on the next launch. If saving fails, the dialog offers a retry or closing without saving. A shared editor can also be hosted inside the mobile shell; the button remains disabled when the audio backend does not implement equalization.

### Available hosts

- **Desktop:** the main implementation path uses LibVLCSharp, SQLite, and local storage. The desktop project includes the Windows LibVLC native package. macOS and Linux need appropriate native runtime packaging and platform verification before being presented as supported releases.
- **Browser:** includes dedicated audio and persistent-storage adapters, but local-file workflows and desktop-only capabilities still need adaptation. Its audio backend does not implement the native spectrum or output-device interfaces.
- **Android:** uses the system MediaPlayer, with a media-playback foreground service, media session, notification controls, audio-focus handling, and pause on headphone disconnection. Android 7.0/API 24 is the minimum target. The mobile shell supports settings navigation, the system Back action, and importing selected audio files into private storage.
- **iOS:** uses AVAudioPlayer with a playback audio session, the background-audio capability, Now Playing metadata, remote commands, interruption/route handling, and playback checkpoints. It shares the mobile shell and import workflow. Compiling the managed host on Windows does not validate native linking, signing, or device execution; those require the Apple build environment.

### Mobile usage and remaining validation

Use **Import music** in the player to select one or more files. The app copies their contents into private storage and indexes those copies, so playback does not depend on retaining a document-provider URI permission. Originals are untouched. Importing the same content under the same filename reuses the existing copy. Copies occupy device storage and are removed when the app is uninstalled.

In the mobile layout, swipe horizontally across the album artwork to change tracks. The current gesture mapping is **left for previous** and **right for next**. The gesture uses the same playback commands as the transport buttons.

The mobile audio engines use system codecs, so they do not promise the same format coverage as desktop VLC. The current native engines do not implement the equalizer, audio spectrum, or manual output-device selection. Output routing is controlled by the operating system.

The implementation includes background playback and controls, but these still need end-to-end validation on real Android/iOS devices: import from local/cloud document providers, screen locking, incoming calls, Bluetooth/headphone disconnection, process recreation, safe-area layout, and release builds. A successful build alone is not a device compatibility claim.

## Credits and licensing

My thanks to the **Avalonia team** for organizing the Avalonia Port Challenge and giving me the opportunity to participate. Rebuilding this player has been a valuable opportunity to learn, explore cross-platform development, and put Avalonia into practice.

Credit for the original MusicPlayer2 belongs to **zhongyang219 and the upstream contributors**. Its [license](https://github.com/zhongyang219/MusicPlayer2/blob/master/LICENSE) is GNU GPL v3. This repository currently contains an [MIT license file](LICENSE); that file does not relicense upstream code or assets. These references describe the files in the two repositories, not a completed audit of source or asset provenance.

This implementation also uses Avalonia, CommunityToolkit.Mvvm, LibVLCSharp/LibVLC, TagLibSharp, Entity Framework Core, and SQLite. Their respective authors and contributors provide the foundations for the UI, playback, metadata, and persistence layers.

## Project structure

- `src/BuildingBlocks/MusicPlayer2-Avalonia`: shared Avalonia application, including the UI and view models.
- `src/BuildingBlocks/MusicPlayer2-Avalonia.Application`: application services and use cases.
- `src/BuildingBlocks/MusicPlayer2-Avalonia.Domain`: domain models and contracts.
- `src/BuildingBlocks/MusicPlayer2-Avalonia.Infrastructure`: infrastructure and service implementations.
- `src/Hosts/Desktop`: desktop host targeting Windows, macOS, and Linux.
- `src/Hosts/Android`, `src/Hosts/iOS`, and `src/Hosts/Browser`: additional hosts for running the shared application on mobile devices and in the browser.
- `src/BuildingBlocks/MusicPlayer2-Avalonia/Localization`: English, Brazilian Portuguese, and Simplified Chinese resources and runtime language switching.
- `tests`: executable checks for localization, mobile imports and playback concurrency, and the desktop equalizer.

## Development

The desktop project targets .NET 10. Install the .NET 10 SDK, then build and run it from the repository root:

```sh
dotnet build src/Hosts/Desktop/MusicPlayer2-Avalonia.Desktop.csproj
dotnet run --project src/Hosts/Desktop
```

To build the entire solution, including the mobile and browser hosts, install the workloads required by those targets and run:

```sh
dotnet build MusicPlayer2-Avalonia.slnx
```

### Mobile release builds

The VLC backend and its `LibVLCSharp` dependency live in `MusicPlayer2-Avalonia.Infrastructure.Desktop`, referenced only by the Desktop host and its equalizer checks. Shared infrastructure does not register an audio engine; each host registers its own. This keeps VLC native framework requirements out of iOS, Android and Browser builds.

Mobile hosts currently preserve managed code because the Avalonia XAML dependency metadata and the reflection-based EF Core, SQLite and TagLibSharp dependencies have not been validated with trimming. Android disables `PublishTrimmed` and `RunAOTCompilation` (Android AOT requires trimming). iOS uses `TrimMode=copy` to preserve assemblies while retaining the required Apple linker pipeline. Compiler warnings still fail the build.

This compatibility configuration trades package size and Android AOT startup optimizations for preserving code used at runtime. Re-enable trimming only after resolving linker diagnostics and testing imports, database access, metadata reading and playback on devices. iOS native linking and signing must be validated on macOS.

### Android debugging in VS Code

The repository includes **Debug - Desktop** and **Debug - Android** profiles in [launch.json](.vscode/launch.json). Android debugging requires the Android workload and SDK, the **Mono Debug** extension (`ms-vscode.mono-debug`), and an authorized device or emulator.

Enable USB debugging on the phone, connect it, accept the computer's authorization prompt, and check the connection:

```sh
adb devices
```

The device must appear as `device`, not `unauthorized`. Select **Debug - Android** and press **F5**. Its pre-launch task builds, installs, and starts the app with the debugger enabled, then attaches on port 10000. Keep one target connected for this default configuration.

The Android debug APK includes its managed assemblies for direct installation without IDE fast deployment. Building the Android host in Debug produces `src/Hosts/Android/bin/Debug/net10.0-android/io.github.victorlcosta.musicplayer2-Signed.apk`. This is a development package, not a store release; the device validation items are listed under **Mobile usage and remaining validation** above.

### Automated checks

Run from the repository root:

```sh
dotnet run --project tests/LocalizationChecks
dotnet run --project tests/MobileChecks
```

Localization checks verify translation completeness, loading of language resources, live updates in an existing view, fallback, applying/canceling changes, and saved language preferences. Mobile checks cover provider streams, import integrity and cancellation, duplicate handling, SQLite indexing, and concurrent playback requests.

The equalizer checks require native LibVLC. After building the desktop host on Windows:

```sh
dotnet run --project tests/EqualizerChecks -- src/Hosts/Desktop/bin/Debug/net10.0/libvlc/win-x64
```

These checks exercise saved settings and actual decoded PCM to verify equalizer gain and bypass behavior. Successful automated checks and builds do not replace testing audio, gestures, layout, and lifecycle behavior on physical devices.
