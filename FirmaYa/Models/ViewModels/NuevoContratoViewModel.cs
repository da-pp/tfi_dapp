using System.Globalization;

namespace FirmaYa.Models.ViewModels;

/// <summary>Formulario de alta de contrato del CU-01 (pasos 6 a 10) y sus validaciones (paso 16).</summary>
public sealed class NuevoContratoViewModel
{
    public const string MensajeObligatorio = "Este campo es obligatorio";
    public const string MensajeFechaInicio = "La fecha debe tener el formato DD/MM/AAAA y no puede ser anterior a la fecha de hoy";

    public string? NombreContrato { get; set; }
    public string? PartesInvolucradas { get; set; }
    public string? FechaInicio { get; set; }
    public string? FechaExpiracion { get; set; }
    public string? DescripcionPropiedad { get; set; }

    public DateOnly FechaInicioValor { get; private set; }
    public DateOnly? FechaExpiracionValor { get; private set; }

    public Dictionary<string, string> Validar(DateOnly hoy)
    {
        var errores = new Dictionary<string, string>();

        Texto(nameof(NombreContrato), NombreContrato, obligatorio: true, maximo: 200);
        Texto(nameof(PartesInvolucradas), PartesInvolucradas, obligatorio: true, maximo: 1000);
        Texto(nameof(DescripcionPropiedad), DescripcionPropiedad, obligatorio: false, maximo: 2000);

        if (string.IsNullOrWhiteSpace(FechaInicio))
            errores[nameof(FechaInicio)] = MensajeObligatorio;
        else if (!TryFecha(FechaInicio, out var inicio) || inicio < hoy)
            errores[nameof(FechaInicio)] = MensajeFechaInicio;
        else
            FechaInicioValor = inicio;

        if (!string.IsNullOrWhiteSpace(FechaExpiracion))
        {
            if (TryFecha(FechaExpiracion, out var expiracion))
                FechaExpiracionValor = expiracion;
            else
                errores[nameof(FechaExpiracion)] = "La fecha debe tener el formato DD/MM/AAAA";
        }

        return errores;

        void Texto(string campo, string? valor, bool obligatorio, int maximo)
        {
            if (obligatorio && string.IsNullOrWhiteSpace(valor))
                errores[campo] = MensajeObligatorio;
            else if (valor?.Trim().Length > maximo)
                errores[campo] = $"Longitud máxima {maximo} caracteres";
        }
    }

    public static bool TryFecha(string? texto, out DateOnly fecha) =>
        DateOnly.TryParseExact(texto?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
}
