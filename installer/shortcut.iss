[Setup]
AppId={{0C9E8D4A-9B49-4B0E-9A56-9B0CF69C1BBA}
AppName=Shortcut Proxy Switcher
AppVersion=1.0.0
AppPublisher=hyang0
DefaultDirName={autopf}\ShortcutProxySwitcher
DefaultGroupName=Shortcut Proxy Switcher
DisableDirPage=no
DisableProgramGroupPage=no
OutputBaseFilename=ShortcutProxySwitcherSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "chinese"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "在桌面创建快捷方式"; Flags: unchecked

[Files]
Source: "..\bin\Release\net10.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Shortcut Proxy Switcher"; Filename: "{app}\shortcut.exe"
Name: "{commondesktop}\Shortcut Proxy Switcher"; Filename: "{app}\shortcut.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\shortcut.exe"; Description: "安装完成后立即运行"; Flags: nowait postinstall skipifsilent
