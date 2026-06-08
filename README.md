# SILF — Sistema Integral de Liquidación y Flotación

Sistema de escritorio Windows para la gestión de **liquidaciones de minerales** (Zinc, Plata, Plomo), **liquidación de concentrados**, **inversión-flotación** y **caja chica** de una empresa minera en Potosí, Bolivia.

Desarrollado en **C# / WPF** con Material Design por **picosoft**.

---

## 1. Stack Tecnológico

| Componente | Tecnología | Versión |
|---|---|---|
| Lenguaje | C# | .NET 10 |
| UI Framework | WPF + MaterialDesignThemes | Material Design 3 |
| Base de datos | SQLite | vía EF Core |
| ORM | Entity Framework Core | 9.x |
| MVVM | CommunityToolkit.Mvvm | 8.x |
| PDF | QuestPDF | Community |
| Excel | ClosedXML | 0.104+ |
| QR | QRCoder (PngByteQRCode) | 1.6+ |
| Instalador | Inno Setup | 7.x |
| Fuentes | Montserrat (UI), Segoe UI (datos) | — |

---

## 2. Estructura del Proyecto

```
D:\ARCHIVOS\POTOSI\SILF\
├── SILF.sln                    # Solución principal
├── installer.iss               # Script Inno Setup (instalador con marca picosoft)
├── build_installer.ps1         # Publica la app y compila el instalador en un paso
├── .gitignore
│
├── setup/                      # Imágenes del asistente del instalador (BMP)
│   ├── wizard-large.bmp        # Panel de bienvenida (marca picosoft)
│   └── wizard-small.bmp        # Ícono de páginas internas
├── brand/                      # Marca picosoft (uso general)
│   ├── picosoft_logo.png       # Logotipo horizontal (fondo transparente)
│   ├── picosoft_mark.png       # Símbolo (hexágono "p")
│   └── picosoft_icon.ico       # Ícono multitamaño
│
├── SILF.Core/                  # Modelos, Enums, Helpers
│   ├── Models/
│   │   ├── Empresa.cs          # Razón social, NIT, logo, T/C, NombreLiquidador
│   │   ├── Usuario.cs          # Nombre, usuario, passwordHash, rol, activo
│   │   ├── Cooperativa.cs / Mina.cs / Proveedor.cs
│   │   ├── Lote.cs             # Pesos, leyes, chofer, placa, estado, tipo mineral
│   │   ├── Liquidacion.cs      # Cálculos completos, deducciones, líquido pagable
│   │   ├── Concentrado.cs      # Liquidación de concentrados (módulo independiente)
│   │   ├── Flotacion.cs / ProcesoFlotacion.cs
│   │   ├── Pago.cs / BonoTransporte.cs
│   │   └── ReciboCaja.cs / MovimientoCaja.cs / ArqueoCaja.cs
│   ├── Enums/
│   │   ├── EstadoLote.cs / EstadoProcesoFlotacion.cs
│   │   ├── TipoMineral.cs      # Complejo, Brosa
│   │   ├── TipoConcentrado.cs  # ZnAg, Ag  (etiquetas ZN-AG / AG-PB)
│   │   └── RolUsuario.cs       # Administrador, Contador
│   └── Helpers/
│       ├── NumeroALetras.cs    # 1500.50 → "MIL QUINIENTOS 50/100 BOLIVIANOS"
│       ├── ConcentradoCalculator.cs  # Motor de cálculo de concentrados (validado vs Excel)
│       └── SilfCrypto.cs       # AES-256-CBC + HMAC-SHA256 (export/import de arqueos)
│
├── SILF.Data/
│   └── SilfDbContext.cs        # Tablas, relaciones, índices, datos semilla
│
├── SILF.App/                   # Aplicación WPF
│   ├── App.xaml(.cs)           # DI, MaterialDesign, detección primera vez
│   ├── Themes/                 # Sistema de tokens para modo claro/oscuro
│   │   ├── SilfTokens.xaml      # Tokens semánticos (defaults claros)
│   │   └── SilfPalette.Dark.xaml # Paleta oscura (gris neutro, WCAG AA)
│   ├── Views/                  # Vistas XAML
│   │   ├── LoginView / SetupWizardView / MainWindow / InicioView
│   │   ├── LotesView / LoteFormView
│   │   ├── LiquidacionListView / LiquidacionView
│   │   ├── FlotacionView
│   │   ├── ConcentradosListView          # Lista de concentrados
│   │   ├── TipoConcentradoDialog          # Elegir tipo (ZN-AG / AG-PB)
│   │   ├── ConcentradoFormView            # Ventana única de cálculo (vigente)
│   │   ├── CajaChicaView / ReciboPreviewView / ReportesView
│   │   ├── EmpresaView / UsuariosView / CatalogosView / AgregarItemDialog
│   │   └── (code-behind .xaml.cs correspondientes)
│   ├── ViewModels/             # ViewModels (MVVM)
│   │   ├── ...                  # uno por vista
│   │   ├── ConcentradoFormViewModel.cs    # ~70 propiedades + motor de cálculo
│   │   ├── ConcentradosListViewModel.cs
│   │   └── SesionService.cs / BaseViewModel.cs
│   ├── Converters/
│   │   ├── StringMatchConverter.cs        # Resaltar sidebar activo
│   │   ├── PercentInputConverter.cs       # Entrada en % (53) ↔ fracción (0.53)
│   │   ├── RequiredFieldConverter.cs      # Resaltar campos obligatorios
│   │   └── WizardConverters.cs
│   ├── Behaviors/
│   │   └── NumericInput.cs                # Select-on-focus, 4 decimales
│   └── Assets/
│       ├── Images/             # logo.png, back-image.jpg, user/key icons
│       └── Icons/              # icono.ico (app), anonimous.ico (recovery)
│
├── SILF.Reports/               # Generadores PDF y Excel
│   ├── LiquidacionPdfReport.cs / LiquidacionConsolidadaExcel.cs
│   ├── FlotacionConsolidadaExcel.cs / FlotacionExcelReport.cs
│   ├── ConcentradoReciboPdfGenerator.cs   # Recibo de concentrado
│   ├── ReciboPdfGenerator.cs / ReciboAnticipoPdfGenerator.cs
│   ├── LibroDiarioPdfReport.cs / LibroDiarioExcelReport.cs
│   ├── HistorialProveedorPdfReport.cs
│   ├── ArqueoExportService.cs / ArqueoImportService.cs
│   └── QrHelper.cs
│
└── SILF.Recovery/              # Herramienta de emergencia (consola)
    ├── Program.cs              # Reset de contraseñas con clave maestra
    └── SILF.Recovery.csproj    # Proyecto independiente, PublishTrimmed
```

