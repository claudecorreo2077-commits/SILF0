# SILF — Sistema Integral de Liquidación y Flotación

Sistema de escritorio para la gestión de liquidaciones de minerales (Zinc, Plata, Plomo) desarrollado en **C# / WPF** con **Material Design**.

## Tecnologías

- **Framework:** .NET 10 (WPF - Windows)
- **UI:** Material Design In XAML Toolkit
- **ORM:** Entity Framework Core + SQLite
- **PDF:** QuestPDF + QRCoder
- **Excel:** ClosedXML
- **MVVM:** CommunityToolkit.Mvvm
- **Fuentes:** Montserrat (UI), Segoe UI (datos)
- **Instalador:** Inno Setup 7

## Estructura del Proyecto

```
SILF.sln
├── SILF.Core/            # Modelos, Enums, Helpers
│   ├── Models/           # Empresa, Usuario, Lote, Liquidacion, Flotacion,
│   │                     # Pago, BonoTransporte, ReciboCaja, MovimientoCaja, ArqueoCaja
│   ├── Enums/            # EstadoLote, TipoMineral, RolUsuario
│   └── Helpers/          # NumeroALetras
├── SILF.Data/            # DbContext
│   └── SilfDbContext.cs  # 12 tablas, relaciones, índices, datos semilla
├── SILF.App/             # Aplicación WPF principal
│   ├── Views/            # 16 vistas XAML + code-behind
│   ├── ViewModels/       # 14 ViewModels MVVM
│   ├── Services/         # SesionService
│   ├── Converters/       # StringMatchConverter
│   └── Assets/           # Images/ (logo, back-image) + Icons/ (icono.ico)
├── SILF.Reports/         # Generación de PDFs y Excel
│   ├── LiquidacionPdfReport.cs        # PDF liquidación (2 copias + bono)
│   ├── LiquidacionConsolidadaExcel.cs # Excel resumen + pestañas por lote
│   ├── FlotacionConsolidadaExcel.cs   # Excel consolidado con deducciones
│   ├── ReciboPdfGenerator.cs          # PDF recibo caja chica (2 copias + QR)
│   ├── LibroDiarioPdfReport.cs        # PDF libro diario de caja
│   ├── HistorialProveedorPdfReport.cs # PDF historial por proveedor
│   └── QrHelper.cs                    # Generador de QR
├── SILF.Recovery/        # Herramienta de recuperación de contraseñas
│   └── Program.cs        # Consola independiente con clave maestra
└── installer.iss         # Script Inno Setup para generar instalador
```

## Requisitos Previos (solo para desarrollo)

