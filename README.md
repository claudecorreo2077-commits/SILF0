# SILF — Sistema Integral de Liquidación y Flotación

Sistema de escritorio Windows para la gestión de liquidaciones de minerales (Zinc, Plata, Plomo), inversión-flotación y caja chica de una empresa minera en Potosí, Bolivia.

Desarrollado en **C# / WPF** con **Material Design**.

---

## 1. Stack Tecnológico

| Componente | Tecnología | Versión |
|------------|-----------|---------|
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
├── installer.iss               # Script Inno Setup para generar instalador
│
├── SILF.Core/                  # Modelos, Enums, Helpers
│   ├── Models/
│   │   ├── Empresa.cs          # Razón social, NIT, logo, T/C, NombreLiquidador
│   │   ├── Usuario.cs          # Nombre, usuario, passwordHash, rol, activo
│   │   ├── Cooperativa.cs      # Nombre
│   │   ├── Mina.cs             # Nombre (CERRO, PORCO R.L., HUAYNA PORCO)
│   │   ├── Proveedor.cs        # NombreCompleto, CiNit, CooperativaId
│   │   ├── Lote.cs             # Pesos, leyes, chofer, placa, estado, tipo mineral
│   │   ├── Liquidacion.cs      # Cálculos completos, deducciones, líquido pagable
│   │   ├── Flotacion.cs        # Referencia consolidada al lote
│   │   ├── Pago.cs             # Anticipo, saldo, completado
│   │   ├── BonoTransporte.cs   # Monto variable por lote
│   │   ├── ReciboCaja.cs       # Nº, fecha, beneficiario, monto, concepto, QR
│   │   ├── MovimientoCaja.cs   # Entrada/salida vinculada a recibo
│   │   └── ArqueoCaja.cs       # Saldo contable vs físico
│   ├── Enums/
│   │   ├── EstadoLote.cs       # Registrado→AnticipoPagado→EnLaboratorio→...→Completado
│   │   ├── TipoMineral.cs      # Complejo, Brosa
│   │   └── RolUsuario.cs       # Administrador, Contador
│   └── Helpers/
│       └── NumeroALetras.cs    # Convierte 1500.50 → "MIL QUINIENTOS 50/100 BOLIVIANOS"
│
├── SILF.Data/
│   └── SilfDbContext.cs        # 13 tablas, relaciones 1:1 y N:1, índices, datos semilla
│
├── SILF.App/                   # Aplicación WPF
│   ├── App.xaml(.cs)           # DI, MaterialDesign, detección primera vez
│   ├── Views/                  # 17 vistas XAML
│   │   ├── LoginView.xaml      # Login oscuro con gradiente, Enter=Ingresar
│   │   ├── SetupWizardView.xaml # Wizard 3 pasos (primera ejecución)
│   │   ├── MainWindow.xaml     # Sidebar colapsable + área de contenido
│   │   ├── InicioView.xaml     # Dashboard con tarjetas resumen
│   │   ├── LotesView.xaml      # Lista de lotes con filtros por estado
│   │   ├── LoteFormView.xaml   # Formulario de lote 3 columnas
│   │   ├── LiquidacionListView.xaml  # Lista de lotes para liquidar
│   │   ├── LiquidacionView.xaml      # Formulario de cálculo de liquidación
│   │   ├── FlotacionView.xaml        # Tabla consolidada con deducciones
│   │   ├── CajaChicaView.xaml        # 3 tabs: recibos, libro diario, arqueo
│   │   ├── ReciboPreviewView.xaml    # Vista previa recibo (2 copias)
│   │   ├── ReportesView.xaml         # Central de reportes con filtros
│   │   ├── EmpresaView.xaml          # Config empresa + exportar/importar BD
│   │   ├── UsuariosView.xaml         # CRUD usuarios + cambio contraseña
│   │   ├── CatalogosView.xaml        # Tabs: Proveedores, Cooperativas, Minas
│   │   ├── AgregarItemDialog.xaml    # Diálogo genérico para agregar items
│   │   └── (code-behind .xaml.cs correspondientes)
│   ├── ViewModels/             # 15 ViewModels
│   │   ├── LoginViewModel.cs
│   │   ├── SetupWizardViewModel.cs   # Wizard primera vez
│   │   ├── MainViewModel.cs         # Navegación + permisos sidebar
│   │   ├── InicioViewModel.cs       # Dashboard
│   │   ├── LotesViewModel.cs        # Lista de lotes
│   │   ├── LoteFormViewModel.cs     # Formulario de lote
│   │   ├── LiquidacionListViewModel.cs
│   │   ├── LiquidacionViewModel.cs  # Motor de cálculos
│   │   ├── FlotacionViewModel.cs    # Vista consolidada
│   │   ├── CajaChicaViewModel.cs    # Recibos + diario + arqueo
│   │   ├── ReciboPreviewViewModel.cs
│   │   ├── ReportesViewModel.cs     # 5 reportes consolidados
│   │   ├── EmpresaViewModel.cs      # Config + exportar/importar BD
│   │   ├── UsuariosViewModel.cs
│   │   ├── CatalogosViewModel.cs
│   │   ├── SesionService.cs         # Singleton: usuario logueado + permisos
│   │   └── BaseViewModel.cs         # Cargando, Titulo
│   ├── Converters/
│   │   └── StringMatchConverter.cs  # Para resaltar sidebar activo
│   └── Assets/
│       ├── Images/             # logo.png, back-image.jpg, user-icon.png, key-icon.png
│       └── Icons/              # icono.ico (app), anonimous.ico (recovery)
│
├── SILF.Reports/               # Generadores PDF y Excel
│   ├── LiquidacionPdfReport.cs        # PDF carta: 2 copias (proveedor + liquidador) + bono recortable + QR
│   ├── LiquidacionConsolidadaExcel.cs # Excel: hoja resumen + pestaña por lote
│   ├── FlotacionConsolidadaExcel.cs   # Excel: tabla con todas las deducciones
│   ├── ReciboPdfGenerator.cs          # PDF media carta ×2: recibo caja chica + QR
│   ├── LibroDiarioPdfReport.cs        # PDF: movimientos de caja con saldo anterior
│   ├── HistorialProveedorPdfReport.cs # PDF: todos los lotes de un proveedor
│   └── QrHelper.cs                    # Genera QR con datos del documento
│
└── SILF.Recovery/              # Herramienta de emergencia (consola)
    ├── Program.cs              # Reset de contraseñas con clave maestra
    └── SILF.Recovery.csproj    # Proyecto independiente, PublishTrimmed
