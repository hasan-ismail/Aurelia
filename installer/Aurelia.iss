; Inno Setup script for Aurelia.
;
; Produces a normal, self-contained .exe installer. The payload is the output of
;   dotnet publish -r <rid> --self-contained true
; so there is no .NET prerequisite and nothing needs to be code-signed for the user
; to be able to install it.
;
; Build with:
;   iscc /DAppVersion=0.10.1 /DArch=x64 /DPayloadDir=..\publish\win-x64 Aurelia.iss

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef Arch
  #define Arch "x64"
#endif
#ifndef PayloadDir
  #define PayloadDir "..\publish\win-" + Arch
#endif

#define AppName       "Aurelia"
#define AppPublisher  "hasan-ismail"
#define AppUrl        "https://github.com/hasan-ismail/Aurelia"
#define AppExe        "AureliaFlyouts.exe"
; What the program file was called before 0.17.0. See [InstallDelete].
#define OldAppExe     "Aurelia.exe"

[Setup]
; The installer's identity. Never change it, or upgrades stop recognising old installs.
; InitializeSetup repeats it to find the uninstall entry.
AppId={{7C864741-5192-4753-B124-8599487283BC}
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
SetupIconFile=..\Aurelia\Assets\Logo.ico
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

[InstallDelete]
; 0.17.0 renamed the program file from Aurelia.exe to AureliaFlyouts.exe, because
; Discord lists an unrelated game as "aurelia/aurelia.exe" and was showing anyone
; running Aurelia as playing it. Upgrading would otherwise leave the old program
; beside the new one, still matching that entry. Aurelia.Core.* is a different
; library and is replaced by [Files] as usual.
Type: files; Name: "{app}\{#OldAppExe}"
Type: files; Name: "{app}\Aurelia.dll"
Type: files; Name: "{app}\Aurelia.dll.config"
Type: files; Name: "{app}\Aurelia.pdb"
Type: files; Name: "{app}\Aurelia.deps.json"
Type: files; Name: "{app}\Aurelia.runtimeconfig.json"

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Only registers the value for removal at uninstall. Whether it is set while Aurelia is
; installed is decided in [Code], so an upgrade can respect a choice the user made
; inside the app after the previous install.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: none; ValueName: "{#AppName}"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName} now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The app is a tray app with no main window, so the restart manager will not close
; it for us. Stop it before removing the files.
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExe}"; Flags: runhidden; RunOnceId: "StopAurelia"

[Code]
const
  RunKey = 'Software\Microsoft\Windows\CurrentVersion\Run';

var
  // Captured before anything is installed: by the time the startup choice is applied,
  // the uninstall entry exists even on a first install.
  IsUpgrade: Boolean;
  HadRunAtStartup: Boolean;
  StartupDefaultApplied: Boolean;

function InitializeSetup(): Boolean;
var
  Value: String;
begin
  IsUpgrade := RegKeyExists(HKCU,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{7C864741-5192-4753-B124-8599487283BC}_is1');
  HadRunAtStartup := RegQueryStringValue(HKCU, RunKey, '{#AppName}', Value) and (Value <> '');
  Result := True;
end;

// On an upgrade, start the "Start when I sign in" box from what is actually in effect.
// The user may have changed it inside the app since the last install, and the box Inno
// remembers from that install would otherwise quietly undo it.
procedure CurPageChanged(CurPageID: Integer);
begin
  if (CurPageID = wpSelectTasks) and IsUpgrade and not StartupDefaultApplied then
  begin
    StartupDefaultApplied := True;
    if HadRunAtStartup then
      WizardSelectTasks('startup')
    else
      WizardSelectTasks('!startup');
  end;
end;

function StartupWanted(): Boolean;
begin
  // A silent upgrade shows no checkbox, so keep whatever was already in effect.
  if WizardSilent() and IsUpgrade then
    Result := HadRunAtStartup
  else
    Result := WizardIsTaskSelected('startup');
end;

// Stop every copy running from the install folder, under the current program name and
// the pre-0.17.0 one. A copy left running keeps its files locked, and because it holds
// the single-instance lock, the new version would hand over to it and exit. Matching on
// the folder as well as the name means nothing unrelated that happens to be called
// Aurelia.exe is touched.
procedure StopRunningCopies();
var
  Locator, Service, Processes, Process, Path: Variant;
  AppDir: String;
  I, ResultCode: Integer;
begin
  AppDir := Lowercase(AddBackslash(ExpandConstant('{app}')));
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('.', 'root\CIMV2');
    Processes := Service.ExecQuery(
      'SELECT * FROM Win32_Process WHERE Name = ''{#AppExe}'' OR Name = ''{#OldAppExe}''');
    for I := 0 to Processes.Count - 1 do
    begin
      Process := Processes.ItemIndex(I);
      Path := Process.ExecutablePath;
      if not VarIsNull(Path) then
        if Pos(AppDir, Lowercase(Path)) = 1 then
          Process.Terminate(0);
    end;
  except
    // Without WMI, fall back to stopping the current program by name, as before.
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#AppExe}', '',
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
  // Termination is asynchronous; give the file handles a moment to close.
  Sleep(700);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopRunningCopies();
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  // The same value the app's own "Run at startup" setting writes, so the two agree.
  if CurStep = ssPostInstall then
  begin
    if StartupWanted() then
      RegWriteStringValue(HKCU, RunKey, '{#AppName}',
        '"' + ExpandConstant('{app}\{#AppExe}') + '"')
    else
      RegDeleteValue(HKCU, RunKey, '{#AppName}');
  end;
end;
