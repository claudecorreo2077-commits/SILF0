// Ruta: D:\ARCHIVOS\POTOSI\SILF\SILF.App\ViewModels\ReciboPreviewViewModel.cs
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SILF.Core.Helpers;
using SILF.Core.Models;
using SILF.Data;
using SILF.Reports;

namespace SILF.App.ViewModels;

public partial class ReciboPreviewViewModel : BaseViewModel
{
    [ObservableProperty] private int _numeroRecibo;
    [ObservableProperty] private decimal _monto;
    [ObservableProperty] private string _montoFormateado = "";
    [ObservableProperty] private string _beneficiario = "";
    [ObservableProperty] private string _montoEnLetras = "";
    [ObservableProperty] private string _concepto = "";
    [ObservableProperty] private DateTime _fecha = DateTime.Now;
    [ObservableProperty] private string _fechaTexto = "";

    // Empresa
    [ObservableProperty] private string _empresaNombre = "Empresa Minera";
    [ObservableProperty] private string _empresaNit = "";
    [ObservableProperty] private string _empresaMunicipio = "";
    [ObservableProperty] private BitmapImage? _logoImage;

    public bool TieneNit => !string.IsNullOrWhiteSpace(EmpresaNit);
    public bool TieneMunicipio => !string.IsNullOrWhiteSpace(EmpresaMunicipio);
    partial void OnEmpresaNitChanged(string value) => OnPropertyChanged(nameof(TieneNit));
    partial void OnEmpresaMunicipioChanged(string value) => OnPropertyChanged(nameof(TieneMunicipio));

    // Firmas dinámicas según tipo de movimiento
    [ObservableProperty] private string _firmaEntrego = "";
    [ObservableProperty] private string _firmaRecibio = "";

    // Etiqueta copia empresa con tipo
    [ObservableProperty] private string _etiquetaCopiaEmpresa = "COPIA EMPRESA";

    // Label dinámico: "RECIBO DEL SR.(A):" para entrada, "PÁGUESE AL SR.(A):" para salida
    [ObservableProperty] private string _labelBeneficiario = "RECIBO DEL SR.(A):";

    // QR
    [ObservableProperty] private BitmapImage? _qrImage;

    public Func<Task>? OnVolver { get; set; }

    private ReciboCaja? _reciboActual;
    private Empresa? _empresa;
    private byte[]? _logoBytes;

