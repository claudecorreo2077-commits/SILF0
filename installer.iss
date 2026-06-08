; Ruta: D:\ARCHIVOS\POTOSI\SILF\installer.iss
; Script de Inno Setup para SILF
; Compilar con: Inno Setup Compiler (Ctrl+F9)  o  ISCC.exe installer.iss
;
; PASO PREVIO OBLIGATORIO (genera la carpeta publish\SILF que este script empaqueta):
;   dotnet publish SILF.App\SILF.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\SILF

#define MyAppName "SILF"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "picosoft"
#define MyAppEmail "krlosdelfin@gmail.com"
#define MyAppExeName "SILF.App.exe"
#define MyAppDescription "Sistema Integral de Liquidación y Flotación"

[Setup]
AppId={{B8F2A3D1-7E4C-4A9B-8D5F-1C2E3F4A5B6C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}

; ── Datos del desarrollador (visibles en "Programas y características" y soporte) ──
AppPublisher={#MyAppPublisher}
AppPublisherURL=mailto:{#MyAppEmail}
AppSupportURL=mailto:{#MyAppEmail}
AppUpdatesURL=mailto:{#MyAppEmail}
AppContact={#MyAppEmail}

; ── Metadatos incrustados en el propio Setup.exe (clic derecho > Propiedades > Detalles) ──
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} - Instalador
VersionInfoCopyright=© 2026 {#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}.0

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.\installer_output
OutputBaseFilename=SILF_Setup_{#MyAppVersion}
SetupIconFile=SILF.App\Assets\Icons\icono.ico
; ── Imágenes del asistente (marca picosoft) ──
WizardImageFile=setup\wizard-large.bmp
WizardSmallImageFile=setup\wizard-small.bmp
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
; Copiar toda la carpeta publicada (incluye el runtime .NET autocontenido)
Source: "publish\SILF\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Ícono de la app (para los accesos directos)
Source: "SILF.App\Assets\Icons\icono.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icono.ico"; Comment: "{#MyAppDescription}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icono.ico"; Tasks: desktopicon; Comment: "{#MyAppDescription}"

[Run]
; Ejecutar la app al finalizar la instalación
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Limpiar archivos generados por la app (BD, logos, etc.) al desinstalar.
; OJO: esto borra la base de datos y los logos. Para una reinstalación que conserve
; datos, comentá estas dos líneas.
Type: filesandordirs; Name: "{app}\Assets"
Type: files; Name: "{app}\silf.db"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Asegurar el directorio de imágenes/logos de la empresa
    ForceDirectories(ExpandConstant('{app}\Assets\Images'));
  end;
end;
