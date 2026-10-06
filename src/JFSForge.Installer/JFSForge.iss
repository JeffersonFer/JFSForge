[Setup]
AppId={{a74e0b02-b4d1-4b39-aff1-5e8fa4073502}
AppName=JFS Forge
AppVersion=0.5.0
AppPublisher=Jefferson Fernando Santana
DefaultDirName={autopf}\JFSForge
DisableProgramGroupPage=yes
OutputDir=..\..\dist\Installer
OutputBaseFilename=JFSForge-Setup-0.5.0
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
SetupIconFile=..\..\resources\icons\jfsForgeIcon.ico
UninstallDisplayIcon={app}\jfsForgeIcon.ico
WizardImageFile=..\..\resources\icons\JFSForgeBanner.bmp
InfoBeforeFile=..\..\resources\notas-antes.txt
InfoAfterFile=..\..\resources\notas-depois.txt

[Files]
; Revit 2025 - DLLs (ProgramData)
Source: "..\JFSForge.Revit\bin\x64\Release R25\net8.0-windows\*"; DestDir: "{commonappdata}\JFSForge\DLLs\2025"; Flags: recursesubdirs ignoreversion
; Revit 2026 - DLLs (ProgramData)
Source: "..\JFSForge.Revit\bin\x64\Release R26\net8.0-windows\*"; DestDir: "{commonappdata}\JFSForge\DLLs\2026"; Flags: recursesubdirs ignoreversion
; Revit 2027 - DLLs (ProgramData - local das DLLs não muda, só o manifesto)
Source: "..\JFSForge.Revit\bin\x64\Release R27\net10.0-windows\*"; DestDir: "{commonappdata}\JFSForge\DLLs\2027"; Flags: recursesubdirs ignoreversion
; Manifesto .addin - 2025 (ProgramData)
Source: "..\..\resources\addins\JFSForge.2025.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion
; Manifesto .addin - 2026 (ProgramData)
Source: "..\..\resources\addins\JFSForge.2026.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2026"; Flags: ignoreversion
; Manifesto .addin - 2027 (Program Files - mudança de segurança do Revit 2027)
Source: "..\..\resources\addins\JFSForge.2027.addin"; DestDir: "{commonpf}\Autodesk\Revit\Addins\2027"; Flags: ignoreversion
; Ícone (usado na desinstalação)
Source: "..\..\resources\icons\jfsForgeIcon.ico"; DestDir: "{app}"; Flags: ignoreversion
; WebView2 Runtime Bootstrapper (instalado apenas se necessário, ver [Run])
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Instalando componente necessário (WebView2 Runtime)..."; Check: not IsWebView2Installed; Flags: waituntilterminated

[UninstallDelete]
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs\2025"
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs\2026"
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs\2027"
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs"
Type: dirifempty; Name: "{commonappdata}\JFSForge"

[InstallDelete]
Type: filesandordirs; Name: "{commonappdata}\JFSForge\DLLs\2025\WebUI"
Type: filesandordirs; Name: "{commonappdata}\JFSForge\DLLs\2026\WebUI"
Type: filesandordirs; Name: "{commonappdata}\JFSForge\DLLs\2027\WebUI"

[Code]
function IsWebView2Installed: Boolean;
var
  Version: String;
begin
  Result :=
    RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version)
    or RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version);
end;