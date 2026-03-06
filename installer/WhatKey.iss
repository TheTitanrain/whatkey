#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

#define AppName    "WhatKey"
#define AppExeName "WhatKey.exe"
#define SourceDir  "..\bin\Release\net48"
#define OutputDir  "output"

[Setup]
AppId={{A7F3D2B1-4E5C-4A8D-B9F0-1C2D3E4F5A6B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=WhatKey Contributors
AppPublisherURL=https://github.com/lehachev/whatkey
AppSupportURL=https://github.com/lehachev/whatkey
AppUpdatesURL=https://github.com/lehachev/whatkey
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=WhatKeySetup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
MinVersion=6.1sp1
ArchitecturesInstallIn64BitMode=x64compatible arm64
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start WhatKey automatically with Windows"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\WhatKey.exe";                           DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\WhatKey.exe.config";                    DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Hardcodet.Wpf.TaskbarNotification.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.Text.Json.dll";                        DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.Text.Encodings.Web.dll";              DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.Buffers.dll";                         DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.Memory.dll";                          DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.Numerics.Vectors.dll";                DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.IO.Pipelines.dll";                    DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.Threading.Tasks.Extensions.dll";      DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\Microsoft.Bcl.AsyncInterfaces.dll";          DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\System.ValueTuple.dll";                      DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}";   Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}";     Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,WhatKey}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "taskkill.exe"; Parameters: "/f /im {#AppExeName}"; RunOnceId: "KillBeforeUninstall"; Flags: runhidden skipifdoesntexist

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{userappdata}\WhatKey');
    if DirExists(DataDir) then
    begin
      if MsgBox('Delete your saved hotkeys and settings?' + #13#10 + DataDir,
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
        DelTree(DataDir, True, True, True);
    end;
  end;
end;