```

---

## 3. Requisitos para Desarrollo

### 3.1 Software obligatorio

```powershell
# .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

# Git
winget install Git.Git

# Inno Setup 7 (para generar instalador)
winget install JRSoftware.InnoSetup
```

### 3.2 Editor recomendado

**VS Code** con extensiones:
```powershell
code --install-extension ms-dotnettools.csdevkit
```

### 3.3 Paquetes NuGet (se instalan con `dotnet restore`)

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

---

## 4. Clonar, Compilar y Ejecutar

```powershell
git clone https://github.com/claudecorreo2077-commits/SILF0
cd SILF0
dotnet restore
dotnet build SILF.sln
dotnet run --project SILF.App
```

> **Nota:** Si hay errores de cache WPF (`.baml` no encontrado):
> ```powershell
> dotnet clean SILF.sln
> Remove-Item "SILF.App\obj" -Recurse -Force
> Remove-Item "SILF.App\bin" -Recurse -Force
> dotnet build SILF.sln --no-incremental
> ```

---

## 5. Primera Ejecución — Wizard de Configuración Inicial

Al ejecutar SILF por primera vez (o con la BD recién creada), aparece automáticamente un **wizard de 3 pasos** antes del login:

### Paso 1: Datos de la Empresa
- Razón Social (obligatorio)
- Municipio (opcional)
- Ingenio (opcional)
- Tipo de Cambio Bs/$US (ej: 6.96)
- Logo de la empresa (preview visual al seleccionar)

### Paso 2: Cuenta de Administrador
- Nombre Completo (se usa también como Nombre del Liquidador)
- Nombre de Usuario
- Contraseña + Confirmar (con toggle 👁 para ver/ocultar)
- Indicador en tiempo real: ✓/✗ contraseñas coinciden

### Paso 3: Resumen
- Preview del logo
- Datos de empresa y administrador para verificar
- Botón FINALIZAR guarda todo y abre el Login

**Detección automática:** El wizard aparece si el admin (Id=1) tiene el hash de la contraseña semilla `admin123`. Una vez completado, no vuelve a aparecer.

---

## 6. Módulos del Sistema — Estado Actual

| Módulo | Estado | Descripción |
|--------|--------|-------------|
| **Login** | ✅ Completo | Autenticación SHA256, tema oscuro gradiente, Enter = Ingresar |
| **Wizard Primera Vez** | ✅ Completo | 3 pasos, preview logo, toggle contraseña, indicador match |
| **Dashboard** | ✅ Completo | Tarjetas resumen, accesos rápidos, últimos lotes |
| **Lotes** | ✅ Completo | CRUD, formulario 3 columnas, autocompletar CI/NIT, estados |
| **Liquidación** | ✅ Completo | Lista + detalle + motor de cálculos verificado vs Excel, PDF + Excel |
| **Flotación** | ✅ Completo | Vista consolidada con deducciones desglosadas, filtros por mina, Excel |
| **Caja Chica** | ✅ Completo | Recibos CRUD, libro diario, arqueo, PDF con QR, impresión carta, firmas dinámicas por tipo Entrada/Salida |
| **Reportes** | ✅ Completo | 5 reportes consolidados con filtros de fecha (ver sección 7) |
| **Configuración** | ✅ Completo | Empresa, logo, liquidador, T/C, exportar/importar BD |
| **Usuarios** | ✅ Completo | CRUD + roles Admin/Contador, cambio de contraseña |
| **Catálogos** | ✅ Completo | Proveedores, Cooperativas, Minas con protección de dependencias |

---

## 7. Reportes y Exportaciones

Accesibles desde el módulo **Reportes** en el sidebar, con filtro global de rango de fechas.

| Reporte | Formato | Librería | Acceso | Descripción |
|---------|---------|----------|--------|-------------|
| Liquidación individual | PDF Carta | QuestPDF + QRCoder | Admin | 2 copias (proveedor + liquidador) + bono transporte recortable + código QR |
| Liquidaciones consolidadas | Excel | ClosedXML | Admin | Hoja resumen + pestaña individual por lote |
| Flotación consolidada | Excel | ClosedXML | Admin | Tabla con todas las deducciones desglosadas por columna |
| Libro diario de caja | PDF Carta | QuestPDF | Admin + Contador | Saldo anterior, movimientos del rango, totales, paginado |
| Historial por proveedor | PDF Carta | QuestPDF | Admin | Todos los lotes del proveedor con leyes, montos y estados |
| Recibo de caja chica | PDF Media carta ×2 | QuestPDF + QRCoder | Admin + Contador | 2 copias recortables con QR, firmas dinámicas |

### Código QR en PDFs

Los PDFs de liquidación y recibos de caja incluyen un **código QR** generado con `QRCoder` que contiene:
- Tipo de documento (SILF|L para liquidación, SILF|R para recibo)
- Número del documento
- Fecha
- Monto
- Datos relevantes (proveedor, tipo movimiento)

---

## 8. Roles y Permisos

| Elemento del sidebar | Administrador | Contador |
|---------------------|---------------|----------|
| Inicio (Dashboard) | ✅ Visible | ✅ Visible |
| Lotes | ✅ Visible | ❌ Oculto |
| Liquidación | ✅ Visible | ❌ Oculto |
| Inv. Flotación | ✅ Visible | ❌ Oculto |
| Caja Chica | ✅ CRUD completo | ✅ Solo crear y consultar (no editar/eliminar) |
| Reportes | ✅ Todas las secciones | ✅ Solo Libro Diario (Caja Chica) |
| Configuración | ✅ Visible | ❌ Oculto |
| Usuarios | ✅ Visible | ❌ Oculto |
| Catálogos | ✅ Visible | ❌ Oculto |

**Implementación:** `MainViewModel` expone propiedades `Visibility` que ocultan/muestran los RadioButtons del sidebar. `CajaChicaViewModel` recibe `_esAdmin` y expone `PuedeEditarEliminar`. `ReportesViewModel` usa las mismas propiedades de visibilidad.

---

## 9. Flujo del Negocio

```
Proveedor llega con mineral
    → Registro de lote (peso bruto, tara, peso neto)
        → Pago de anticipo obligatorio
            → Laboratorio analiza (días)
                → Registro de leyes (ZN%, AG oz/t, PB%)
                    → Liquidación + Flotación (simultáneas)
                        → Cálculo de deducciones
                            → Pago del saldo (líquido pagable - anticipo)
                                → Lote completado (fecha/hora)
