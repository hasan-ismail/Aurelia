# What's changed

This is a maintained continuation of
[ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts), which stopped receiving
updates. Everything below was verified on **Windows 11 build 26200 (25H2), x64**, on .NET 9.

---

## Distribution

### You can install it again

**Original issues:** [#1507](https://github.com/ModernFlyouts-Community/ModernFlyouts/issues/1507),
[#1494](https://github.com/ModernFlyouts-Community/ModernFlyouts/issues/1494),
[#1471](https://github.com/ModernFlyouts-Community/ModernFlyouts/issues/1471)

The only download used to be a signed MSIX bundle. Its certificate expired, so installing it simply
failed — people were rolling their system clock back a month to get it to validate.

There are now two artifacts, neither of which needs a certificate or a .NET install:

- **`ModernFlyouts-Setup-<arch>.exe`** — a per-user installer (Inno Setup). Installs without a UAC
  prompt, adds a Start Menu entry, optionally starts with Windows, and uninstalls cleanly.
- **`ModernFlyouts-portable-<rid>.zip`** — unzip and run.

Both are built for `x64` and `arm64` by [GitHub Actions](.github/workflows/build.yml) and attached
to each release automatically. The project previously built only through Azure Pipelines and had no
GitHub Actions workflows at all.

---

## Bug fixes

### Settings were never saved

**Original issue:** [#1483](https://github.com/ModernFlyouts-Community/ModernFlyouts/issues/1483)

Every setting was persisted through `ApplicationData.Current.LocalSettings`, which only works inside
an MSIX package. Run unpackaged and it throws `InvalidOperationException` — and since every call site
wrapped it in `catch { }`, the failure was invisible: reads silently returned defaults and writes went
nowhere. Settings appeared to reset on every launch.

Packaged builds still use `ApplicationData` so existing installs keep their settings. Unpackaged
builds now use a small JSON file at `%LOCALAPPDATA%\ModernFlyouts\settings.json`, written atomically
via a temporary file, with writes coalesced so dragging a slider doesn't hammer the disk, and flushed
on exit.

### "Run at startup" did nothing

Same root cause, different API: `StartupHelper` used `Windows.ApplicationModel.StartupTask`, which is
also MSIX-only. Unpackaged, the setter did nothing and the getter's `catch { return true; }` reported
the feature as *enabled* regardless — so the toggle looked stuck on and never worked.

Packaged builds keep `StartupTask`. Unpackaged builds use the per-user `Run` registry key, which is
the same value the installer's "start with Windows" checkbox writes, so the two stay in sync. The
uninstaller always clears it, including when the app rather than the installer created it.

### The on-screen display was never re-detected on Windows 11

Windows 11 22H2 rebuilt the volume/brightness OSD on XAML islands, renaming its host window class from
`NativeHWNDHost` to `XamlExplorerHostIslandWindow`. `GetAllInfos()` was updated for that. **`WinEventProc()`
was not**, and still compared against the old literal:

```csharp
if (GetWindowClassName(hWnd) == "NativeHWNDHost")   // never true on Windows 11
```

That is the path that re-acquires the OSD after it is destroyed — and Windows creates that window
lazily and tears it down when idle, so it happens routinely, not just at login. Once it failed, the
handler never found the OSD again.

Both class names now come from one place, so they can't drift apart again. Verified with a global
`SetWinEventHook`: the live OSD host is `XamlExplorerHostIslandWindow`, band 18, which is what the
fixed code resolves.

### Windows build detection could crash or pick the wrong OSD

The build number came from parsing `RuntimeInformation.OSDescription`:

```csharp
String build = RuntimeInformation.OSDescription.Substring(RuntimeInformation.OSDescription.LastIndexOf('.') + 1);
int buildNumber = int.Parse(build);
```

On a description carrying a revision — `Microsoft Windows 10.0.26100.1742` — that yields `1742`, which
is below the 22H2 threshold, so the pre-22H2 OSD layout was selected and the flyout was never found.
Any unexpected format threw outright, during flyout-handler initialisation.

Replaced with `Environment.OSVersion.Version.Build`, which is backed by `RtlGetVersion`.

### Thumbnails decoded from the wrong offset

`BitmapHelper` called `stream.Seek(0, SeekOrigin.Current)` — zero bytes from the *current* position,
which does nothing. The intent was to rewind, so any already-read stream decoded from the middle of
the image. Now seeks to `Begin`, handles non-seekable streams, freezes the decoded frame for the UI
thread, and no longer lets a malformed thumbnail escape as an exception.

### A WNF retry that could never run

```csharp
if (status == 0xC0000023)   // int vs uint literal: always false
```

`STATUS_BUFFER_TOO_SMALL` doesn't fit in an `int`, so this was constant-false — the compiler had been
reporting it as `CS0652`. The buffer-growth retry never ran and an undersized buffer was read as if the
call had succeeded. Now compares against the `unchecked` value, breaks on success, and refuses to read
the buffer on any other status.

---

## Modernisation

### Glass effect

The flyout can now render as a translucent glass surface with a specular sheen and a lit rim, instead
of a flat opaque panel. It's on by default and there's a **Glass effect** toggle in
Settings → Personalization. Windows 11 rounded corners are applied to the flyout window.

The glass is drawn in XAML rather than using the compositor's acrylic blur, and that was a deliberate
choice after trying both. The accent-policy blur applies to the **whole window rectangle**, and this
window is much larger than the cards you see — the surplus is the transparent margin the drop shadow
needs. Enabling it turned that margin into a dark blurred halo and hid the wallpaper around the cards,
which looked worse than no effect. The XAML surface clips correctly to each card's rounded rectangle.

The trade-off is that translucency without blur lets sharp content behind show through, so the surface
is kept at 80% of the configured opacity — glassy, but still legible over a busy background. Turn the
toggle off for the original flat look.

### Removed Visual Studio App Center

Release builds started the App Center SDK on launch. Microsoft **retired App Center on 31 March 2025**,
so this was a failed network round-trip at every startup reporting to a service that no longer exists.
Removed, along with its three packages. The app now makes no network requests at all —
see [Privacy.md](Privacy.md).

Also dropped `Microsoft.Services.Store.SDK`, a 2017 package whose every usage was already commented out,
and the in-app "Rate and review" button that opened the original project's Microsoft Store listing.

---

## Lighter

A self-contained build went from **200 MB to 167 MB** (~17% smaller); the installer is 56 MB.

The win came from the `NAudio` meta-package, which depends on `NAudio.WinForms` and so dragged the
entire **Windows Forms framework reference** — `System.Windows.Forms`, `.Design` and `.Primitives`,
about 23 MB — into every publish. The app only ever used `NAudio.CoreAudioApi`, so it now references
`NAudio.Wasapi` directly. No Windows Forms assembly ships any more.

The remainder was dependency cleanup: a duplicate `CommunityToolkit.Mvvm` reference (`NU1504`), and
`System.Drawing.Common`/`System.Management` pinned to .NET 9 *release-candidate* builds, now on stable
9.0.0.

`RetainVMGarbageCollection` is disabled so freed segments go back to the OS. Note that GC tuning did
**not** meaningfully move the footprint — measured, it was within noise, so the more aggressive option
of disabling background GC was rejected: it risks a blocking collection stalling a flyout that has to
appear within ~50 ms, and there was no measured benefit to pay for that.

---

## Known limitations

- Verified on Windows 11 26200 x64. The `arm64` artifacts are built by CI but have not been run on an
  ARM64 device.
- Memory use is essentially unchanged (~230 MB working set). That's WPF plus the Windows SDK
  projection, and reducing it would need trimming, which WPF doesn't support.
- The MSIX packaging project and the two C++ projects are untouched and still need Visual Studio with
  the C++ and packaging workloads. Neither the installer nor the portable build uses them.
- Several reports from the original project — invisible flyouts on particular GPU/driver combinations,
  displays switching off on media keys, ExplorerPatcher interactions — could not be reproduced here and
  are not addressed.
