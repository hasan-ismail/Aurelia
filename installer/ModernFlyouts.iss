; Inno Setup script for ModernFlyouts.
;
; Produces a normal, self-contained .exe installer. The payload is the output of
;   dotnet publish -r <rid> --self-contained true
; so there is no .NET prerequisite and nothing needs to be code-signed for the user
; to be able to install it.
;
; Build with:
;   iscc /DAppVersion=0.10.1 /DArch=x64 /DPayloadDir=..\publish\win-x64 ModernFlyouts.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef Arch
  #define Arch "x64"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\publish\win-" + Arch
#endif

#define AppName       "ModernFlyouts"
#define AppPublisher  "hasan-ismail"
#define AppUrl        "https://github.com/hasan-ismail/ModernFlyouts"
#define AppExe        "ModernFlyouts.exe"

[Setup]
AppId={{8E0C2F5A-1C3B-4E7A-9D42-6B1F0A5C7E31}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases

; Per-user install: no UAC prompt, and it matches the per-user HKCU Run key the app
; uses for "start with Windows".
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto
AllowNoIcons=yes

OutputDir=..\artifacts
OutputBaseFilename={#AppName}-Setup-{#Arch}
SetupIconFile=..\ModernFlyouts\Assets\Logo.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes

; The app itself is x64/arm64; keep the installer 32-bit so it runs anywhere, but
; install into the native Program Files.
#if Arch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start {#AppName} when I sign in"; GroupDescription: "Additional options:"
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional options:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Same key and value name the app's own "Run at startup" setting uses, so the
; installer checkbox and the in-app toggle stay in sync rather than fighting.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"""; \
    Flags: uninsdeletevalue; Tasks: startup

; When the box is left unticked, clear any stale value and still register it for
; removal at uninstall. The user can turn "Run at startup" on inside the app, which
; writes this same value, and leaving it behind would point at a deleted executable.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: none; ValueName: "{#AppName}"; \
    Flags: deletevalue uninsdeletevalue; Tasks: not startup

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName} now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The app is a tray app with no main window, so the restart manager will not close
; it for us. Stop it before removing the files.
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe}"; Flags: runhidden; RunOnceId: "StopModernFlyouts"

[Code]
// Stop a running copy before installing over it, otherwise the files are locked.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#AppExe}', '',
       SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
