# ──────────────────────────────────────────────────────────────
#  build_installer.ps1  ·  picosoft / SILF
#  Publica la app (Release, autocontenida) y compila el instalador con Inno Setup.
#
#  Uso:
#     .\build_installer.ps1                 (publica + compila el instalador)
#     .\build_installer.ps1 -SkipPublish    (solo recompila el instalador, sin republicar)
#
#  Si PowerShell no deja ejecutar el script, corrélo así:
#     powershell -ExecutionPolicy Bypass -File .\build_installer.ps1
# ──────────────────────────────────────────────────────────────
param(
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
$sw = [System.Diagnostics.Stopwatch]::StartNew()

# Raíz del proyecto = carpeta donde está este script
$base = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
Set-Location $base

function Paso($m) { Write-Host "`n=== $m ===" -ForegroundColor Cyan }

try {
    # 1) Publicar la app
    if (-not $SkipPublish) {
        Paso "1/3  Publicando la app (Release, autocontenida)"
        if (Test-Path "$base\publish\SILF") { Remove-Item "$base\publish\SILF" -Recurse -Force }
        dotnet publish "SILF.App\SILF.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "publish\SILF"
        if ($LASTEXITCODE -ne 0) { throw "Falló 'dotnet publish'." }
    }
    else {
        Paso "1/3  Publicación omitida (-SkipPublish)"
    }

    # 2) Ubicar Inno Setup (ISCC.exe)
    Paso "2/3  Buscando Inno Setup"
    $candidatos = @(
        "C:\Program Files\Inno Setup 7\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 7\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    )
    $iscc = $candidatos | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc) { throw "No se encontró ISCC.exe. Instalá Inno Setup o ajustá la ruta en el script." }
    Write-Host "Usando: $iscc"

    # 3) Compilar el instalador
    Paso "3/3  Compilando el instalador"
    & $iscc "installer.iss"
    if ($LASTEXITCODE -ne 0) { throw "Falló la compilación del instalador (ISCC)." }

    # Resultado
    $sw.Stop()
    $exe = Join-Path $base "installer_output\SILF_Setup_1.0.0.exe"
    Write-Host "`n  LISTO en $([math]::Round($sw.Elapsed.TotalSeconds,1)) s" -ForegroundColor Green
    if (Test-Path $exe) {
        $mb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
        Write-Host "  Instalador: $exe  ($mb MB)" -ForegroundColor Green
        explorer.exe "/select,`"$exe`""   # abre la carpeta y resalta el instalador
    }
    else {
        Write-Host "  (No encontré el .exe esperado; revisá la salida de ISCC de arriba.)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "`n  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