```

**Estados del lote:** `Registrado` → `AnticipoPagado` → `EnLaboratorio` → `LeyesRegistradas` → `Liquidado` → `Completado`

---

## 10. Fórmulas de Liquidación

```
1. PesoHumedad      = ROUND(PesoNeto × %Humedad / 100, 2)
2. PesoNetoSeco     = PesoNeto - PesoHumedad

3. ValorZn_$US      = PesoNetoSeco × LeyZN × CotizaciónZN
4. ValorAg_$US      = PesoNetoSeco × LeyAG × CotizaciónAG
5. ValorPb_$US      = PesoNetoSeco × LeyPB × CotizaciónPB
6. ValorComercial_$US = ValorZn + ValorAg + ValorPb
7. ValorComercial_Bs  = ValorComercial_$US × TipoCambio

Deducciones legales (sobre ValorComercial_Bs):
 8. Regalías    = ValorComercial_Bs × 6%
 9. CNS         = ValorComercial_Bs × 1.8%
10. COMIBOL     = ValorComercial_Bs × 1%

Otras deducciones:
11. FENCOMIN     = ValorComercial_Bs × 0.4%
12. FEDECOMIN    = ValorComercial_Bs × 1%
13. Cooperativa  = ValorComercial_Bs × %variable (editable, checkbox)
14. IUE          = ValorComercial_Bs × 5% (toggle on/off)

