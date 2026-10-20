namespace FirmaYa.Models.Entidades;

/// <summary>Tabla version_contrato del DER. fecha_creacion en UTC.</summary>
public sealed class VersionContrato
{
    public int IdVersion { get; set; }
    public int IdContrato { get; set; }
    public int? IdUsuarioAutor { get; set; }
    public int NumeroVersion { get; set; }
    public string Contenido { get; set; } = "";
    public string HashSha256 { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
}
