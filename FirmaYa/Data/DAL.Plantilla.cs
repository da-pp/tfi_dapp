using FirmaYa.Models.Entidades;
using Npgsql;

namespace FirmaYa.Data;

/// <summary>Operaciones de :DAL invocadas por :PlantillaBLL (CU-01).</summary>
public sealed partial class DAL
{
    private const string SelectPlantilla = """
        SELECT id_plantilla, nombre, tipo_contrato, descripcion_uso, cuerpo, estado, COALESCE(version, 1)
        FROM plantilla
        """;

    public Task<List<Plantilla>> ObtenerPlantillasActivas(CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            ListaAsync(Comando(SelectPlantilla + " WHERE estado = $1 ORDER BY tipo_contrato, version DESC", conexion, null,
                EstadosPlantilla.Activa), LeerPlantilla, token), ct);

    /// <summary>La plantilla activa del tipo pedido; si hay varias versiones, la más reciente.</summary>
    public Task<Plantilla?> ObtenerPlantillaPorTipo(string tipoPlantilla, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            PrimeroAsync(Comando(SelectPlantilla + " WHERE estado = $1 AND tipo_contrato = $2 ORDER BY version DESC, id_plantilla DESC LIMIT 1",
                conexion, null, EstadosPlantilla.Activa, tipoPlantilla), LeerPlantilla, token), ct);

    private static Plantilla LeerPlantilla(NpgsqlDataReader lector) => new()
    {
        IdPlantilla = lector.GetInt32(0),
        Nombre = Texto(lector, 1),
        TipoContrato = Texto(lector, 2),
        DescripcionUso = Texto(lector, 3),
        Cuerpo = Texto(lector, 4),
        Estado = Texto(lector, 5),
        Version = lector.GetInt32(6)
    };
}
