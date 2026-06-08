// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\SetupWizardViewModel.cs
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SILF.Data;

namespace SILF.App.ViewModels;

public partial class SetupWizardViewModel : ObservableObject
{
    // ══════════════════════════════════════════
    // NAVEGACIÓN
    // ══════════════════════════════════════════

    [ObservableProperty] private int _pasoActual = 1;
    [ObservableProperty] private string _mensajeError = "";
    [ObservableProperty] private bool _tieneError;

    public bool EsPaso1 => PasoActual == 1;
    public bool EsPaso2 => PasoActual == 2;
    public bool EsPaso3 => PasoActual == 3;
    public bool PuedeRetroceder => PasoActual > 1;
    public string TextoBotonSiguiente => PasoActual == 3 ? "FINALIZAR" : "SIGUIENTE";

    partial void OnPasoActualChanged(int value)
    {
        OnPropertyChanged(nameof(EsPaso1));
        OnPropertyChanged(nameof(EsPaso2));
        OnPropertyChanged(nameof(EsPaso3));
        OnPropertyChanged(nameof(PuedeRetroceder));
        OnPropertyChanged(nameof(TextoBotonSiguiente));
        TieneError = false;
        MensajeError = "";
    }

    // ══════════════════════════════════════════
    // PASO 1: EMPRESA
    // ══════════════════════════════════════════

    [ObservableProperty] private string _empresaNombre = "";
    [ObservableProperty] private string _empresaMunicipio = "";
    [ObservableProperty] private string _empresaIngenio = "";

    // ── Dos tipos de cambio independientes ──
    [ObservableProperty] private decimal _tipoCambioRegalias = 6.96m;
    [ObservableProperty] private decimal _tipoCambioGeneral = 6.90m;

    [ObservableProperty] private string? _logoPath;
    [ObservableProperty] private string _logoInfo = "Sin logo seleccionado";
    [ObservableProperty] private BitmapImage? _logoPreview;
    [ObservableProperty] private bool _tieneLogoCargado;

    // ══════════════════════════════════════════
    // PASO 2: ADMINISTRADOR
    // ══════════════════════════════════════════

    [ObservableProperty] private string _adminNombreCompleto = "";
    [ObservableProperty] private string _adminUsuario = "";
    [ObservableProperty] private string _adminPassword = "";
    [ObservableProperty] private string _adminConfirmarPassword = "";
    [ObservableProperty] private bool _mostrarPassword;
    [ObservableProperty] private string _passwordTextoVisible = "";
    [ObservableProperty] private string _confirmarPasswordTextoVisible = "";

    /// <summary>True si ambas contraseñas tienen texto y coinciden.</summary>
    public bool PasswordsCoinciden =>
        !string.IsNullOrEmpty(AdminPassword) &&
        AdminPassword == AdminConfirmarPassword;

    public string PasswordMatchIcon => PasswordsCoinciden ? "✓" : "✗";
    public string PasswordMatchColor => PasswordsCoinciden ? "#66BB6A" : "#FF6B6B";
    public bool MostrarIndicadorMatch =>
        !string.IsNullOrEmpty(AdminPassword) && !string.IsNullOrEmpty(AdminConfirmarPassword);

    partial void OnAdminPasswordChanged(string value)
    {
        OnPropertyChanged(nameof(PasswordsCoinciden));
        OnPropertyChanged(nameof(PasswordMatchIcon));
        OnPropertyChanged(nameof(PasswordMatchColor));
        OnPropertyChanged(nameof(MostrarIndicadorMatch));
    }

    partial void OnAdminConfirmarPasswordChanged(string value)
    {
        OnPropertyChanged(nameof(PasswordsCoinciden));
        OnPropertyChanged(nameof(PasswordMatchIcon));
        OnPropertyChanged(nameof(PasswordMatchColor));
        OnPropertyChanged(nameof(MostrarIndicadorMatch));
    }

    // ══════════════════════════════════════════
    // RESULTADO
    // ══════════════════════════════════════════

    public bool Completado { get; private set; }
    public Action? CerrarVentana { get; set; }

