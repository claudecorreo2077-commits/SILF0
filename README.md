# SILF — Sistema Integral de Liquidación y Flotación

Sistema de escritorio para la gestión de liquidaciones de minerales (Zinc, Plata, Plomo) desarrollado en **C# / WPF** con **Material Design**.

## Tecnologías

- **Framework:** .NET 10 (WPF - Windows)
- **UI:** Material Design In XAML Toolkit
- **ORM:** Entity Framework Core + SQLite
- **PDF:** QuestPDF
- **MVVM:** CommunityToolkit.Mvvm
- **Fuentes:** Montserrat, Segoe UI

## Estructura del Proyecto

```
SILF.sln
├── SILF.Core/          # Modelos, Enums, Interfaces
│   ├── Models/         # Empresa, Usuario, Lote, Liquidacion, Flotacion, etc.
│   └── Enums/          # EstadoLote, TipoMineral, RolUsuario
├── SILF.Data/          # DbContext, Migraciones
│   └── SilfDbContext.cs
├── SILF.App/           # Aplicación WPF principal
│   ├── Views/          # XAML + code-behind
│   ├── ViewModels/     # MVVM ViewModels
│   ├── Services/       # SesionService, etc.
│   ├── Converters/     # StringMatchConverter, etc.
│   ├── Assets/Images/  # Logo, iconos
│   └── App.xaml(.cs)   # Entrada de la aplicación
└── SILF.Reports/       # Generación de PDFs con QuestPDF
```

## Requisitos Previos

1. **.NET 10 SDK** — [Descargar](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **Visual Studio 2022+** o **VS Code** con extensión C#
3. **Windows 10/11** (WPF es solo Windows)

## Instalación desde Cero

### 1. Clonar el repositorio

```powershell
git clone <URL_DEL_REPOSITORIO>
cd SILF
```

### 2. Restaurar paquetes y compilar

```powershell
dotnet restore
dotnet build SILF.sln
```

### 3. Ejecutar

```powershell
dotnet run --project SILF.App
```

La base de datos SQLite (`silf.db`) se crea automáticamente en el directorio de ejecución con datos semilla:

- **Usuario:** `admin` / **Contraseña:** `admin123`
- **Minas:** CERRO, PORCO R.L., HUAYNA PORCO
- **Empresa:** Empresa Minera (Porco, Villa Imperial)
- **Tipo de cambio:** 6.97 Bs/USD

## Módulos

### ✅ Implementados

| Módulo | Descripción |
|--------|-------------|
| **Login** | Autenticación SHA256, tema oscuro con gradiente |
| **Dashboard** | Tarjetas resumen, accesos rápidos, últimos lotes |
| **Lotes** | CRUD completo con formulario de 3 columnas. Campos: fecha, tipo mineral, proveedor (autocompletar CI/NIT), cooperativa, mina, pesos, chofer, placa, ticket, anticipo, bono transporte, observaciones |
| **Configuración** | Datos de empresa (razón social, NIT, dirección, teléfono, municipio, ingenio), logo editable, tipo de cambio USD→Bs configurable |
| **Usuarios** | CRUD de usuarios. Cambiar mi contraseña (todos). Crear/editar/eliminar usuarios (solo admin). Roles: Administrador y Contador |
| **Catálogos** | Vista con tabs: Proveedores (CI, nombre, cooperativa), Cooperativas, Minas/Parajes. CRUD con protección (no eliminar si tiene dependencias) |

### ⏳ Pendientes

| Módulo | Descripción |
|--------|-------------|
| **Liquidación** | Motor de cálculo: peso seco, valor comercial $US/Bs, deducciones legales (regalías 6%, CNS 1.8%, COMIBOL 1%), otras deducciones (FENCOMIN 0.4%, FEDECOMIN 1%, cooperativa, IUE 5%, anticipo), líquido pagable. Modelo `Liquidacion.cs` ya preparado con todos los campos |
| **Flotación** | Inversión-Flotación simultánea con Liquidación por lote |
| **Caja Chica** | Recibos, movimientos, arqueo de caja |
| **Reportes** | Exportación PDF/Excel con QuestPDF |
| **Bono Transporte** | Recibo recortable media carta |

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

## Fórmulas de Liquidación (extraídas del Excel)

```
1. PesoHumedad    = ROUND(PesoNeto × %Humedad / 100, 8)
2. PesoNetoSeco   = PesoNeto - PesoHumedad
3. ValorZn_$US    = PesoNetoSeco × LeyZN × PrecioZN
4. ValorAg_$US    = PesoNetoSeco × LeyAG × PrecioAG
5. ValorPb_$US    = PesoNetoSeco × LeyPB × PrecioPB
6. ValorComercial_$US = ValorZn + ValorAg + ValorPb
7. ValorComercial_Bs  = ValorComercial_$US × TipoCambio

Deducciones legales (sobre ValorComercial_Bs):
8.  Regalías  = ValorComercial_Bs × 6%
9.  CNS       = ValorComercial_Bs × 1.8%
10. COMIBOL   = ValorComercial_Bs × 1%

Otras deducciones:
11. FENCOMIN     = ValorComercial_Bs × 0.4%
12. FEDECOMIN    = ValorComercial_Bs × 1%
13. Cooperativa  = ValorComercial_Bs × %variable
14. Anticipo     = monto pagado previamente
15. IUE          = ValorComercial_Bs × 5% (si aplica)

Resultado:
16. TotalDeducciones = Legales + Otras
17. LiquidoPagable_Bs  = ValorComercial_Bs - TotalDeducciones
18. LiquidoPagable_$US = LiquidoPagable_Bs / TipoCambio
```

## Reglas de Negocio

- **Tipos de mineral:** COMPLEJO (2-3 minerales: ZN+AG o ZN+AG+PB) y BROSA (1 mineral)
- **Correlativo de lotes:** se resetea cada 70 toneladas
- **Anticipo:** obligatorio por lote, no se arrastra entre lotes
- **Bono transporte:** obligatorio, monto variable
- **Registros visibles/ocultos:** flag para filtrar en reportes
- **Proveedor:** tiene N lotes, cada lote con su chofer/vehículo
- **Autocompletar:** CI/NIT busca proveedor existente
- **Roles:** Admin (acceso total), Contador (solo caja chica, sin editar/eliminar)
- **Una sola empresa** con logo configurable
- **Liquidación y Flotación** son simultáneas por lote
- **Minas conocidas:** CERRO, PORCO R.L., HUAYNA PORCO
- **Ingenio:** Villa Imperial | **Municipio:** Porco

## UI / Diseño

- **Login:** Estilo oscuro con gradiente (referencia: RJCodeAdvance/ModernLoginUI-WPF)
- **Sidebar:** Extensión de menú al colapsar, no tooltip. Logo como toggle del sidebar
- **Tema:** Claro por defecto (`_isDarkMode = false`, `ApplyTheme()` en `Loaded`), toggle oscuro/claro
- **Formularios:** Material Design Outlined, 3 columnas con grid
- **Diálogos:** Overlay oscuro + border con sombra y corner radius 16

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
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" />

<!-- SILF.Reports -->
<PackageReference Include="QuestPDF" />
```

## Licencia

Proyecto privado — uso interno.
