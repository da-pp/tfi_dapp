using FirmaYa.Models.Entidades;
using Npgsql;

namespace FirmaYa.Data;

public sealed partial class DAL
{
    private const string SelectContrato = """
        SELECT c.id_contrato, c.id_usuario_creador, trim(concat_ws(' ', u.nombre, u.apellido)), u.email,
               c.id_estado, e.nombre, c.id_plantilla, c.nombre, c.descripcion_propiedad, c.fecha_inicio, c.fecha_expiracion,
               v.id_version, COALESCE(v.numero_version, 0), v.hash_sha256, v.contenido, v.fecha_creacion,
               ARRAY(SELECT d.nombre::text
                     FROM transicion_estado t
                     JOIN estado_contrato d ON d.id_estado = t.id_estado_destino
                     WHERE t.id_estado_origen = c.id_estado
                     ORDER BY d.id_estado)
        FROM contrato c
        LEFT JOIN usuario u ON u.id_usuario = c.id_usuario_creador
        LEFT JOIN estado_contrato e ON e.id_estado = c.id_estado
        LEFT JOIN LATERAL (
            SELECT vc.id_version, vc.numero_version, vc.hash_sha256, vc.contenido, vc.fecha_creacion
            FROM version_contrato vc
            WHERE vc.id_contrato = c.id_contrato
            ORDER BY vc.numero_version DESC
            LIMIT 1) v ON TRUE
        """;

    private const string SelectParte = """
        SELECT p.id_parte, p.id_usuario, p.id_contrato, p.rol_parte, p.correo_invitado, p.estado_invitacion,
               p.fecha_invitacion, p.token_invitacion, trim(concat_ws(' ', u.nombre, u.apellido))
        FROM usuario_contrato p
        LEFT JOIN usuario u ON u.id_usuario = p.id_usuario
        """;

    // Los literales coinciden con TiposAccion.Firma, TiposNotificacion.SolicitudFirma y EstadosEnvio.Enviado.
    private const string SelectParteConFirma = """
        SELECT p.id_parte, p.id_usuario, p.id_contrato, p.rol_parte, p.correo_invitado, p.estado_invitacion,
               p.fecha_invitacion, p.token_invitacion, trim(concat_ws(' ', u.nombre, u.apellido)),
               f.estado_firma, f.fecha_firma, f.hash_firma,
               (SELECT a.direccion_ip FROM auditoria a
                WHERE a.tipo_accion = 'Firma' AND a.entidad_afectada = 'usuario_contrato' AND a.id_entidad_afectada = p.id_parte
                ORDER BY a.fecha_hora DESC LIMIT 1),
               (SELECT MAX(n.fecha_envio) FROM notificacion n
                WHERE n.id_contrato = p.id_contrato AND n.tipo = 'Solicitud de firma' AND n.estado_envio = 'Enviado'
                  AND lower(n.correo_destino) = lower(p.correo_invitado))
        FROM usuario_contrato p
        LEFT JOIN usuario u ON u.id_usuario = p.id_usuario
        LEFT JOIN LATERAL (
            SELECT fi.estado_firma, fi.fecha_firma, fi.hash_firma
            FROM firma fi
            WHERE fi.id_parte = p.id_parte
              AND fi.id_version = (SELECT vc.id_version FROM version_contrato vc
                                   WHERE vc.id_contrato = p.id_contrato ORDER BY vc.numero_version DESC LIMIT 1)
            ORDER BY fi.id_firma DESC
            LIMIT 1) f ON TRUE
        """;

    private const string VersionActual =
        "(SELECT id_version FROM version_contrato WHERE id_contrato = $1 ORDER BY numero_version DESC LIMIT 1)";

