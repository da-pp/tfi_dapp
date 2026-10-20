namespace FirmaYa.Models.Entidades;

/// <summary>
/// Tabla contrato del DER, con el nombre del estado (estado_contrato), el creador (usuario)
/// y los datos de la versión actual (version_contrato con el mayor numero_version).
/// Es el "datosContrato" que :DAL devuelve en los CU-02, CU-04, CU-05, CU-08 y CU-10.
/// </summary>
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

    // Versión actual.
    public int? IdVersion { get; set; }
    public int NumeroVersion { get; set; }
    public string? HashVersion { get; set; }
    public string? ContenidoActual { get; set; }
    public DateTime? FechaUltimaModificacion { get; set; }

    /// <summary>Destinos de transicion_estado cuyo origen es el estado actual (CU-05).</summary>
    public List<string> TransicionesDisponibles { get; set; } = [];

    public bool EsTitular(int idUsuario) => IdUsuarioCreador == idUsuario;
}

/// <summary>Valores de estado_contrato.nombre (CU-05, CU-17 paso 13).</summary>
public static class EstadosContrato
{
    public const string Borrador = "Borrador";
    public const string EnRevision = "En Revisión";
    public const string ListoParaFirmar = "Listo para firmar";
    public const string Firmado = "Firmado";
    public const string Archivado = "Archivado";

    /// <summary>CU-02: solo se edita en Borrador o En Revisión.</summary>
    public static bool EsEditable(string? estado) => estado is Borrador or EnRevision;
}