> **Nota sobre carpetas no versionadas** (`.gitignore`): `bin/`, `obj/`, `publish/`, `installer_output/`, `*.db` y reportes generados (`*.pdf`, `*.xlsx`) **no** se suben al repo. El instalador se distribuye por **GitHub Releases**.

---

## 3. Requisitos para Desarrollo

```powershell
# .NET 10 SDK
winget install Microsoft.DotNet.SDK.10
# Git
winget install Git.Git
# Inno Setup 7 (para generar el instalador)
winget install JRSoftware.InnoSetup
```

Editor recomendado: **VS Code** con `ms-dotnettools.csdevkit`.

> **Codificación de archivos:** todos los `.cs` y `.xaml` se guardan en **UTF-8 con BOM**. Es obligatorio para que WPF lea correctamente los acentos (ñ, í, á…). Sin BOM, WPF interpreta el archivo como ANSI y los acentos se corrompen (ej.: "Elegí" → "ElegÃ").

---

## 4. Clonar, Compilar y Ejecutar

```powershell
git clone https://github.com/claudecorreo2077-commits/SILF0
cd SILF0
dotnet restore
dotnet build SILF.sln
dotnet run --project SILF.App
```

Si hay errores de caché WPF (`.baml` no encontrado):

```powershell
dotnet clean SILF.sln
Remove-Item "SILF.App\obj" -Recurse -Force
Remove-Item "SILF.App\bin" -Recurse -Force
dotnet build SILF.sln --no-incremental
```

---

## 5. Primera Ejecución — Wizard de Configuración Inicial

Al ejecutar SILF por primera vez (BD recién creada) aparece un wizard de 3 pasos antes del login:

1. **Datos de la Empresa** — Razón Social, Municipio, Ingenio, Tipo de Cambio, logo con preview.
2. **Cuenta de Administrador** — Nombre, usuario, contraseña (toggle ver/ocultar, indicador de coincidencia).
3. **Resumen** — verificación y FINALIZAR.

Detección automática: el wizard aparece si el admin (Id=1) tiene el hash de la contraseña semilla `admin123`. Una vez completado, no vuelve a aparecer.

---

## 6. Módulos del Sistema — Estado Actual

