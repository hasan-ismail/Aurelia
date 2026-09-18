![ModernFlyouts](ModernFlyouts/Assets/Images/Readme_Banner.png)

# ModernFlyouts

A modern, Fluent Design replacement for the Windows volume, brightness, media and airplane-mode flyouts.

[![Build](https://github.com/hasan-ismail/ModernFlyouts/actions/workflows/build.yml/badge.svg)](https://github.com/hasan-ismail/ModernFlyouts/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/hasan-ismail/ModernFlyouts?logo=github)](https://github.com/hasan-ismail/ModernFlyouts/releases/latest)

![Overview](docs/images/Overview.png)

Press a volume, media or brightness key and Windows shows a small on-screen popup. ModernFlyouts
replaces it with a nicer one that also gives you media controls, a proper volume slider, per-monitor
brightness, and flyouts for the lock keys.

Your built-in flyout isn't modified — it's just hidden while ModernFlyouts is running. Quit the app
and Windows goes back to normal.

## Download

Grab the latest zip from the [Releases page](https://github.com/hasan-ismail/ModernFlyouts/releases/latest):

| Your PC | File |
| --- | --- |
| Most PCs (Intel/AMD) | `ModernFlyouts-portable-win-x64.zip` |
| ARM devices (Surface Pro X, Snapdragon laptops) | `ModernFlyouts-portable-win-arm64.zip` |

Unzip it anywhere and run **`ModernFlyouts.exe`**. That's it — nothing to install, no certificate to
trust, and no .NET download.

The app sits in your system tray. Double-click the icon to open settings, or right-click it for
Settings and Exit. To launch it automatically with Windows, turn on **Run at startup** in the
General settings page.

**Requires Windows 10 1809 or newer, or Windows 11.**

## Features

- Volume flyout with a real slider, plus media controls (play/pause, next/previous, shuffle, repeat, stop, timeline)
- Brightness flyout, with per-monitor control on multi-monitor setups
- Airplane-mode flyout
- Lock-key flyouts — Caps Lock, Num Lock, Scroll Lock and Insert/Overtype
- Light and dark themes
- Drag the flyout anywhere; it remembers where you put it
- Pick which monitor it appears on
- Adjustable opacity, timeout and layout
- Every module can be turned off individually

Media controls depend on what the app you're playing from reports to Windows. See
[which players support what](docs/GSMTC-Support-And-Popular-Apps.md).

> There's no flyout for keyboard backlight or the Fn key — those aren't key presses, they're hardware
> signals handled by your OEM's driver, so no application can see them.

## About this fork

The [original project](https://github.com/ModernFlyouts-Community/ModernFlyouts) is no longer
maintained, and its last release shipped as a signed MSIX whose certificate has since expired — which
is why installing it stopped working for most people.

This fork fixes that by shipping a plain portable build instead, and repairs the flyout detection that
broke on recent Windows 11 builds. See [FORK_CHANGES.md](FORK_CHANGES.md) for the details.

## Building from source

You need the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). Visual Studio is not
required.

```bash
git clone https://github.com/hasan-ismail/ModernFlyouts.git
cd ModernFlyouts

# run it
dotnet run --project ModernFlyouts/ModernFlyouts.csproj -p:Platform=x64

# or produce the same portable build the releases use
dotnet publish ModernFlyouts/ModernFlyouts.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o out
```

The `ModernFlyoutsBridge`, `ModernFlyoutsHost` and `ModernFlyouts.Package` projects are only needed to
produce an MSIX package. The portable build doesn't use them, and you can ignore them unless you're
packaging for the Store.

## Credits

Built on [ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts), which in turn grew
out of [AudioFlyout](https://github.com/ADeltaX/AudioFlyout) by [ADeltaX](https://github.com/ADeltaX/).

Uses [NAudio](https://github.com/naudio/NAudio),
[ModernWpf](https://github.com/Kinnara/ModernWpf) and
[Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon).

## License

[MIT](LICENSE).