Resultado:
15. TotalDeducciones    = Legales + Otras
16. LiquidoPagable_Bs   = ValorComercial_Bs - TotalDeducciones
17. LiquidoPagable_$US  = LiquidoPagable_Bs / TipoCambio
18. SaldoPagar          = LiquidoPagable - Anticipo
```

---

## 11. Reglas de Negocio

- **Tipos de mineral:** COMPLEJO (2-3 minerales: ZN+AG o ZN+AG+PB) y BROSA (1 mineral)
- **Correlativo de lotes:** se resetea cada 70 toneladas
- **Anticipo:** obligatorio por lote, no se arrastra entre lotes
- **Bono transporte:** obligatorio, monto variable ingresado por el usuario
- **Registros visibles/ocultos:** flag `Visible` para filtrar en reportes
- **Proveedor:** tiene N lotes, cada lote con su chofer/vehículo
- **Autocompletar:** CI/NIT busca proveedor existente y rellena nombre/cooperativa
- **Una sola empresa** con logo configurable
- **Liquidación y Flotación** son simultáneas por lote
- **Minas conocidas:** CERRO, PORCO R.L., HUAYNA PORCO
- **Ingenio:** Villa Imperial | **Municipio:** Porco
- **Tipo de cambio por defecto:** 6.96 Bs/$US

---

## 12. Base de Datos (SQLite)

### 12.1 Tablas

```
Empresas, Usuarios, Cooperativas, Minas, Proveedores,
Lotes, Liquidaciones, Flotaciones, Pagos, BonosTransporte,
RecibosCaja, MovimientosCaja, ArqueosCaja
```

### 12.2 Datos semilla

- **Admin:** Id=1, usuario `admin`, contraseña `admin123` (SHA256)
- **Minas:** CERRO (Id=1), PORCO R.L. (Id=2), HUAYNA PORCO (Id=3)
- **Empresa:** Id=1, "Empresa Minera", Municipio "Porco", Ingenio "Villa Imperial", T/C 6.96

### 12.3 Exportar / Importar (desde Configuración, solo Admin)

- **Exportar Respaldo:** copia `silf.db` a ubicación elegida con nombre `silf_backup_YYYYMMDD_HHMM.db`
- **Importar BD:** muestra advertencia, crea respaldo automático (`silf_antes_importar_...db`), reemplaza la BD y reinicia la app

---

## 13. UI / Diseño

- **Login:** Estilo oscuro con gradiente (#060531 → #1B1448), borde neón púrpura→rosa, ventana sin bordes. Enter dispara login
- **Wizard:** Mismo estilo visual que login, 3 indicadores de paso (dots), botones fijos fuera del ScrollViewer
- **Sidebar:** Colapsable con animación (65px ↔ 220px), extensión de menú al colapsar, logo como toggle, RadioButtons con borde lateral al seleccionar
- **Tema:** Claro por defecto, toggle oscuro/claro con botón en sidebar
- **Formularios:** Material Design Outlined, 3 columnas con grid
- **Diálogos:** Overlay oscuro + border con sombra y corner radius 16
- **Colores de estado:** Registrado (gris), AnticipoPagado (azul), EnLaboratorio (ámbar), LeyesRegistradas (púrpura), Liquidado (teal), Completado (verde)
- **Referencias de diseño:**
  - Login: [RJCodeAdvance/ModernLoginUI-WPF](https://github.com/RJCodeAdvance/ModernLoginUI-WPF)
  - Sidebar: [CSharpDesignPro/Navigation-Drawer-Sidebar-Menu-in-WPF](https://github.com/CSharpDesignPro/Navigation-Drawer-Sidebar-Menu-in-WPF)

---

## 14. Distribución

### 14.1 Generar instalador

```powershell
cd D:\ARCHIVOS\POTOSI\SILF

