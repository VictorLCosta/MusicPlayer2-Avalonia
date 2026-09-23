# Equalizer integration checks

This executable checks saved settings, invalid-data recovery, live preamp and band processing, bypass, and retention across track changes using the production VLC engine. It generates a temporary 1 kHz WAV and captures decoded PCM through VLC callbacks, without playing the test tone through speakers.

From the repository root on Windows x64:

```powershell
dotnet build src/Hosts/Desktop/MusicPlayer2-Avalonia.Desktop.csproj -o artifacts/equalizer-desktop
dotnet run --project tests/EqualizerChecks -- artifacts/equalizer-desktop/libvlc/win-x64
```

The argument must point to a native LibVLC directory matching the process architecture. The executable exits with an error if a check fails. It does not change the user's music library or settings; persistence checks use in-memory storage. These checks validate audio behavior and persistence, not the dialog's visual layout.
