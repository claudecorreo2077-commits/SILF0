// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\EmpresaViewModel.cs
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class EmpresaViewModel : BaseViewModel
{
    [ObservableProperty] private string _empRazonSocial = string.Empty;
    [ObservableProperty] private string _empNit = string.Empty;
    [ObservableProperty] private string _empDireccion = string.Empty;
    [ObservableProperty] private string _empTelefono = string.Empty;
    [ObservableProperty] private string _empMunicipio = string.Empty;
    [ObservableProperty] private string _empIngenio = string.Empty;
    [ObservableProperty] private string _empNombreLiquidador = string.Empty;
    [ObservableProperty] private string? _empLogoPath;

    // ── Dos tipos de cambio independientes ──
    [ObservableProperty] private decimal _tipoCambioRegalias = 6.96m;
    [ObservableProperty] private decimal _tipoCambioGeneral = 6.90m;

    [ObservableProperty] private string _mensaje = string.Empty;
    [ObservableProperty] private bool _mensajeVisible;

    private int _empresaId;

    [RelayCommand]
    public async Task CargarDatos()
    {
        using var db = new SilfDbContext();
        var emp = await db.Empresas.FirstOrDefaultAsync();
        if (emp is null) return;

        _empresaId = emp.Id;
        EmpRazonSocial = emp.RazonSocial;
        EmpNit = emp.NIT ?? string.Empty;
        EmpDireccion = emp.Direccion ?? string.Empty;
        EmpTelefono = emp.Telefono ?? string.Empty;
        EmpMunicipio = emp.Municipio ?? string.Empty;
        EmpIngenio = emp.Ingenio ?? string.Empty;
        EmpNombreLiquidador = emp.NombreLiquidador ?? string.Empty;
        EmpLogoPath = emp.LogoPath;
        TipoCambioRegalias = emp.TipoCambioRegalias;
        TipoCambioGeneral = emp.TipoCambioGeneral;
    }

    [RelayCommand]
    private void SeleccionarLogo()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar logo de la empresa",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp|Todos|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            var destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
            Directory.CreateDirectory(destDir);
            var fileName = $"logo{Path.GetExtension(dialog.FileName)}";
            var destPath = Path.Combine(destDir, fileName);
            File.Copy(dialog.FileName, destPath, overwrite: true);
            EmpLogoPath = destPath;
        }
    }

    [RelayCommand]
    private async Task Guardar()
    {
        if (string.IsNullOrWhiteSpace(EmpRazonSocial))
        { Mensaje = "La razón social es obligatoria."; MensajeVisible = true; return; }

        if (TipoCambioRegalias <= 0)
        { Mensaje = "El tipo de cambio de Regalías debe ser mayor a 0."; MensajeVisible = true; return; }

        if (TipoCambioGeneral <= 0)
        { Mensaje = "El tipo de cambio General debe ser mayor a 0."; MensajeVisible = true; return; }

        using var db = new SilfDbContext();
        var emp = await db.Empresas.FindAsync(_empresaId);
        if (emp is null) return;

        emp.RazonSocial = EmpRazonSocial.Trim();
        emp.NIT = string.IsNullOrWhiteSpace(EmpNit) ? null : EmpNit.Trim();
        emp.Direccion = string.IsNullOrWhiteSpace(EmpDireccion) ? null : EmpDireccion.Trim();
        emp.Telefono = string.IsNullOrWhiteSpace(EmpTelefono) ? null : EmpTelefono.Trim();
        emp.Municipio = string.IsNullOrWhiteSpace(EmpMunicipio) ? null : EmpMunicipio.Trim();
        emp.Ingenio = string.IsNullOrWhiteSpace(EmpIngenio) ? null : EmpIngenio.Trim();
        emp.NombreLiquidador = string.IsNullOrWhiteSpace(EmpNombreLiquidador) ? null : EmpNombreLiquidador.Trim();
        emp.LogoPath = EmpLogoPath;
        emp.TipoCambioRegalias = TipoCambioRegalias;
        emp.TipoCambioGeneral = TipoCambioGeneral;

        await db.SaveChangesAsync();
        Mensaje = "✓ Datos guardados correctamente.";
        MensajeVisible = true;
    }

    // ══════════════════════════════════════════
    // EXPORTAR / IMPORTAR BASE DE DATOS
    // ══════════════════════════════════════════

    private static string RutaBd =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "silf.db");

    [RelayCommand]
    private void ExportarBd()
    {
        var sfd = new SaveFileDialog
        {
            Title = "Exportar Base de Datos",
            Filter = "Base de datos SQLite|*.db",
            FileName = $"silf_backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
        };

        if (sfd.ShowDialog() != true) return;

        try
        {
            File.Copy(RutaBd, sfd.FileName, overwrite: true);
            Mensaje = $"✓ Base de datos exportada a: {Path.GetFileName(sfd.FileName)}";
            MensajeVisible = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al exportar:\n{ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ImportarBd()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Importar Base de Datos",
            Filter = "Base de datos SQLite|*.db"
        };

        if (ofd.ShowDialog() != true) return;

        var resultado = MessageBox.Show(
            "⚠ ATENCIÓN: Esto reemplazará TODOS los datos actuales con los del archivo seleccionado.\n\n" +
            "Se creará un respaldo automático antes de importar.\n\n" +
            "¿Desea continuar?",
            "SILF — Importar Base de Datos",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (resultado != MessageBoxResult.Yes) return;

        try
        {
            // Crear respaldo automático
            var backupPath = Path.Combine(
                Path.GetDirectoryName(RutaBd)!,
                $"silf_antes_importar_{DateTime.Now:yyyyMMdd_HHmm}.db");
            File.Copy(RutaBd, backupPath, overwrite: true);

            // Copiar la BD importada
            File.Copy(ofd.FileName, RutaBd, overwrite: true);

            MessageBox.Show(
                "Base de datos importada correctamente.\n\n" +
                $"Respaldo guardado en:\n{Path.GetFileName(backupPath)}\n\n" +
                "La aplicación se reiniciará ahora.",
                "SILF", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reiniciar la app
            var exePath = Environment.ProcessPath;
            if (exePath != null)
            {
                Process.Start(exePath);
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al importar:\n{ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
