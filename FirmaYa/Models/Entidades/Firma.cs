namespace FirmaYa.Models.Entidades;

/// <summary>Tabla firma del DER. fecha_firma en UTC.</summary>
public sealed class Firma
{
    public int IdFirma { get; set; }
    public int IdVersion { get; set; }
    public int IdParte { get; set; }
    public string EstadoFirma { get; set; } = EstadosFirma.Pendiente;
    public DateTime? FechaFirma { get; set; }
    public string? HashFirma { get; set; }
}

/// <summary>"datosFirmas(nombreFirmante, fechaHoraFirma, ipFirmante, hashVersion)" del CU-10.</summary>
public sealed record DatosFirma(
    string NombreFirmante, DateTime FechaHoraFirma, string? IpFirmante, string HashVersion, string? HashFirma);
