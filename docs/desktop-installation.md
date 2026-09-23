# Desktop installation

These packages include the .NET runtime. Linux and macOS packages **require a
separate native VLC 3 installation**, including its plugins. They are not standalone
VLC distributions. Windows packages include the native VLC libraries.

Download the package matching your operating system and CPU architecture.

## Windows (win-x64)

Extract the ZIP into a folder and run `MusicPlayer2-Avalonia.Desktop.exe`.
Keep the other files and the `libvlc` directory alongside the executable.

## Linux (linux-x64)

Install VLC 3 from your distribution's package manager. On Debian/Ubuntu:

```sh
sudo apt update
sudo apt install vlc libvlc5
```

Extract the `.tar.gz` with `tar -xzf MusicPlayer2-linux-x64.tar.gz` into an empty
directory, then run `./MusicPlayer2-Avalonia.Desktop` from that directory in a
graphical desktop session. The archive preserves executable permissions. Keep
all extracted files together. Other distributions may use different package names.

## macOS (osx-arm64 or osx-x64)

Install VLC 3 from https://www.videolan.org/vlc/ into `/Applications/VLC.app`.
Use matching architectures: Apple Silicon for `osx-arm64`, Intel for `osx-x64`.

Extract the `.tar.gz` using Archive Utility or `tar -xzf`, then open
`MusicPlayer2-Avalonia.Desktop.app`. Keep the app bundle intact. These development
bundles are not Developer ID signed or notarized; macOS may require approval
through Privacy & Security before opening them.

## Troubleshooting

If the app reports that LibVLC cannot be loaded, check the VLC version and CPU
architecture, and ensure its native libraries and plugins are installed. Installing
only the managed `LibVLCSharp` wrapper is insufficient. Packaging and playback
still need validation on the target Linux/macOS installation.
