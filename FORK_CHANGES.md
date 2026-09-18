# Fork changes

Upstream [ModernFlyouts-Community/ModernFlyouts](https://github.com/ModernFlyouts-Community/ModernFlyouts)
stopped being maintained. This fork picks it up to keep the app working on current Windows 11 builds.

Everything below was verified on **Windows 11 build 26200 (25H2), x64**, with .NET 9.

---

## 1. You can install it again (portable build, no certificate)

**Upstream issues:** [#1507](https://github.com/ModernFlyouts-Community/ModernFlyouts/issues/1507),
[#1494](https://github.com/ModernFlyouts-Community/ModernFlyouts/issues/1494),
[#1471](https://github.com/ModernFlyouts-Community/ModernFlyouts/issues/1471)

Upstream shipped only a signed MSIX bundle. Its signing certificate expired, so installing it started
failing outright — people were resorting to rolling their system clock back a month to get the
certificate to validate.

This fork adds a GitHub Actions workflow ([`.github/workflows/build.yml`](.github/workflows/build.yml))
that publishes a **self-contained portable build** for `win-x64` and `win-arm64`. Unzip, run
`ModernFlyouts.exe`. No MSIX, no certificate, no .NET runtime install. Code signing simply stops being
part of the install path.

The project previously built only through Azure Pipelines; there were no GitHub Actions workflows at all.

## 2. The OSD was never re-detected on Windows 11

**File:** [`ModernFlyouts.Core/Interop/NativeFlyoutHandler.cs`](ModernFlyouts.Core/Interop/NativeFlyoutHandler.cs)

Windows 11 22H2 rebuilt the volume/brightness OSD on XAML islands, renaming its host window class from
`NativeHWNDHost` to `XamlExplorerHostIslandWindow`. `GetAllInfos()` was updated for that. **`WinEventProc()`
was not** — it still compared against the literal `"NativeHWNDHost"`:

```csharp
if (GetWindowClassName(hWnd) == "NativeHWNDHost")   // never true on Windows 11
```

That check is the path that re-detects the OSD after it is destroyed. Windows creates the OSD host lazily
and tears it down again when idle, so this fires routinely — not just at login. Once it failed, the handler
never re-acquired the window, so `NativeFlyoutShown` stopped firing and the hook manager never saw the new
window.

The two class names are now derived from one place and shared by both call sites, so they cannot drift apart
again.

Measured on this machine, with a global `SetWinEventHook` watching the OSD appear:

```
NATIVE-OSD SHOW 0x6044C band=18 class='XamlExplorerHostIslandWindow'   <- actual
resolved by fixed code:        'XamlExplorerHostIslandWindow'          <- match
old hardcoded value:           'NativeHWNDHost'                        <- no match
```

## 3. Windows build number was parsed in a way that can crash or silently pick the wrong OSD

**File:** [`ModernFlyouts.Core/Helpers/OSVersionHelper.cs`](ModernFlyouts.Core/Helpers/OSVersionHelper.cs) (new)

The build number was obtained by taking everything after the last `.` of `RuntimeInformation.OSDescription`
and calling `int.Parse` on it:

```csharp
String build = RuntimeInformation.OSDescription.Substring(RuntimeInformation.OSDescription.LastIndexOf('.') + 1);
int buildNumber = int.Parse(build);
```

Two failure modes:

- When the description carries a revision — `Microsoft Windows 10.0.26100.1742` — this yields `1742`, not
  `26100`. `1742 < 22620`, so the code picks the **pre-22H2 OSD layout** and never finds the flyout.
- Any unexpected format throws, during `NativeFlyoutHandler.Initialize()`, i.e. a crash on launch.

Replaced with `Environment.OSVersion.Version.Build`, which on .NET is backed by `RtlGetVersion` and is not
affected by the app compatibility manifest.

## 4. Thumbnail decoding started from the wrong stream offset

**File:** [`ModernFlyouts.Core/Helpers/BitmapHelper.cs`](ModernFlyouts.Core/Helpers/BitmapHelper.cs)

```csharp
stream.Seek(0, SeekOrigin.Current);   // seeks 0 bytes from the current position: a no-op
```

The intent was to rewind. If the stream had already been read from, the decoder began mid-image. Now seeks to
`SeekOrigin.Begin`, handles non-seekable streams, freezes the decoded frame so it is safe to hand to the UI
thread, and no longer lets a malformed thumbnail escape as an exception.

## 5. A WNF retry that could never run

**File:** [`ModernFlyouts.Core/Interop/Wnf.cs`](ModernFlyouts.Core/Interop/Wnf.cs)

```csharp
if (status == 0xC0000023)   // int vs. uint literal: always false
    continue;
```

`0xC0000023` (`STATUS_BUFFER_TOO_SMALL`) does not fit in an `int`, so this comparison was constant-false — the
compiler had been flagging it as `CS0652`. The buffer-growth retry never ran, and an undersized buffer was read
as though the call had succeeded. The loop also never stopped early on success, re-querying all 10 times.

Now compares against `unchecked((int)0xC0000023)`, breaks on success, and refuses to read the buffer on any
other failure status.

## 6. Removed Visual Studio App Center

**File:** [`ModernFlyouts/Program.cs`](ModernFlyouts/Program.cs)

Release builds started the App Center SDK on launch. Microsoft **retired App Center on 31 March 2025**, so this
was a failed network round-trip at every startup, reporting to a service that no longer exists. Removed, along
with the three `Microsoft.AppCenter.*` packages.

Also dropped `Microsoft.Services.Store.SDK` (a 2017 package whose every usage in the codebase was already
commented out).

## 7. Build hygiene

- `ModernFlyouts.Core.csproj` declared **`CommunityToolkit.Mvvm` twice** (`8.2.1` and `8.3.2`) — `NU1504`. Removed the duplicate.
- `System.Drawing.Common` and `System.Management` were pinned to **.NET 9 release-candidate** builds
  (`9.0.0-rc.2.*`). Moved to the stable `9.0.0` releases.

The build now completes with no errors and no `CS`/`NU` warnings beyond pre-existing style ones.

---

## Known limitations

- Fixes were verified on Windows 11 26200 x64. The `win-arm64` artifact is built by CI but has not been
  run on an ARM64 device.
- The MSIX packaging project (`ModernFlyouts.Package`) and the two C++ projects are untouched and still
  require Visual Studio with the C++ and packaging workloads. The portable build does not use them.
- Several upstream reports (invisible flyouts on specific GPU/driver setups, display-off on media keys,
  ExplorerPatcher interactions) could not be reproduced on this hardware and are not addressed.