| Módulo | Estado | Descripción |
|---|---|---|
| Login | ✅ Completo | SHA256, tema oscuro gradiente, Enter = Ingresar |
| Wizard Primera Vez | ✅ Completo | 3 pasos, preview logo, indicador de match |
| Dashboard | ✅ Completo | Tarjetas resumen, accesos rápidos, últimos lotes |
| Lotes | ✅ Completo | CRUD, formulario 3 columnas, autocompletar CI/NIT, estados |
| Liquidación | ✅ Completo | Motor de cálculos verificado vs Excel, PDF + Excel |
| Flotación | ✅ Completo | Vista consolidada con deducciones, filtros por mina, Excel |
| **Concentrados** | ✅ Completo | **Cálculo de concentrados ZN-AG y AG-PB, ventana única, recibo PDF** |
| Caja Chica | ✅ Completo | Recibos, libro diario, arqueo, PDF con QR, export/import cifrado |
| Reportes | ✅ Completo | Reportes consolidados con filtros de fecha |
| Configuración | ✅ Completo | Empresa, logo, liquidador, T/C, exportar/importar BD |
| Usuarios | ✅ Completo | CRUD + roles Admin/Contador, cambio de contraseña |
| Catálogos | ✅ Completo | Proveedores, Cooperativas, Minas |

---

## 7. Módulo de Concentrados

Cálculo independiente de la liquidación de minerales en bruto. Maneja **dos tipos**:

- **ZN-AG** — concentrado de zinc que paga zinc y plata.
- **AG-PB** — concentrado de plata que paga plata y plomo.

### Interfaz (ventana única)

Diseño de una sola pantalla con feedback inmediato del resultado:

- **Izquierda (entradas):** tarjetas por bloque → *Cliente y lote*, *Análisis del lote* (TMH, humedad, merma, leyes e impurezas), *Cotizaciones, T/C y alícuotas*, *Deducciones que se aplican*. Un panel plegable **"Parámetros avanzados"** agrupa maquila, factores, libres, refinación y penalidades.
- **Derecha (resultado fijo):** tarjeta que no se mueve al hacer scroll, con el **Líquido Pagable** (Bs y $us) destacado, el desglose completo y los botones **CALCULAR · Guardar · Recibo**, más "Cargar datos de muestra" para comparar con el Excel del cliente.

### Convenciones de entrada

- Los porcentajes se **escriben como porcentaje** (53, 8, 30) aunque internamente se guarden como fracción (0.53, 0.08, 0.30) — vía `PercentInputConverter`.
- Las leyes de **plata (AG)** y el "AG libre (oz)" **no** son porcentaje: se ingresan tal cual.
- Las antiguas marcas "X" del Excel (qué se aplica) ahora son **casillas (checkboxes)**: regalías, COMIBOL, CNS, FEDECOMIN, FENCOMIN, Wilstermann, Aporte Cooperativa, M-02, comisión bancaria y los fletes (rollback, transporte, AHK, molienda).
- Las tasas de las deducciones son **editables en %**.
- El **N° de liquidación** es automático y correlativo por tipo: `CZN-TMT-001`, `CPB-TMT-001`, …

### Motor de cálculo

`ConcentradoCalculator` (en `SILF.Core`) cubre: peso neto seco, valor pagable por mineral (con límite libre y factor), gastos de tratamiento (maquila + escalador) y refinación, penalidades por impureza, valor FOB ($us y Bs), regalías por alícuota y el resto de retenciones, hasta el líquido pagable y el saldo.

**Validado al centavo contra el Excel del cliente:**

| Tipo | Resultado | Bs | $us |
|---|---|---|---|
| ZN-AG | Líquido Pagable | 2 792,32 | 320,47 |
| AG-PB | Saldo a Pagar | 31 893,29 | 4 582,37 |

---

## 8. Roles y Permisos

| Sidebar | Administrador | Contador |
|---|---|---|
| Inicio | ✅ | ✅ |
| Lotes / Liquidación / Inv. Flotación / Concentrados | ✅ | ❌ |
| Caja Chica | ✅ CRUD | ✅ Solo crear y consultar |
| Reportes | ✅ Todo | ✅ Solo Libro Diario |
| Configuración / Usuarios / Catálogos | ✅ | ❌ |

El Contador puede exportar arqueos como archivos `.silf-arqueo` cifrados (AES-256-CBC + HMAC-SHA256) y el Admin los importa en otra PC para consolidar.

---

## 9. Flujo del Negocio (mineral en bruto)

```
Proveedor llega con mineral
 → Registro de lote → Anticipo obligatorio → Laboratorio → Registro de leyes
   → Liquidación + Flotación → Deducciones → Pago del saldo → Lote completado
```

Estados del lote: Registrado → AnticipoPagado → EnLaboratorio → LeyesRegistradas → Liquidado → Completado.

---

## 10. Fórmulas de Liquidación (mineral en bruto)

```
PesoNetoSeco      = PesoNeto − (PesoNeto × %Humedad/100)
Valor_$US         = PesoNetoSeco × Σ(Ley × Cotización)   (ZN, AG, PB)
Valor_Bs          = Valor_$US × Tipo de Cambio
Deducciones       = Regalías 6% (T/C Regalías) + CNS 1.8% + COMIBOL 1%
                    + FENCOMIN 0.4% + FEDECOMIN 1% + Cooperativa (variable)
                    + IUE 5% (condicional)   (resto con T/C General)
LíquidoPagable_Bs = Valor_Bs − Deducciones
SaldoPagar        = LíquidoPagable − Anticipo
```

