using Microsoft.Extensions.Options;

namespace FirmaYa.Services;

public sealed class AplicacionOptions
{
    public string? UrlBase { get; set; }

    public string ZonaHoraria { get; set; } = "America/Argentina/Buenos_Aires";
}

public sealed class HoraLocal(TimeProvider reloj, IOptions<AplicacionOptions> opciones)
{
    private readonly TimeZoneInfo _zona = BuscarZona(opciones.Value.ZonaHoraria);

    public DateTime AhoraUtc => reloj.GetUtcNow().UtcDateTime;

    public DateOnly Hoy => DateOnly.FromDateTime(ALocal(AhoraUtc));

    public DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _zona);

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