1. **.NET 10 SDK** — [Descargar](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **VS Code** con extensión C# Dev Kit
3. **Windows 10/11** (WPF es solo Windows)
4. **Inno Setup 7** — para generar instalador (`winget install JRSoftware.InnoSetup`)

> **Nota:** El instalador generado es autocontenido — el usuario final NO necesita instalar .NET.

## Instalación desde Cero (desarrollo)

```powershell
git clone https://github.com/claudecorreo2077-commits/SILF0
cd SILF
dotnet restore
dotnet build SILF.sln
dotnet run --project SILF.App
```

## Primera Ejecución

Al ejecutar por primera vez, aparece un **wizard de configuración inicial** de 3 pasos:

1. **Empresa:** Razón social, municipio (opcional), ingenio (opcional), tipo de cambio, logo
2. **Administrador:** Nombre completo, usuario, contraseña
3. **Resumen:** Verificación de datos antes de guardar

El wizard se detecta automáticamente al verificar que el admin tiene la contraseña semilla. Una vez completado, no vuelve a aparecer.

**Datos semilla** (antes del wizard):
- Usuario: `admin` / Contraseña: `admin123`
- Minas: CERRO, PORCO R.L., HUAYNA PORCO
- Tipo de cambio: 6.96 Bs/USD

## Módulos del Sistema

| Módulo | Estado | Descripción |
|--------|--------|-------------|
| **Login** | ✅ | SHA256, tema oscuro gradiente, Enter = Ingresar |
| **Wizard Primera Vez** | ✅ | 3 pasos: Empresa → Admin → Resumen, preview logo, toggle contraseña |
| **Dashboard** | ✅ | Tarjetas resumen, accesos rápidos, últimos lotes |
| **Lotes** | ✅ | CRUD completo, formulario 3 columnas, autocompletar CI/NIT |
| **Liquidación** | ✅ | Lista + detalle + cálculos verificados vs Excel, PDF + Excel |
| **Flotación** | ✅ | Vista consolidada con deducciones desglosadas, Excel |
| **Caja Chica** | ✅ | Recibos CRUD, libro diario, arqueo, PDF con QR, impresión carta, firmas dinámicas |
| **Reportes** | ✅ | 5 reportes consolidados (ver sección Reportes) |
| **Configuración** | ✅ | Empresa, logo, liquidador, T/C, exportar/importar BD |
| **Usuarios** | ✅ | CRUD + roles Admin/Contador, cambio de contraseña |
| **Catálogos** | ✅ | Proveedores, Cooperativas, Minas con protección de dependencias |

## Reportes

| Reporte | Formato | Acceso |
|---------|---------|--------|
| Liquidación individual | PDF (2 copias + bono recortable + QR) | Admin |
| Liquidaciones consolidadas | Excel (resumen + pestaña por lote) | Admin |
| Flotación consolidada | Excel (deducciones desglosadas) | Admin |
| Libro diario de caja | PDF (saldo anterior, movimientos, totales) | Admin + Contador |
| Historial por proveedor | PDF (todos los lotes con leyes y montos) | Admin |
| Recibo de caja chica | PDF (2 copias recortables + QR) | Admin + Contador |

## Roles y Permisos

| Elemento | Administrador | Contador |
|----------|---------------|----------|
| Dashboard | ✅ | ✅ |
| Lotes | ✅ CRUD | ❌ Oculto |
| Liquidación | ✅ CRUD | ❌ Oculto |
| Flotación | ✅ Vista | ❌ Oculto |
| Caja Chica | ✅ CRUD completo | ✅ Solo crear y consultar |
| Reportes | ✅ Todos | ✅ Solo Caja Chica |
| Configuración | ✅ | ❌ Oculto |
| Usuarios | ✅ | ❌ Oculto |
| Catálogos | ✅ | ❌ Oculto |

## Flujo del Negocio

```
Proveedor llega con mineral
    → Registro de lote (peso bruto, tara, peso neto)
        → Pago de anticipo obligatorio
            → Laboratorio analiza (días)
                → Registro de leyes (ZN%, AG oz/t, PB%)
                    → Liquidación + Flotación (simultáneas)
                        → Cálculo de deducciones
                            → Pago del saldo (líquido pagable - anticipo)
                                → Lote completado
```

**Estados del lote:** Registrado → AnticipoPagado → EnLaboratorio → LeyesRegistradas → Liquidado → Completado

## Fórmulas de Liquidación

```
1. PesoHumedad      = ROUND(PesoNeto × %Humedad / 100, 2)
2. PesoNetoSeco     = PesoNeto - PesoHumedad
3. ValorZn_$US      = PesoNetoSeco × LeyZN × CotizaciónZN
4. ValorAg_$US      = PesoNetoSeco × LeyAG × CotizaciónAG
5. ValorPb_$US      = PesoNetoSeco × LeyPB × CotizaciónPB
6. ValorComercial_$US = ValorZn + ValorAg + ValorPb
7. ValorComercial_Bs  = ValorComercial_$US × TipoCambio

Deducciones legales (sobre ValorComercial_Bs):
8.  Regalías    = ValorComercial_Bs × 6%
9.  CNS         = ValorComercial_Bs × 1.8%
10. COMIBOL     = ValorComercial_Bs × 1%

Otras deducciones:
11. FENCOMIN     = ValorComercial_Bs × 0.4%
12. FEDECOMIN    = ValorComercial_Bs × 1%
13. Cooperativa  = ValorComercial_Bs × %variable (editable)
14. IUE          = ValorComercial_Bs × 5% (toggle on/off)

Resultado:
15. TotalDeducciones    = Legales + Otras
16. LiquidoPagable_Bs   = ValorComercial_Bs - TotalDeducciones
17. LiquidoPagable_$US  = LiquidoPagable_Bs / TipoCambio
18. SaldoPagar          = LiquidoPagable - Anticipo
```

## Reglas de Negocio

- **Tipos de mineral:** COMPLEJO (2-3 minerales: ZN+AG o ZN+AG+PB) y BROSA (1 mineral)
- **Correlativo de lotes:** se resetea cada 70 toneladas
- **Anticipo:** obligatorio por lote, no se arrastra entre lotes
- **Bono transporte:** obligatorio, monto variable
- **Registros visibles/ocultos:** flag para filtrar en reportes
- **Proveedor:** tiene N lotes, cada lote con su chofer/vehículo
- **Autocompletar:** CI/NIT busca proveedor existente
- **Una sola empresa** con logo configurable
- **Liquidación y Flotación** son simultáneas por lote
- **Minas:** CERRO, PORCO R.L., HUAYNA PORCO
- **Ingenio:** Villa Imperial | **Municipio:** Porco

## Base de Datos (SQLite)

### Exportar / Importar

Desde **Configuración** (solo Admin):
- **Exportar Respaldo:** copia `silf.db` con nombre `silf_backup_YYYYMMDD_HHMM.db`
- **Importar BD:** reemplaza la BD actual (crea respaldo automático antes), reinicia la app

### Tablas

```
Empresas, Usuarios, Cooperativas, Minas, Proveedores,
Lotes, Liquidaciones, Flotaciones, Pagos, BonosTransporte,
RecibosCaja, MovimientosCaja, ArqueosCaja
```

## UI / Diseño

- **Login:** Estilo oscuro con gradiente (ref: RJCodeAdvance/ModernLoginUI-WPF). Enter = Ingresar
- **Sidebar:** Extensión de menú al colapsar. Logo como toggle del sidebar
- **Tema:** Claro por defecto, toggle oscuro/claro
- **Formularios:** Material Design Outlined, 3 columnas con grid
- **Diálogos:** Overlay oscuro + border con sombra y corner radius 16

## Distribución

### Generar instalador

```powershell
# 1. Publicar (autocontenido, no requiere .NET en el cliente)
dotnet publish SILF.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=false -o .\publish\SILF

# 2. Compilar instalador
& "C:\Program Files\Inno Setup 7\ISCC.exe" installer.iss

# Resultado: installer_output\SILF_Setup_1.0.0.exe
```

### Herramienta de Recovery

Consola independiente para resetear contraseñas de usuarios en caso de emergencia.

```powershell
# Compilar
dotnet publish SILF.Recovery -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o .\publish\Recovery

# Resultado: publish\Recovery\SilfMaintenance.exe
# Colocar junto a silf.db y ejecutar
# Clave de acceso: [protegida — ver documentación interna]
```

## Paquetes NuGet

```xml
<!-- SILF.Core -->
<PackageReference Include="System.ComponentModel.Annotations" />

<!-- SILF.Data -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" />

<!-- SILF.App -->
<PackageReference Include="MaterialDesignThemes" />
<PackageReference Include="MaterialDesignColors" />
<PackageReference Include="CommunityToolkit.Mvvm" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />

<!-- SILF.Reports -->
<PackageReference Include="QuestPDF" />
<PackageReference Include="ClosedXML" />
<PackageReference Include="QRCoder" />

<!-- SILF.Recovery -->
<PackageReference Include="Microsoft.Data.Sqlite" />
```

## Repositorio

- **GitHub:** https://github.com/claudecorreo2077-commits/SILF0
- **Branch:** main

## Licencia

Proyecto privado — uso interno.
