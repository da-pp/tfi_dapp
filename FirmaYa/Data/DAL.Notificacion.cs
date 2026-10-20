using FirmaYa.Models.Entidades;

namespace FirmaYa.Data;

/// <summary>Operaciones de :DAL invocadas por :NotificacionBLL (CU-03, CU-05 a CU-09).</summary>
public sealed partial class DAL
{
    public Task RegistrarEnvioCorreo(Notificacion notificacion, CancellationToken ct) => InsertarNotificaciones([notificacion], ct);

    /// <summary>
    /// Partes del contrato que reciben avisos: las no bloqueadas, salvo los usuarios internos que desactivaron
    /// el canal de correo en preferencia_notificacion. Las partes externas no tienen preferencias.
    /// </summary>
    public Task<List<ParteContrato>> ObtenerPartesConNotificacionesActivas(int idContrato, CancellationToken ct) =>
        EjecutarConsulta((conexion, token) =>
            ListaAsync(Comando(SelectParte + """
                 WHERE p.id_contrato = $1
                   AND COALESCE(p.estado_invitacion, '') <> $2
                   AND NOT EXISTS (SELECT 1 FROM preferencia_notificacion pn
                                   WHERE pn.id_usuario = p.id_usuario AND pn.canal = $3 AND pn.activa = FALSE)
                 ORDER BY p.id_parte
                """, conexion, null, idContrato, EstadosInvitacion.Bloqueado, CanalesNotificacion.CorreoElectronico),
                LeerParte, token), ct);

    public Task EnviarNotificacionesCambioEstado(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct) =>
        InsertarNotificaciones(notificaciones, ct);

    public Task EnviarNotificacionesComentario(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct) =>
        InsertarNotificaciones(notificaciones, ct);

    public Task RegistrarEnvioNotificacion(Notificacion notificacion, CancellationToken ct) => InsertarNotificaciones([notificacion], ct);

    public Task RegistrarEnvioNotificacion(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct) =>
        InsertarNotificaciones(notificaciones, ct);

    public Task RegistrarEnvioOTP(Notificacion notificacion, CancellationToken ct) => InsertarNotificaciones([notificacion], ct);

    public Task EnviarNotificacionContratoFirmado(Notificacion notificacion, CancellationToken ct) =>
        InsertarNotificaciones([notificacion], ct);

    private Task InsertarNotificaciones(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct) =>
        EjecutarModificacion(async (conexion, transaccion, token) =>
        {
            const string sql = """
                INSERT INTO notificacion (id_usuario, id_contrato, canal, correo_destino, tipo, mensaje, estado_envio, fecha_envio)
                VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
                RETURNING id_notificacion
                """;
            foreach (var n in notificaciones)
            {
                n.IdNotificacion = (int)(await EscalarAsync(Comando(sql, conexion, transaccion,
                    n.IdUsuario, n.IdContrato, n.Canal, n.CorreoDestino, n.Tipo, n.Mensaje, n.EstadoEnvio, n.FechaEnvio), token))!;
            }
            return notificaciones.Count;
        }, ct);
}
