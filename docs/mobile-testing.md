# Mobile test downloads

These are test packages, not store releases. Download the artifact from the
Publish Platforms run and extract it. GitHub Actions artifact downloads require
a GitHub login and are retained for 14 days.

## Android: install on a phone

Open `MusicPlayer2-Android-test.apk` on Android 7 or later. Allow installation
from the browser/file manager when Android asks. Alternatively, use
`adb install MusicPlayer2-Android-test.apk` with USB debugging enabled.

The APK includes its managed assemblies and uses the SDK's development signing
key. Each fresh CI runner can generate a different key. If Android refuses an
update due to an incompatible signature, uninstall the previous test app first.
Uninstalling deletes its local settings, library database and imported private
files; preserve any files you need before doing so. These APKs are not intended
for Google Play distribution.

## iOS: simulator only, on an Apple Silicon Mac

This download cannot be installed on a physical iPhone or iPad. It requires an
Apple Silicon Mac with Xcode and an installed iOS simulator runtime.

Extract `MusicPlayer2-iOS-Simulator-arm64.tar.gz`, start an iPhone simulator from
Xcode, then drag `MusicPlayer2-Avalonia.iOS.app` onto the simulator. Alternatively:

```sh
xcrun simctl install booted MusicPlayer2-Avalonia.iOS.app
xcrun simctl launch booted io.github.victorlcosta.musicplayer2
```

Physical iPhone testing needs a separate Apple signing/provisioning or TestFlight
distribution setup. An unsigned IPA would not make this simulator build
installable on a phone.