# Publicar (autocontenido — el cliente NO necesita instalar .NET)
dotnet publish SILF.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=false -o .\publish\SILF

# Generar instalador con Inno Setup
& "C:\Program Files\Inno Setup 7\ISCC.exe" installer.iss

# Resultado: installer_output\SILF_Setup_1.0.0.exe
```

El instalador incluye:
- Acceso directo en Escritorio (con ícono personalizado)
- Acceso en Menú Inicio
- Desinstalador
- Idioma español
- Ejecución automática al finalizar instalación

### 14.2 Herramienta de Recovery (SilfMaintenance)

Consola independiente para resetear contraseñas de usuarios en caso de emergencia.

```powershell
# Compilar (autocontenido + recortado, ~15MB)
dotnet publish SILF.Recovery -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -o .\publish\Recovery

# Resultado: publish\Recovery\SilfMaintenance.exe
```

**Uso:**
1. Copiar `SilfMaintenance.exe` junto a `silf.db` (carpeta de instalación)
2. Ejecutar con doble clic
3. Ingresar clave de acceso (ver documentación interna)
4. Opciones: listar usuarios, resetear contraseña, activar/desactivar usuario

---

## 15. Repositorio

- **GitHub:** https://github.com/claudecorreo2077-commits/SILF0
- **Branch:** main

---

## 16. Pendientes

- **Testing con datos reales** — Probar todos los módulos con datos del Excel original del cliente
- **Ajustes post-testing** — Correcciones que surjan de las pruebas con el cliente

---

## Licencia

Proyecto privado — uso interno.
