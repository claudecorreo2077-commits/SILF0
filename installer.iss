; Ruta: D:\ARCHIVOS\POTOSI\SILF\installer.iss
; Script de Inno Setup para SILF
; Compilar con: Inno Setup Compiler (Ctrl+F9)

#define MyAppName "SILF"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "SILF - Sistema Integral de Liquidación y Flotación"
#define MyAppExeName "SILF.App.exe"
#define MyAppDescription "Sistema Integral de Liquidación y Flotación"

[Setup]
AppId={{B8F2A3D1-7E4C-4A9B-8D5F-1C2E3F4A5B6C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\installer_output
OutputBaseFilename=SILF_Setup_{#MyAppVersion}
SetupIconFile=SILF.App\Assets\Icons\icono.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} - {#MyAppDescription}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible


[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el &Escritorio"; GroupDescription: "Accesos directos:"
Name: "startmenu"; Description: "Crear acceso en el &Menú Inicio"; GroupDescription: "Accesos directos:"

[Files]
; Copiar toda la carpeta publicada
Source: "publish\SILF\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Ícono de la app
Source: "SILF.App\Assets\Icons\icono.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icono.ico"; Comment: "{#MyAppDescription}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icono.ico"; Tasks: desktopicon; Comment: "{#MyAppDescription}"

[Run]
; Ejecutar la app al finalizar la instalación
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Limpiar archivos generados por la app (BD, logos, etc.)
Type: filesandordirs; Name: "{app}\Assets"
Type: files; Name: "{app}\silf.db"

[Code]
// Verificar si la app está corriendo antes de instalar/desinstalar
function IsAppRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec('tasklist', '/FI "IMAGENAME eq SILF.App.exe" /NH', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    // tasklist siempre retorna 0, verificamos por otro medio
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  // Mensaje de bienvenida personalizado
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Post-instalación: crear directorio para Assets si no existe
    ForceDirectories(ExpandConstant('{app}\Assets\Images'));
  end;
end;
