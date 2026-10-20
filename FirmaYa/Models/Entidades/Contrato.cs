namespace FirmaYa.Models.Entidades;

public sealed class Contrato
{
    public int IdContrato { get; set; }
    public int? IdUsuarioCreador { get; set; }
    public string? NombreCreador { get; set; }
    public string? EmailCreador { get; set; }
    public int? IdEstado { get; set; }
    public string? Estado { get; set; }
    public int? IdPlantilla { get; set; }
    public string? Nombre { get; set; }
    public string? DescripcionPropiedad { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaExpiracion { get; set; }

    public int? IdVersion { get; set; }
    public int NumeroVersion { get; set; }
    public string? HashVersion { get; set; }
    public string? ContenidoActual { get; set; }
    public DateTime? FechaUltimaModificacion { get; set; }

    public List<string> TransicionesDisponibles { get; set; } = [];

    public bool EsTitular(int idUsuario) => IdUsuarioCreador == idUsuario;
}

public static class EstadosContrato
{
    public const string Borrador = "Borrador";
    public const string EnRevision = "En Revisión";
    public const string ListoParaFirmar = "Listo para firmar";
    public const string Firmado = "Firmado";
    public const string Archivado = "Archivado";

    public static bool EsEditable(string? estado) => estado is Borrador or EnRevision;
}
