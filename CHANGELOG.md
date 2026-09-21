# Changelog

All notable changes to Aurelia. Verified on **Windows 11 build 26200 (25H2), x64**, on .NET 9.

## 0.17.0

**Discord no longer thinks Aurelia is a game.** Discord matches running programs against a public
list of detectable games, and that list has an unrelated game called Aurelia registered under
`aurelia.exe` — so anyone with Discord open was shown as playing it. The program file is now named
`AureliaFlyouts.exe`, which is not on the list. Nothing else changes: the app, the tray icon, the
Start Menu entry and your settings are all still Aurelia. If you had added the old name as a game
in Discord's activity settings, you can remove it.

**New theme: Jellyfish.** Aqua-teal glass with jellyfish drifting up through the volume and
brightness sliders. They are drawn as live vector art rather than a video, so they are cheap to
run, they follow whatever accent colour you pick, and they hold still if you have turned Windows
animations off. The media player is left alone — album art is backdrop enough there.

## 0.16.0

The top bar is collapsed by default, so the flyout opens as just the controls. You can still bring
it back from Settings. Added a screenshot gallery of every theme to the README.

## 0.15.0

Relicensed under the **AGPL-3.0**. The MIT notices for the projects Aurelia grew out of are kept in
`NOTICE.md` and continue to govern those portions.

## 0.14.0

Renamed the project to **Aurelia**. Namespaces, assemblies, the installer and the settings location
all moved with it, so settings live at `%LOCALAPPDATA%\Aurelia\settings.json` now and do not carry
over from an earlier install. The installer uses its own product identity, so Aurelia neither
upgrades nor uninstalls anything that was there before.

## 0.13.0

**Accent colour now actually applies.** It had been tinting only the glass sheen, while everything
the eye reads as "the accent" — slider fills, toggles, highlights — comes from ModernWpf's
`SystemAccentColor` family. It now drives `ThemeManager.AccentColor`, and is re-asserted after every
theme update, because ModernWpf recomputes its accent brushes on a theme change and would silently
put the Windows colour back. A "Use the Windows accent colour" toggle keeps the old behaviour as the
default.

The sheen itself stays white on purpose: it is a specular highlight, and tinting it made the glass
look stained rather than lit.

**Theme presets.** Nine looks — Classic, Frost, Midnight, Aurora, Ember, Nord, Rose, Vapor, Mono —
each setting accent, glass opacity, corner roundness and a light/dark preference. A preset writes
into the individual settings rather than locking them, so everything stays adjustable afterwards.
Corner roundness became a setting in its own right.

**A brightness slider that did nothing is gone.** `SetMonitorBrightness` returns success on some
panels — an ASUS Zenbook Duo's second display among them — and then ignores the write, so a
controller was being created for a display that cannot actually be controlled. Controllers are now
probed by nudging brightness one step and reading it back; anything that ignores the write is not
listed. The monitor index badges went too, since they said nothing once a single slider was left.

**The seek bar moves.** Media apps report their timeline only occasionally, often just on seek or
track change, so the bar sat frozen and appeared to jump only when the flyout was re-triggered. Its
position is now advanced from a local clock anchored to the last real report.

**Motion.** The flyout used to hold at zero opacity for 83 ms and then fade in linearly, which is
what made the entrance feel abrupt; it now eases from the first frame and settles up from 0.94
scale. The Windows "Show animations" accessibility setting is honoured — when it is off, animations
are skipped entirely.

The halo added in 0.12 was removed. It read as a smudge rather than a glow.

## 0.12.0

- A highlight that follows the cursor across each flyout card, painted on a non-hit-testable overlay
  that takes its mouse events from the parent so it never swallows clicks meant for the controls.
- Slider thumbs grow under the pointer and press in while dragged.
- Album art is scaled with a high-quality filter instead of nearest-neighbour, which had been
  discarding most of a 544×544 image on its way into a 64 px box.
- The media card's seek bar is always visible. It had been sharing a grid row with the transport
  controls — the grid declared two rows while its children asked for a third — so a chevron had to
  toggle between them. The card gained a real third row instead.
- Brightness can be linked across displays, with a lock button to decouple them.
- Removed the border DWM was drawing around the whole flyout window: asking it to round the window's
  corners also makes it draw the window frame, which on an otherwise-invisible window showed up as a
  rectangle around everything, shadow margin included.

## 0.11.0

- **Settings are saved.** They had been going through `ApplicationData.Current.LocalSettings`, which
  only works inside an MSIX package; unpackaged it throws, and every call site swallowed the
  exception, so reads returned defaults and writes vanished. Unpackaged builds now use a JSON file
  written atomically, with writes coalesced and flushed on exit.
- **"Run at startup" works.** Same root cause, different API — `StartupTask` is also MSIX-only, and
  the getter's `catch { return true; }` reported the feature as enabled regardless. Unpackaged
  builds use the per-user `Run` key, the same value the installer writes.
- A per-user installer (Inno Setup) alongside the portable build, for x64 and arm64, built and
  released by GitHub Actions. Neither needs a certificate or a separate .NET install.
- Glass surface with a specular sheen and rim light, with a toggle.
- Dropped the Windows Forms dependency: the `NAudio` meta-package pulled in `NAudio.WinForms` and
  with it ~23 MB of `System.Windows.Forms` assemblies, for waveform controls this app never used.
  Referencing `NAudio.Wasapi` directly took a self-contained build from 200 MB to 167 MB.
- Removed Visual Studio App Center, which Microsoft retired on 31 March 2025 — release builds had
  been making a failed network call at every startup. The app now makes no network requests at all.

## 0.10.x and earlier

Work inherited from ModernFlyouts. The fixes that mattered most:

- **The on-screen display was never re-detected on Windows 11.** 22H2 rebuilt the OSD on XAML
  islands and renamed its host window class; one code path was updated and the other still compared
  against the old literal. That second path is the one that re-acquires the OSD after Windows tears
  it down, which happens routinely rather than just at login.
- **Windows build detection could crash or pick the wrong layout**, because it parsed everything
  after the last `.` of `RuntimeInformation.OSDescription` — on a string carrying a revision that
  yields the revision, not the build.
- **Thumbnails decoded from the wrong offset**, from a `Seek(0, SeekOrigin.Current)` that was meant
  to rewind and did nothing.
- **A WNF retry that could never run**, comparing an `int` status against a `uint` literal that does
  not fit in one — constant-false, and the compiler had been reporting it as `CS0652`.
