using Microsoft.Extensions.Options;

namespace FirmaYa.Services;

public sealed class AplicacionOptions
{
    /// <summary>URL pública de la aplicación para los enlaces enviados por correo. Vacía: se usa la del request actual.</summary>
    public string? UrlBase { get; set; }

    /// <summary>Zona horaria para la "fecha actual" de las validaciones y para mostrar fechas.</summary>
    public string ZonaHoraria { get; set; } = "America/Argentina/Buenos_Aires";
}

/// <summary>Las fechas se guardan en UTC; esta clase da la fecha y hora locales para validar y mostrar.</summary>
public sealed class HoraLocal(TimeProvider reloj, IOptions<AplicacionOptions> opciones)
{
    private readonly TimeZoneInfo _zona = BuscarZona(opciones.Value.ZonaHoraria);

    public DateTime AhoraUtc => reloj.GetUtcNow().UtcDateTime;

    public DateOnly Hoy => DateOnly.FromDateTime(ALocal(AhoraUtc));

    public DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zona);

    /// <summary>Formato DD/MM/AAAA HH:mm del documento.</summary>
    public string Formatear(DateTime? utc) => utc is null ? "—" : ALocal(utc.Value).ToString("dd/MM/yyyy HH:mm");

    private static TimeZoneInfo BuscarZona(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
