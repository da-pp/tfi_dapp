using FirmaYa.Models;
using FirmaYa.Models.Entidades;
using Npgsql;

namespace FirmaYa.Data;

public sealed partial class DAL
{
    public Task<Usuario?> ObtenerUsuarioPorCorreo(string correoElectronico, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
        {
            const string sql = """
                SELECT u.id_usuario, u.id_rol, r.nombre, u.nombre, u.apellido, u.email,
                       u.contrasena_hash, u.estado, COALESCE(u.intentos_fallidos, 0),
                       (SELECT MAX(a.fecha_hora)
                        FROM auditoria a
                        WHERE a.id_usuario = u.id_usuario AND a.tipo_accion = $2)
                FROM usuario u
                LEFT JOIN rol r ON r.id_rol = u.id_rol
                WHERE lower(u.email) = lower($1)
                """;
            return PrimeroAsync(Comando(sql, conexion, null, correoElectronico, TiposAccion.BloqueoCuenta),
                lector => new Usuario
                {
                    IdUsuario = lector.GetInt32(0),
                    IdRol = Entero(lector, 1),
                    NombreRol = Texto(lector, 2),
                    Nombre = Texto(lector, 3),
                    Apellido = Texto(lector, 4),
                    Email = Texto(lector, 5),
                    ContrasenaHash = Texto(lector, 6),
                    Estado = Texto(lector, 7),
                    IntentosFallidos = lector.GetInt32(8),
                    FechaUltimoBloqueo = FechaUtc(lector, 9)
                }, token);
        }, ct);

