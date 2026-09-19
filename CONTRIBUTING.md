# Contributing

Contributions are welcome — bug reports, fixes, features and translations all help.

## Reporting a bug

Before opening an issue, please:

1. Make sure you're on the [latest release](https://github.com/hasan-ismail/ModernFlyouts/releases/latest).
   A lot of the reports on the original project were fixed versions ago.
2. [Search existing issues](https://github.com/hasan-ismail/ModernFlyouts/issues) so we don't end up
   with duplicates.

Then [open an issue](https://github.com/hasan-ismail/ModernFlyouts/issues/new/choose) and include:

- Your Windows version — `winver` gives you the build number, which matters a lot for this app
- What you expected to happen, and what actually happened
- Steps to reproduce it
- A screenshot or short clip if it's a visual problem

Windows build numbers genuinely matter here: Microsoft has changed the on-screen display twice
(22H2 and again since), and most "it doesn't work" reports come down to those changes.

## Making a change

Fork the repo, make your change on a branch, and open a pull request.

For anything non-trivial, open an issue first so we can agree on the approach before you spend time
on it.

Keep pull requests focused — one logical change per PR is much easier to review than a large mixed
one.

## Building

You need the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0). Visual Studio is not
required.

```bash
dotnet build ModernFlyouts/ModernFlyouts.csproj -p:Platform=x64
dotnet run   --project ModernFlyouts/ModernFlyouts.csproj -p:Platform=x64
```

If you're changing how the flyout is detected or positioned, please say which Windows build you
tested on — that's the part most likely to behave differently across versions.

The `ModernFlyoutsBridge`, `ModernFlyoutsHost` and `ModernFlyouts.Package` projects are only used for
MSIX packaging and need Visual Studio with the C++ and packaging workloads. Normal builds and the
shipped installer don't touch them.

## Translations

Translations live in [`ModernFlyouts/MultilingualResources`](ModernFlyouts/MultilingualResources) as
`.xlf` files, with the source strings in
[`ModernFlyouts/Properties/Strings.resx`](ModernFlyouts/Properties/Strings.resx).

To fix or improve an existing language, edit its `.xlf` file and open a PR. To add a new one, open an
issue and I'll generate the file for you.

## Code style

Match the surrounding code. Nothing more formal than that — the codebase is fairly consistent
already, so following what's nearby is enough.