    public Task<int> GuardarContrato(Contrato contrato, string estado, int version, string hash, string contenidoVersion1,
        int usuarioCreador, DateTime fechaHora, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            const string sqlContrato = """
                INSERT INTO contrato (id_usuario_creador, id_estado, id_plantilla, nombre, descripcion_propiedad,
                                      fecha_inicio, fecha_expiracion)
                VALUES ($1, (SELECT id_estado FROM estado_contrato WHERE nombre = $2), $3, $4, $5, $6, $7)
                RETURNING id_contrato
                """;
            var idContrato = (int)(await EscalarAsync(Comando(sqlContrato, conexion, transaccion,
                usuarioCreador, estado, contrato.IdPlantilla, contrato.Nombre, contrato.DescripcionPropiedad,
                contrato.FechaInicio, contrato.FechaExpiracion), token))!;

            const string sqlVersion = """
                INSERT INTO version_contrato (id_contrato, id_usuario_autor, numero_version, contenido, hash_sha256, fecha_creacion)
                VALUES ($1, $2, $3, $4, $5, $6)
                """;
            await EjecutarAsync(Comando(sqlVersion, conexion, transaccion,
                idContrato, usuarioCreador, version, contenidoVersion1, hash, fechaHora), token);
            return idContrato;
        }, ct);

    public Task<Contrato?> ObtenerContrato(int idContrato, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            PrimeroAsync(Comando(SelectContrato + " WHERE c.id_contrato = $1", conexion, null, idContrato), LeerContrato, token), ct);

    public Task<bool> GuardarVersion(int idContrato, string contenidoNuevo, int numeroVersion, string hash, int autor,
        DateTime fechaHora, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            // Bloquea el contrato para que dos guardados simultáneos no generen el mismo número de versión.
            await EjecutarAsync(Comando("SELECT 1 FROM contrato WHERE id_contrato = $1 FOR UPDATE", conexion, transaccion, idContrato), token);

            const string sql = """
                INSERT INTO version_contrato (id_contrato, id_usuario_autor, numero_version, contenido, hash_sha256, fecha_creacion)
                SELECT $1, $2, $3, $4, $5, $6
                WHERE NOT EXISTS (SELECT 1 FROM version_contrato WHERE id_contrato = $1 AND numero_version = $3)
                """;
            return await EjecutarAsync(Comando(sql, conexion, transaccion,
                idContrato, autor, numeroVersion, contenidoNuevo, hash, fechaHora), token) == 1;
        }, ct);

    public Task ActualizarHistorialVersiones(int idContrato, int numeroVersion, int autor, DateTime fechaHora, string hash,
        string? comentarioVersion, CancellationToken ct) =>
        EjecutarModificacion((conexion, transaccion, token) =>
        {
            var descripcion = $"Versión v{numeroVersion} guardada. Hash: {hash}."
                + (string.IsNullOrWhiteSpace(comentarioVersion) ? "" : $" Comentario: {comentarioVersion}");
            return InsertarAuditoriaAsync(conexion, transaccion, autor, TiposAccion.Edicion, "contrato", idContrato,
                fechaHora, null, descripcion, token);
        }, ct);

    public Task<List<ParteContrato>> ObtenerPartesInvitadas(int idContrato, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            ListaAsync(Comando(SelectParte + " WHERE p.id_contrato = $1 ORDER BY p.id_parte", conexion, null, idContrato),
                LeerParte, token), ct);

    public Task<bool> ExisteCorreoInvitado(int idContrato, string correoElectronico, CancellationToken ct) =>
        EjecutarConsulta(async (conexion, token) =>
        {
            const string sql = """
                SELECT EXISTS (SELECT 1 FROM usuario_contrato
                               WHERE id_contrato = $1 AND lower(correo_invitado) = lower($2))
                """;
            return (bool)(await EscalarAsync(Comando(sql, conexion, null, idContrato, correoElectronico), token))!;
        }, ct);

    public Task<int> RegistrarParteInvitada(int idContrato, string correoElectronico, string rol, string token,
        string estadoInvitacion, DateTime fechaHora, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, cancelacion) =>
        {
            const string sql = """
                INSERT INTO usuario_contrato (id_usuario, id_contrato, rol_parte, correo_invitado, estado_invitacion,
                                              fecha_invitacion, token_invitacion)
                VALUES ((SELECT id_usuario FROM usuario WHERE lower(email) = lower($2)), $1, $3, $2, $5, $6, $4)
                RETURNING id_parte
                """;
            return (int)(await EscalarAsync(Comando(sql, conexion, transaccion,
                idContrato, correoElectronico, rol, token, estadoInvitacion, fechaHora), cancelacion))!;
        }, ct);

    public Task ActualizarEstadoInvitacion(int idContrato, string correoElectronico, string estado, CancellationToken ct) =>
        EjecutarModificacion((conexion, transaccion, token) =>
            EjecutarAsync(Comando("""
                UPDATE usuario_contrato SET estado_invitacion = $3
                WHERE id_contrato = $1 AND lower(correo_invitado) = lower($2)
                """, conexion, transaccion, idContrato, correoElectronico, estado), token), ct);

    public Task<Contrato?> ObtenerContrato(TokenEnlace token, CancellationToken ct) =>
        EjecutarConsulta((conexion, cancelacion) =>
            PrimeroAsync(Comando(SelectContrato + """
                 WHERE c.id_contrato = (SELECT id_contrato FROM usuario_contrato WHERE token_invitacion = $1)
                """, conexion, null, token.Valor), LeerContrato, cancelacion), ct);

    public Task<bool> ExisteFirmanteAsignado(int idContrato, CancellationToken ct) =>
        EjecutarConsulta(async (conexion, token) =>
            (bool)(await EscalarAsync(Comando("""
                SELECT EXISTS (SELECT 1 FROM usuario_contrato WHERE id_contrato = $1 AND rol_parte = $2)
                """, conexion, null, idContrato, RolesParte.Firmante), token))!, ct);

    public Task ActualizarEstadoContrato(int idContrato, string nuevoEstado, CancellationToken ct) =>
        EjecutarModificacion((conexion, transaccion, token) =>
            EjecutarAsync(Comando("""
                UPDATE contrato SET id_estado = (SELECT id_estado FROM estado_contrato WHERE nombre = $2)
                WHERE id_contrato = $1
                """, conexion, transaccion, idContrato, nuevoEstado), token), ct);

    public Task RegistrarAuditoria(int idContrato, int usuario, string estadoActual, string nuevoEstado, DateTime fechaHora,
        string? razonCambio, CancellationToken ct) =>
        EjecutarModificacion((conexion, transaccion, token) =>
        {
            var descripcion = $"Estado anterior: {estadoActual}. Nuevo estado: {nuevoEstado}."
                + (string.IsNullOrWhiteSpace(razonCambio) ? "" : $" Razón: {razonCambio}");
            return InsertarAuditoriaAsync(conexion, transaccion, usuario, TiposAccion.CambioEstado, "contrato", idContrato,
                fechaHora, null, descripcion, token);
        }, ct);

    public Task<string?> ObtenerRolUsuario(int idContrato, int usuario, CancellationToken ct) =>
        EjecutarConsulta(async (conexion, token) =>
        {
            const string sql = """
                SELECT CASE WHEN c.id_usuario_creador = $2 THEN $3
                            ELSE (SELECT p.rol_parte FROM usuario_contrato p
                                  WHERE p.id_contrato = c.id_contrato AND p.id_usuario = $2
                                  ORDER BY p.id_parte LIMIT 1)
                       END
                FROM contrato c
                WHERE c.id_contrato = $1
                """;
            return (string?)await EscalarAsync(Comando(sql, conexion, null, idContrato, usuario, RolesParte.Titular), token);
        }, ct);

    public Task<int> RegistrarComentario(int idContrato, int autor, DateTime fechaHora, string textoComentario, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
            (int)(await EscalarAsync(Comando("""
                INSERT INTO comentario (id_contrato, id_usuario, texto, fecha_publicacion)
                VALUES ($1, $2, $3, $4)
                RETURNING id_comentario
                """, conexion, transaccion, idContrato, autor, textoComentario, fechaHora), token))!, ct);

    public Task<List<Comentario>> ObtenerComentarios(int idContrato, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            ListaAsync(Comando("""
                SELECT co.id_comentario, co.id_contrato, co.id_usuario, trim(concat_ws(' ', u.nombre, u.apellido)),
                       co.texto, co.fecha_publicacion
                FROM comentario co
                LEFT JOIN usuario u ON u.id_usuario = co.id_usuario
                WHERE co.id_contrato = $1
                ORDER BY co.fecha_publicacion, co.id_comentario
                """, conexion, null, idContrato),
                lector => new Comentario
                {
                    IdComentario = lector.GetInt32(0),
                    IdContrato = lector.GetInt32(1),
                    IdUsuario = Entero(lector, 2),
                    NombreAutor = Texto(lector, 3),
                    Texto = Texto(lector, 4) ?? "",
                    FechaPublicacion = FechaUtc(lector, 5) ?? DateTime.MinValue
                }, token), ct);

    public Task<List<ParteContrato>> ObtenerFirmantes(int idContrato, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            ListaAsync(Comando(SelectParteConFirma + " WHERE p.id_contrato = $1 AND p.rol_parte = $2 ORDER BY p.id_parte",
                conexion, null, idContrato, RolesParte.Firmante), LeerParteConFirma, token), ct);

    public Task RegistrarSolicitudFirma(int idContrato, int firmante, DateTime fechaHora, string canalNotificacion,
        DateOnly? fechaLimiteFirma, int usuario, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            var sqlFirma = $"""
                INSERT INTO firma (id_version, id_parte, estado_firma)
                SELECT v.id_version, $2, $3
                FROM (SELECT {VersionActual} AS id_version) v
                WHERE v.id_version IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM firma f WHERE f.id_parte = $2 AND f.id_version = v.id_version)
                """;
            await EjecutarAsync(Comando(sqlFirma, conexion, transaccion, idContrato, firmante, EstadosFirma.Pendiente), token);

            var descripcion = $"Canal: {canalNotificacion}."
                + (fechaLimiteFirma is { } limite ? $" Fecha límite: {limite:dd/MM/yyyy}." : "");
            return await InsertarAuditoriaAsync(conexion, transaccion, usuario, TiposAccion.SolicitudFirma,
                "usuario_contrato", firmante, fechaHora, null, descripcion, token);
        }, ct);

    public Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, CancellationToken ct) =>
        EjecutarModificacion((conexion, transaccion, token) =>
            ActualizarEstadoFirmanteAsync(conexion, transaccion, idContrato, firmante, estado, token), ct);

    public Task<Contrato?> ObtenerContratoPorToken(TokenEnlace token, CancellationToken ct) => ObtenerContrato(token, ct);

    public Task RegistrarFirma(int idContrato, int firmante, DateTime fechaHora, string? ip, int versionContrato,
        string hashVersion, string hashFirma, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            const string sqlActualizar = """
                UPDATE firma SET fecha_firma = $3, hash_firma = $4
                WHERE id_parte = $1 AND id_version = $2
                """;
            var filas = await EjecutarAsync(Comando(sqlActualizar, conexion, transaccion,
                firmante, versionContrato, fechaHora, hashFirma), token);
            if (filas == 0)
            {
                await EjecutarAsync(Comando("""
                    INSERT INTO firma (id_version, id_parte, estado_firma, fecha_firma, hash_firma)
                    VALUES ($2, $1, $5, $3, $4)
                    """, conexion, transaccion, firmante, versionContrato, fechaHora, hashFirma, EstadosFirma.Pendiente), token);
            }

            // Evidencia de validación: el OTP queda marcado como usado (nunca se guardó en texto plano).
            const string sqlOtp = """
                UPDATE token_seguridad SET usado = TRUE
                WHERE tipo_token = $2 AND usado = FALSE
                  AND valor LIKE (SELECT encode(sha256(convert_to(token_invitacion, 'UTF8')), 'hex')
                                  FROM usuario_contrato WHERE id_parte = $1) || ':%'
                """;
            await EjecutarAsync(Comando(sqlOtp, conexion, transaccion, firmante, TiposToken.Otp), token);

            var idUsuario = (int?)await EscalarAsync(Comando("SELECT id_usuario FROM usuario_contrato WHERE id_parte = $1",
                conexion, transaccion, firmante), token);
            return await InsertarAuditoriaAsync(conexion, transaccion, idUsuario, TiposAccion.Firma, "usuario_contrato", firmante,
                fechaHora, ip, $"Firma del contrato {idContrato}. Hash de la versión: {hashVersion}. Hash de la firma: {hashFirma}.", token);
        }, ct);

    public Task<bool> VerificarTodosFirmaron(int idContrato, CancellationToken ct) =>
        EjecutarConsulta(async (conexion, token) =>
        {
            var sql = $"""
                SELECT COUNT(*) > 0 AND COUNT(*) = COUNT(f.id_firma)
                FROM usuario_contrato p
                LEFT JOIN firma f ON f.id_parte = p.id_parte AND f.id_version = {VersionActual} AND f.estado_firma = $3
                WHERE p.id_contrato = $1 AND p.rol_parte = $2
                """;
            return (bool)(await EscalarAsync(Comando(sql, conexion, null, idContrato, RolesParte.Firmante, EstadosFirma.Firmado), token))!;
        }, ct);

    public Task<bool> ExistenSolicitudesFirma(int idContrato, CancellationToken ct) =>
        EjecutarConsulta(async (conexion, token) =>
            (bool)(await EscalarAsync(Comando("""
                SELECT EXISTS (SELECT 1 FROM firma f
                               JOIN usuario_contrato p ON p.id_parte = f.id_parte
                               WHERE p.id_contrato = $1)
                """, conexion, null, idContrato), token))!, ct);

    public Task<List<ParteContrato>> ObtenerFirmantesConEstado(int idContrato, CancellationToken ct) => ObtenerFirmantes(idContrato, ct);

    public Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, DateTime fechaHora, int usuario,
        CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            await ActualizarEstadoFirmanteAsync(conexion, transaccion, idContrato, firmante, estado, token);
            return await InsertarAuditoriaAsync(conexion, transaccion, usuario, TiposAccion.ReenvioSolicitudFirma,
                "usuario_contrato", firmante, fechaHora, null, $"Estado del firmante: {estado}.", token);
        }, ct);

    public Task<(Contrato? DatosContrato, bool AccesoUsuario)> ObtenerContrato(int idContrato, int usuario, CancellationToken ct) =>
        EjecutarConsulta<(Contrato? DatosContrato, bool AccesoUsuario)>(async (conexion, token) =>
        {
            var contrato = await PrimeroAsync(Comando(SelectContrato + " WHERE c.id_contrato = $1", conexion, null, idContrato),
                LeerContrato, token);
            if (contrato is null)
                return (null, false);
            if (contrato.EsTitular(usuario))
                return (contrato, true);

            var esParte = (bool)(await EscalarAsync(Comando("""
                SELECT EXISTS (SELECT 1 FROM usuario_contrato WHERE id_contrato = $1 AND id_usuario = $2)
                """, conexion, null, idContrato, usuario), token))!;
            return (contrato, esParte);
        }, ct);

    public Task RegistrarAuditoria(int idContrato, int? usuario, DateTime fechaHora, int version, string accion,
        string? descripcion, CancellationToken ct) =>
        EjecutarModificacion((conexion, transaccion, token) =>
            InsertarAuditoriaAsync(conexion, transaccion, usuario, accion, "contrato", idContrato, fechaHora, null,
                descripcion ?? $"Versión v{version}.", token), ct);

    public Task<List<Contrato>> ObtenerContratosPorUsuario(int usuario, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            ListaAsync(Comando(SelectContrato + """
                 WHERE c.id_usuario_creador = $1
                    OR EXISTS (SELECT 1 FROM usuario_contrato p WHERE p.id_contrato = c.id_contrato AND p.id_usuario = $1)
                 ORDER BY v.fecha_creacion DESC NULLS LAST, c.id_contrato DESC
                """, conexion, null, usuario), LeerContrato, token), ct);

    private static Task<int> ActualizarEstadoFirmanteAsync(NpgsqlConnection conexion, NpgsqlTransaction transaccion,
        int idContrato, int firmante, string estado, CancellationToken ct)
    {
        var sql = $"UPDATE firma SET estado_firma = $3 WHERE id_parte = $2 AND id_version = {VersionActual}";
        return EjecutarAsync(Comando(sql, conexion, transaccion, idContrato, firmante, estado), ct);
    }

    private static Contrato LeerContrato(NpgsqlDataReader lector) => new()
    {
        IdContrato = lector.GetInt32(0),
        IdUsuarioCreador = Entero(lector, 1),
        NombreCreador = Texto(lector, 2),
        EmailCreador = Texto(lector, 3),
        IdEstado = Entero(lector, 4),
        Estado = Texto(lector, 5),
        IdPlantilla = Entero(lector, 6),
        Nombre = Texto(lector, 7),
        DescripcionPropiedad = Texto(lector, 8),
        FechaInicio = Fecha(lector, 9),
        FechaExpiracion = Fecha(lector, 10),
        IdVersion = Entero(lector, 11),
        NumeroVersion = lector.GetInt32(12),
        HashVersion = Texto(lector, 13),
        ContenidoActual = Texto(lector, 14),
        FechaUltimaModificacion = FechaUtc(lector, 15),
        TransicionesDisponibles = [.. lector.GetFieldValue<string[]>(16)]
    };

    private static ParteContrato LeerParte(NpgsqlDataReader lector) => new()
    {
        IdParte = lector.GetInt32(0),
        IdUsuario = Entero(lector, 1),
        IdContrato = lector.GetInt32(2),
        RolParte = Texto(lector, 3),
        CorreoInvitado = Texto(lector, 4),
        EstadoInvitacion = Texto(lector, 5),
        FechaInvitacion = FechaUtc(lector, 6),
        TokenInvitacion = Texto(lector, 7),
        Nombre = Texto(lector, 8)
    };

    private static ParteContrato LeerParteConFirma(NpgsqlDataReader lector)
    {
        var parte = LeerParte(lector);
        parte.EstadoFirma = Texto(lector, 9) ?? EstadosFirma.Pendiente;
        parte.HashFirma = Texto(lector, 11);
        parte.IpFirma = Texto(lector, 12);
        // Fecha del evento: la firma si ya firmó; si no, el último envío de la solicitud.
        parte.FechaHoraEvento = parte.EstadoFirma == EstadosFirma.Firmado ? FechaUtc(lector, 10) : FechaUtc(lector, 13);
        return parte;
    }
}