    public Task RegistrarInicioSesion(string correoElectronico, DateTime fechaHora, string? ip, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            const string sql = """
                UPDATE usuario
                SET intentos_fallidos = 0, estado = $2
                WHERE lower(email) = lower($1) AND estado IN ($2, $3)
                RETURNING id_usuario
                """;
            var idUsuario = (int?)await EscalarAsync(Comando(sql, conexion, transaccion,
                correoElectronico, EstadosUsuario.Activo, EstadosUsuario.Bloqueado), token);

            return await InsertarAuditoriaAsync(conexion, transaccion, idUsuario, TiposAccion.InicioSesion,
                "usuario", idUsuario, fechaHora, ip, "Inicio de sesión exitoso.", token);
        }, ct);

    public Task<int> IncrementarIntentosFallidos(string correoElectronico, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            const string sql = """
                UPDATE usuario
                SET intentos_fallidos = CASE WHEN estado = $2 THEN 1 ELSE COALESCE(intentos_fallidos, 0) + 1 END,
                    estado = $3
                WHERE lower(email) = lower($1) AND estado IN ($2, $3)
                RETURNING intentos_fallidos
                """;
            return await EscalarAsync(Comando(sql, conexion, transaccion,
                correoElectronico, EstadosUsuario.Bloqueado, EstadosUsuario.Activo), token) is int intentos ? intentos : 0;
        }, ct);

    public Task BloquearCuenta(string correoElectronico, int duracionMinutos, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            const string sql = "UPDATE usuario SET estado = $2 WHERE lower(email) = lower($1) RETURNING id_usuario";
            var idUsuario = (int?)await EscalarAsync(Comando(sql, conexion, transaccion,
                correoElectronico, EstadosUsuario.Bloqueado), token);

            // La fecha la toma la base: es la referencia para calcular el fin del bloqueo.
            const string sqlAuditoria = """
                INSERT INTO auditoria (id_usuario, tipo_accion, entidad_afectada, id_entidad_afectada, fecha_hora, descripcion)
                VALUES ($1, $2, 'usuario', $1, now() AT TIME ZONE 'UTC', $3)
                """;
            return await EjecutarAsync(Comando(sqlAuditoria, conexion, transaccion, idUsuario, TiposAccion.BloqueoCuenta,
                $"Cuenta bloqueada por {duracionMinutos} minutos por intentos fallidos."), token);
        }, ct);

    public Task<ParteContrato?> ObtenerTokenAcceso(TokenEnlace token, CancellationToken ct) =>
        EjecutarConsulta((conexion, cancelacion) =>
            PrimeroAsync(Comando(SelectParteConFirma + " WHERE p.token_invitacion = $1", conexion, null, token.Valor),
                LeerParteConFirma, cancelacion), ct);

    public Task RegistrarAcceso(TokenEnlace token, DateTime fechaHora, string? ip, CancellationToken ct) =>
        InsertarAuditoriaPorTokenAsync(token, TiposAccion.AccesoInvitado, fechaHora, ip, "Acceso al contrato mediante enlace tokenizado.", ct);

    public Task RenovarToken(TokenEnlace token, DateTime fechaHora, CancellationToken ct) =>
        InsertarAuditoriaPorTokenAsync(token, TiposAccion.RenovacionSesion, fechaHora, null, "La parte extendió la sesión.", ct);

    public Task<ParteContrato?> ObtenerTokenFirma(TokenEnlace token, CancellationToken ct) => ObtenerTokenAcceso(token, ct);

    public Task RegistrarOTP(TokenEnlace token, string codigoOTP, DateTime fechaEnvio, DateTime fechaExpiracion, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, cancelacion) =>
        {
            var prefijo = PrefijoOtp(token);

            // Un código nuevo invalida los anteriores del mismo enlace.
            const string sqlInvalidar = """
                UPDATE token_seguridad SET usado = TRUE
                WHERE tipo_token = $1 AND usado = FALSE AND valor LIKE $2 || ':%'
                """;
            await EjecutarAsync(Comando(sqlInvalidar, conexion, transaccion, TiposToken.Otp, prefijo), cancelacion);

            const string sqlInsertar = """
                INSERT INTO token_seguridad (id_usuario, valor, tipo_token, usado, fecha_creacion, fecha_expiracion)
                SELECT p.id_usuario, $2, $3, FALSE, $4, $5
                FROM usuario_contrato p
                WHERE p.token_invitacion = $1
                """;
            return await EjecutarAsync(Comando(sqlInsertar, conexion, transaccion,
                token.Valor, $"{prefijo}:{HashOtp(token, codigoOTP)}", TiposToken.Otp, fechaEnvio, fechaExpiracion), cancelacion);
        }, ct);

    public Task<OtpRegistrado?> ObtenerOTPRegistrado(TokenEnlace token, CancellationToken ct) =>
        EjecutarConsulta((conexion, cancelacion) =>
        {
            const string sql = """
                SELECT valor, fecha_creacion, fecha_expiracion, usado
                FROM token_seguridad
                WHERE tipo_token = $1 AND valor LIKE $2 || ':%'
                ORDER BY fecha_creacion DESC, id_token DESC
                LIMIT 1
                """;
            return PrimeroAsync(Comando(sql, conexion, null, TiposToken.Otp, PrefijoOtp(token)),
                lector => new OtpRegistrado(
                    lector.GetString(0).Split(':')[1],
                    FechaUtc(lector, 1)!.Value,
                    FechaUtc(lector, 2)!.Value,
                    lector.GetBoolean(3)), cancelacion);
        }, ct);

    public Task<int> IncrementarIntentosFallidos(TokenEnlace token, int maxIntentos, DateTime fechaHora, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, cancelacion) =>
        {
            var parte = await PrimeroAsync(Comando(SelectParte + " WHERE p.token_invitacion = $1", conexion, transaccion, token.Valor),
                LeerParte, cancelacion);
            if (parte is null)
                return 0;

            await InsertarAuditoriaAsync(conexion, transaccion, parte.IdUsuario, TiposAccion.OtpIncorrecto,
                "usuario_contrato", parte.IdParte, fechaHora, null, "Código OTP incorrecto.", cancelacion);

            const string sqlContar = """
                SELECT COUNT(*)::int FROM auditoria
                WHERE tipo_accion = $1 AND entidad_afectada = 'usuario_contrato' AND id_entidad_afectada = $2
                """;
            var fallidos = (int)(await EscalarAsync(Comando(sqlContar, conexion, transaccion,
                TiposAccion.OtpIncorrecto, parte.IdParte), cancelacion))!;
            var restantes = Math.Max(maxIntentos - fallidos, 0);

            if (restantes == 0)
            {
                await EjecutarAsync(Comando("UPDATE usuario_contrato SET estado_invitacion = $2 WHERE id_parte = $1",
                    conexion, transaccion, parte.IdParte, EstadosInvitacion.Bloqueado), cancelacion);
                await InsertarAuditoriaAsync(conexion, transaccion, parte.IdUsuario, TiposAccion.BloqueoEnlace,
                    "usuario_contrato", parte.IdParte, fechaHora, null,
                    $"Enlace de firma bloqueado tras {maxIntentos} intentos fallidos.", cancelacion);
            }
            return restantes;
        }, ct);

    private Task InsertarAuditoriaPorTokenAsync(TokenEnlace token, string tipoAccion, DateTime fechaHora, string? ip,
        string descripcion, CancellationToken ct) =>
        EjecutarModificacion((conexion, transaccion, cancelacion) =>
        {
            const string sql = """
                INSERT INTO auditoria (id_usuario, tipo_accion, entidad_afectada, id_entidad_afectada,
                                       fecha_hora, direccion_ip, descripcion)
                SELECT p.id_usuario, $2, 'usuario_contrato', p.id_parte, $3, $4, $5
                FROM usuario_contrato p
                WHERE p.token_invitacion = $1
                """;
            return EjecutarAsync(Comando(sql, conexion, transaccion, token.Valor, tipoAccion, fechaHora, ip, descripcion), cancelacion);
        }, ct);

    // token_seguridad.valor = SHA-256(token) + ":" + SHA-256(token + ":" + código). El prefijo permite buscar por enlace
    // sin guardar el token ni el código en texto plano.
    private static string PrefijoOtp(TokenEnlace token) => HashSha256.Calcular(token.Valor);

    private static string HashOtp(TokenEnlace token, string codigo) => HashSha256.CodigoOtp(token.Valor, codigo);
}