Dos tipos de cambio configurables: `TC_Regalias = 6.96` (solo Regalías) y `TC_General = 6.90` (resto).

---

## 11. Reglas de Negocio

- Tipos de mineral: **COMPLEJO** (2-3 minerales) y **BROSA** (1 mineral); se registran manualmente.
- Anticipo obligatorio por lote, no se arrastra.
- Bono de transporte obligatorio y variable (recibo recortable).
- El T/C se guarda como *snapshot* por liquidación (cambios futuros no afectan registros previos).
- Minas: CERRO, PORCO R.L., HUAYNA PORCO. Ingenio: Villa Imperial · Municipio: Porco.

---

## 12. UI / Diseño

- **Login / Wizard:** estilo oscuro con gradiente y borde neón; ventana sin bordes; Enter dispara el login.
- **Sidebar:** colapsable con animación (65px ↔ 220px), logo como toggle, RadioButtons con borde lateral al seleccionar.
- **Tema claro/oscuro:** sistema de **tokens semánticos** (`SilfTokens.xaml`) con paleta oscura (`SilfPalette.Dark.xaml`) de gris neutro (estética tipo VS Code Dark+ / GitHub Dark), conforme a WCAG AA.
- **Formularios:** Material Design Outlined; entradas numéricas con select-on-focus y 4 decimales; campos obligatorios resaltados.
- **Fuentes:** Montserrat (encabezados/sidebar), Segoe UI (datos/tablas).

---

## 13. Distribución

### 13.1 Generar el instalador (rápido)

```powershell
cd D:\ARCHIVOS\POTOSI\SILF
.\build_installer.ps1
```

El script `build_installer.ps1` publica la app (Release, autocontenida) y compila el instalador con Inno Setup en un solo paso, detectando dónde está instalado ISCC. Opción `-SkipPublish` para recompilar solo el instalador.

Equivalente manual:

```powershell
dotnet publish SILF.App\SILF.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\SILF
& "C:\Program Files\Inno Setup 7\ISCC.exe" installer.iss
# Resultado: installer_output\SILF_Setup_1.0.0.exe
```

El instalador (marca **picosoft**) incluye: panel de bienvenida e ícono de la marca, acceso directo en Escritorio y Menú Inicio, desinstalador, idioma español y ejecución automática al finalizar. El cliente **no** necesita instalar .NET (es autocontenido).

### 13.2 Marca picosoft

La identidad de la desarrolladora (hexágono "p", verde) está en `brand/` (logo, símbolo, ícono) y alimenta las imágenes del asistente del instalador en `setup/`.

### 13.3 Herramienta de Recovery (SilfMaintenance)

```powershell
dotnet publish SILF.Recovery -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish\Recovery
# Resultado: publish\Recovery\SilfMaintenance.exe
```

Consola de emergencia para resetear contraseñas: se copia junto a `silf.db`, se ejecuta, se ingresa la clave maestra y permite listar/resetear/activar usuarios.

---

## 14. Base de Datos (SQLite)

**Tablas:** Empresas, Usuarios, Cooperativas, Minas, Proveedores, Lotes, Liquidaciones, Concentrados, Flotaciones, ProcesosFlotacion, Pagos, BonosTransporte, RecibosCaja, MovimientosCaja, ArqueosCaja.

**Datos semilla:** Admin `admin` / `admin123` (SHA256); Minas CERRO, PORCO R.L., HUAYNA PORCO; Empresa con Municipio "Porco", Ingenio "Villa Imperial", T/C 6.96.

**Exportar / Importar** (Configuración, solo Admin): respaldo `silf_backup_YYYYMMDD_HHMM.db`; la importación crea respaldo automático antes de reemplazar y reinicia la app.

---

## 15. Repositorio

- **GitHub:** https://github.com/claudecorreo2077-commits/SILF0
- **Branch:** `main`
- El instalador (`.exe`) se publica en **Releases**, no dentro del repo.

---

## 16. Pendientes

- Testing con **datos reales** del cliente (todos los módulos).
- Recibo de Anticipo PDF: la referencia "Proceso N°" muestra 0 hasta que el lote se flota (revisión pendiente).
- Warning `SYSLIB0060` en `SilfCrypto.cs` (migrar al método estático `Rfc2898DeriveBytes.Pbkdf2`).
- Ajustes post-testing.

---

## Licencia

Proyecto privado — uso interno. Desarrollado por **picosoft**.
