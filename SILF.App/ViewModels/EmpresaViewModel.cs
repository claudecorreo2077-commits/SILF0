// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\EmpresaViewModel.cs
using System.IO;
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
    [ObservableProperty] private string? _empLogoPath;
    [ObservableProperty] private decimal _tipoCambio = 6.97m;
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
        EmpLogoPath = emp.LogoPath;
        TipoCambio = emp.TipoCambio;
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
        if (TipoCambio <= 0)
        { Mensaje = "El tipo de cambio debe ser mayor a 0."; MensajeVisible = true; return; }

        using var db = new SilfDbContext();
        var emp = await db.Empresas.FindAsync(_empresaId);
        if (emp is null) return;

        emp.RazonSocial = EmpRazonSocial.Trim();
        emp.NIT = string.IsNullOrWhiteSpace(EmpNit) ? null : EmpNit.Trim();
        emp.Direccion = string.IsNullOrWhiteSpace(EmpDireccion) ? null : EmpDireccion.Trim();
        emp.Telefono = string.IsNullOrWhiteSpace(EmpTelefono) ? null : EmpTelefono.Trim();
        emp.Municipio = string.IsNullOrWhiteSpace(EmpMunicipio) ? null : EmpMunicipio.Trim();
        emp.Ingenio = string.IsNullOrWhiteSpace(EmpIngenio) ? null : EmpIngenio.Trim();
        emp.LogoPath = EmpLogoPath;
        emp.TipoCambio = TipoCambio;

        await db.SaveChangesAsync();
        Mensaje = "✓ Datos guardados correctamente.";
        MensajeVisible = true;
    }
}