    [RelayCommand]
    public async Task CargarReciboAsync(int reciboId)
    {
        Cargando = true;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SilfDbContext>();

            _reciboActual = await db.RecibosCaja.FindAsync(reciboId);
            if (_reciboActual == null)
            {
                MessageBox.Show("Recibo no encontrado.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Empresa + logo
            _empresa = await db.Empresas.FirstOrDefaultAsync();
            if (_empresa != null)
            {
                EmpresaNombre = _empresa.RazonSocial;
                EmpresaNit = _empresa.NIT ?? "";
                EmpresaMunicipio = _empresa.Municipio ?? "";

                if (!string.IsNullOrWhiteSpace(_empresa.LogoPath) && File.Exists(_empresa.LogoPath))
                {
                    _logoBytes = await File.ReadAllBytesAsync(_empresa.LogoPath);
                    LogoImage = BytesToBitmap(_logoBytes);
                }
            }

            // Datos del recibo
            NumeroRecibo = _reciboActual.NumeroRecibo;
            Monto = _reciboActual.Monto;
            MontoFormateado = _reciboActual.Monto.ToString("N2");
            Beneficiario = _reciboActual.Beneficiario;
            MontoEnLetras = _reciboActual.MontoEnLetras ?? NumeroALetras.Convertir(_reciboActual.Monto);
            Concepto = _reciboActual.Concepto;
            Fecha = _reciboActual.Fecha;
            FechaTexto = FormatearFechaTexto(_reciboActual.Fecha);

            // Firmas según tipo de movimiento:
            //   Salida  → Empresa ENTREGA, Beneficiario RECIBE
            //   Entrada → Beneficiario ENTREGA, Empresa RECIBE
            var liquidador = _empresa?.NombreLiquidador ?? "";
            if (_reciboActual.TipoMovimiento == "Entrada")
            {
                FirmaEntrego = _reciboActual.Beneficiario;
                FirmaRecibio = liquidador;
            }
            else // Salida
            {
                FirmaEntrego = liquidador;
                FirmaRecibio = _reciboActual.Beneficiario;
            }

            EtiquetaCopiaEmpresa = $"COPIA EMPRESA - {_reciboActual.TipoMovimiento.ToUpperInvariant()}";

            // Label dinámico según tipo
            LabelBeneficiario = _reciboActual.TipoMovimiento == "Salida"
                ? "PÁGUESE AL SR.(A):" : "RECIBO DEL SR.(A):";

            // QR
            var qrData = QrHelper.DatosRecibo(NumeroRecibo, Monto, Fecha,
                Beneficiario, _reciboActual.TipoMovimiento, FirmaEntrego, FirmaRecibio, Concepto);
            var qrBytes = QrHelper.GenerarPng(qrData, 6);
            QrImage = BytesToBitmap(qrBytes);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private void ImprimirRecibo(object? parameter)
    {
        if (parameter is not FrameworkElement visualElement) return;
        try
        {
            var printArea = FindChild<StackPanel>(visualElement, "PrintArea");
            if (printArea == null) { MessageBox.Show("No se encontró el área de impresión.", "SILF"); return; }

            var pd = new PrintDialog();

            // Configurar tamaño carta (Letter: 8.5 x 11 pulgadas = 816 x 1056 DIP a 96dpi)
            if (pd.PrintTicket != null)
            {
                pd.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(
                    System.Printing.PageMediaSizeName.NorthAmericaLetter, 816, 1056);
                pd.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;
            }

            if (pd.ShowDialog() == true)
            {
                // Área imprimible
                var areaW = pd.PrintableAreaWidth;
                var areaH = pd.PrintableAreaHeight;

                // Medir el contenido
                printArea.Measure(new Size(areaW, areaH));
                printArea.Arrange(new Rect(new Point(0, 0), printArea.DesiredSize));

                // Escalar para que quepa en la página
                double scaleX = areaW / printArea.ActualWidth;
                double scaleY = areaH / printArea.ActualHeight;
                double scale = Math.Min(scaleX, scaleY);

                printArea.LayoutTransform = new ScaleTransform(scale, scale);
                printArea.Measure(new Size(areaW, areaH));
                printArea.Arrange(new Rect(new Point(0, 0),
                    new Size(printArea.DesiredSize.Width, printArea.DesiredSize.Height)));

                pd.PrintVisual(printArea, $"Recibo Nº {NumeroRecibo}");

                // Restaurar escala original
                printArea.LayoutTransform = Transform.Identity;
                printArea.InvalidateMeasure();

                MessageBox.Show("Recibo enviado a la impresora.", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al imprimir: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ExportarPdf()
    {
        if (_reciboActual == null) return;
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"Recibo_{NumeroRecibo}.pdf",
                Title = "Exportar Recibo como PDF"
            };
            if (dialog.ShowDialog() == true)
            {
                ReciboPdfGenerator.Generar(_reciboActual, dialog.FileName, _empresa, _logoBytes);
                MessageBox.Show($"PDF guardado en:\n{dialog.FileName}", "SILF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al exportar PDF: {ex.Message}", "SILF",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task VolverAsync()
    {
        if (OnVolver != null) await OnVolver();
    }

    private static BitmapImage? BytesToBitmap(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return null;
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static string FormatearFechaTexto(DateTime fecha)
    {
        var culture = new CultureInfo("es-BO");
        string dia = culture.DateTimeFormat.GetDayName(fecha.DayOfWeek).ToUpperInvariant();
        string mes = culture.DateTimeFormat.GetMonthName(fecha.Month).ToUpperInvariant();
        return $"{dia} {fecha.Day:00} DE {mes} DE {fecha.Year}";
    }

    private static T? FindChild<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t && t.Name == name) return t;
            var found = FindChild<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
