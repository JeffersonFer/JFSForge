[Setup]
AppId={{a74e0b02-b4d1-4b39-aff1-5e8fa4073502}
AppName=JFS Forge
AppVersion=0.1.0
AppPublisher=Jefferson Fernando Santana
DefaultDirName={autopf}\JFSForge
DisableProgramGroupPage=yes
OutputDir=..\..\dist\Installer
OutputBaseFilename=JFSForge-Setup-0.1.0
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Files]
Source: "..\JFSForge.Revit\bin\x64\Release R25\net8.0-windows\*"; DestDir: "{commonappdata}\JFSForge\DLLs\2025"; Flags: recursesubdirs ignoreversion
Source: "..\JFSForge.Revit\bin\x64\Release R26\net8.0-windows\*"; DestDir: "{commonappdata}\JFSForge\DLLs\2026"; Flags: recursesubdirs ignoreversion
Source: "..\JFSForge.Revit\bin\x64\Release R27\net10.0-windows\*"; DestDir: "{commonappdata}\JFSForge\DLLs\2027"; Flags: recursesubdirs ignoreversion

Source: "..\..\resources\addins\JFSForge.2025.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion
Source: "..\..\resources\addins\JFSForge.2026.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2026"; Flags: ignoreversion
Source: "..\..\resources\addins\JFSForge.2027.addin"; DestDir: "{commonpf}\Autodesk\Revit\Addins\2027"; Flags: ignoreversion

[UninstallDelete]
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs\2025"
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs\2026"
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs\2027"
Type: dirifempty; Name: "{commonappdata}\JFSForge\DLLs"
Type: dirifempty; Name: "{commonappdata}\JFSForge"
