![ModernFlyouts](ModernFlyouts/Assets/Images/Readme_Banner.png)

# ModernFlyouts

A modern, Fluent Design replacement for the Windows volume, brightness, media and airplane-mode flyouts.

[![Build](https://github.com/hasan-ismail/ModernFlyouts/actions/workflows/build.yml/badge.svg)](https://github.com/hasan-ismail/ModernFlyouts/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/hasan-ismail/ModernFlyouts?logo=github)](https://github.com/hasan-ismail/ModernFlyouts/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/hasan-ismail/ModernFlyouts/total?logo=github)](https://github.com/hasan-ismail/ModernFlyouts/releases)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

![Overview](docs/images/Overview.png)

Press a volume, media or brightness key and Windows shows a small on-screen popup. ModernFlyouts
replaces it with a much better one — real media controls, a proper volume slider, per-monitor
brightness, and flyouts for the lock keys.

Your built-in flyout isn't modified. It's hidden while ModernFlyouts is running, and Windows goes
straight back to normal when you quit.

> **This is an actively maintained continuation of the project.** The original stopped receiving
> updates and its last release could no longer even be installed — its signing certificate had
> expired. This version is rebuilt on .NET 9, fixes the detection that broke on recent Windows 11
> builds, and ships a normal installer. See [what's changed](FORK_CHANGES.md).

## Install

Download the latest build from the [Releases page](https://github.com/hasan-ismail/ModernFlyouts/releases/latest).

| | |
| --- | --- |
| **Installer** (recommended) | `ModernFlyouts-Setup-x64.exe` — installs, adds a Start Menu entry, optional start-with-Windows, and uninstalls cleanly |
| **Portable** | `ModernFlyouts-portable-win-x64.zip` — unzip and run, nothing is written outside the folder |
| **ARM devices** | Use the `arm64` build (Surface Pro X, Snapdragon laptops) |

No certificate to install, and no separate .NET download — the runtime is bundled.

The app runs in your system tray. Double-click the icon for settings, or right-click for Settings
and Exit.

**Requires Windows 10 1809 or newer, or Windows 11.**

## Features

**Flyouts**

- **Volume** — a real slider, click the icon to mute, scroll the slider to adjust
- **Media** — play/pause, next/previous, shuffle, repeat, stop, and a seekable timeline
- **Brightness** — per-monitor on multi-monitor setups, including external displays over DDC/CI
- **Airplane mode**
- **Lock keys** — Caps Lock, Num Lock, Scroll Lock and Insert/Overtype

**Appearance**

- Light and dark themes, following Windows or pinned to one
- Adjustable background opacity
- Smooth open/close animations, which can be turned off
- Drag the flyout anywhere — it remembers where you put it
- Choose which monitor it shows on, or place it manually
- Configurable timeout, alignment, and content stacking direction
- Show, hide or pin the flyout's top bar; optional coloured tray icon

**Behaviour**

- Every module can be turned off individually — use just the ones you want
- Or turn the whole thing off and get the Windows flyouts back, without uninstalling
- Start with Windows, optional
- Translated into 30+ languages

Media controls depend on what the playing app reports to Windows. See
[which players support what](docs/GSMTC-Support-And-Popular-Apps.md).

> There's no flyout for keyboard backlight or the Fn key. Those aren't key presses — they're hardware
> signals handled by your OEM's driver, so no application can see them.

## Privacy

No analytics, no crash reporting, no telemetry, no network requests. Settings stay on your machine.
See [Privacy.md](Privacy.md).

## Building

You need the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). Visual Studio is not
required.

```bash
git clone https://github.com/hasan-ismail/ModernFlyouts.git
cd ModernFlyouts

# run it
dotnet run --project ModernFlyouts/ModernFlyouts.csproj -p:Platform=x64

# or build the portable output the releases use
dotnet publish ModernFlyouts/ModernFlyouts.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o out
```

## Contributing

Bug reports and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md). When reporting a
problem, please include your Windows build number (`winver`); it matters more than you'd think for
this app.

## Credits

Originally created by [ShankarBUS](https://github.com/ShankarBUS/) and the ModernFlyouts
contributors, and built on [AudioFlyout](https://github.com/ADeltaX/AudioFlyout) by
[ADeltaX](https://github.com/ADeltaX/), whose work made the whole thing possible.

Uses [NAudio](https://github.com/naudio/NAudio),
[ModernWpf](https://github.com/Kinnara/ModernWpf) and
[Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon).
Third-party licences are listed in [NOTICE.md](NOTICE.md).

## License

[MIT](LICENSE).
