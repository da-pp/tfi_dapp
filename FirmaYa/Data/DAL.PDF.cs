using FirmaYa.Models.Entidades;

namespace FirmaYa.Data;

/// <summary>Operaciones de :DAL invocadas por :PDFBLL (CU-10).</summary>
public sealed partial class DAL
{
    public Task<VersionContrato?> ObtenerContenidoVersionFinal(int idContrato, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            PrimeroAsync(Comando("""
                SELECT id_version, id_contrato, id_usuario_autor, numero_version, contenido, hash_sha256, fecha_creacion
                FROM version_contrato
                WHERE id_contrato = $1
                ORDER BY numero_version DESC
                LIMIT 1
                """, conexion, null, idContrato),
                lector => new VersionContrato
                {
                    IdVersion = lector.GetInt32(0),
                    IdContrato = lector.GetInt32(1),
                    IdUsuarioAutor = Entero(lector, 2),
                    NumeroVersion = lector.GetInt32(3),
                    Contenido = Texto(lector, 4) ?? "",
                    HashSha256 = Texto(lector, 5) ?? "",
                    FechaCreacion = FechaUtc(lector, 6) ?? DateTime.MinValue
                }, token), ct);

    /// <summary>Firmas completadas de la versión actual, con la IP registrada en auditoría.</summary>
    public Task<List<DatosFirma>> ObtenerDatosFirmas(int idContrato, CancellationToken ct) =>
        EjecutarConsulta(async (conexion, token) =>
        {
            var firmantes = await ListaAsync(Comando(SelectParteConFirma + " WHERE p.id_contrato = $1 AND p.rol_parte = $2 ORDER BY p.id_parte",
                conexion, null, idContrato, RolesParte.Firmante), LeerParteConFirma, token);
            var hashVersion = (string?)await EscalarAsync(Comando("""
                SELECT hash_sha256 FROM version_contrato WHERE id_contrato = $1 ORDER BY numero_version DESC LIMIT 1
                """, conexion, null, idContrato), token) ?? "";

            return firmantes
                .Where(f => f.EstadoFirma == EstadosFirma.Firmado && f.FechaHoraEvento is not null)
                .Select(f => new DatosFirma(f.NombreVisible, f.FechaHoraEvento!.Value, f.IpFirma, hashVersion, f.HashFirma))
                .ToList();
        }, ct);
}