    // ══════════════════════════════════════════
    // COMANDOS
    // ══════════════════════════════════════════

    [RelayCommand]
    private void SeleccionarLogo()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Seleccionar logo de la empresa",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp"
        };

        if (ofd.ShowDialog() == true)
        {
            LogoPath = ofd.FileName;
            LogoInfo = Path.GetFileName(ofd.FileName);

            // Crear preview
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(ofd.FileName);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 120;
                bitmap.EndInit();
                LogoPreview = bitmap;
                TieneLogoCargado = true;
            }
            catch
            {
                LogoPreview = null;
                TieneLogoCargado = false;
            }
        }
    }

    [RelayCommand]
    private void TogglePassword()
    {
        MostrarPassword = !MostrarPassword;
        if (MostrarPassword)
        {
            PasswordTextoVisible = AdminPassword;
            ConfirmarPasswordTextoVisible = AdminConfirmarPassword;
        }
    }

    [RelayCommand]
    private void Retroceder()
    {
        if (PasoActual > 1) PasoActual--;
    }

    [RelayCommand]
    private async Task SiguienteAsync()
    {
        TieneError = false;

        switch (PasoActual)
        {
            case 1:
                if (string.IsNullOrWhiteSpace(EmpresaNombre))
                {
                    MensajeError = "Ingrese el nombre de la empresa.";
                    TieneError = true;
                    return;
                }
                if (TipoCambioRegalias <= 0)
                {
                    MensajeError = "El T/C para Regalías debe ser mayor a cero.";
                    TieneError = true;
                    return;
                }
                if (TipoCambioGeneral <= 0)
                {
                    MensajeError = "El T/C General debe ser mayor a cero.";
                    TieneError = true;
                    return;
                }
                PasoActual = 2;
                break;

            case 2:
                if (string.IsNullOrWhiteSpace(AdminNombreCompleto))
                {
                    MensajeError = "Ingrese el nombre completo del administrador.";
                    TieneError = true;
                    return;
                }
                if (string.IsNullOrWhiteSpace(AdminUsuario))
                {
                    MensajeError = "Ingrese un nombre de usuario.";
                    TieneError = true;
                    return;
                }
                if (AdminPassword.Length < 4)
                {
                    MensajeError = "La contraseña debe tener al menos 4 caracteres.";
                    TieneError = true;
                    return;
                }
                if (AdminPassword != AdminConfirmarPassword)
                {
                    MensajeError = "Las contraseñas no coinciden.";
                    TieneError = true;
                    return;
                }
                PasoActual = 3;
                break;

            case 3:
                await FinalizarAsync();
                break;
        }
    }

    private async Task FinalizarAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            var empresa = await db.Empresas.FirstOrDefaultAsync();
            if (empresa != null)
            {
                empresa.RazonSocial = EmpresaNombre.Trim();
                empresa.Municipio = string.IsNullOrWhiteSpace(EmpresaMunicipio) ? null : EmpresaMunicipio.Trim();
                empresa.Ingenio = string.IsNullOrWhiteSpace(EmpresaIngenio) ? null : EmpresaIngenio.Trim();
                empresa.TipoCambioRegalias = TipoCambioRegalias;
                empresa.TipoCambioGeneral = TipoCambioGeneral;
                empresa.NombreLiquidador = AdminNombreCompleto.Trim();

                if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
                {
                    var destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Images");
                    Directory.CreateDirectory(destDir);
                    var destPath = Path.Combine(destDir, "empresa-logo" + Path.GetExtension(LogoPath));
                    File.Copy(LogoPath, destPath, true);
                    empresa.LogoPath = destPath;
                }
            }

            var admin = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == 1);
            if (admin != null)
            {
                admin.NombreCompleto = AdminNombreCompleto.Trim();
                admin.NombreUsuario = AdminUsuario.Trim();
                admin.PasswordHash = ComputeSha256(AdminPassword);
            }

            await db.SaveChangesAsync();
            Completado = true;
            CerrarVentana?.Invoke();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar: {ex.Message}";
            TieneError = true;
        }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
