using FirmaYa.Models.Entidades;

namespace FirmaYa.Data.Memory;

/// <summary>Operaciones de :DAL invocadas por :NotificacionBLL y :PDFBLL, con la misma semántica que DAL.Notificacion.cs y DAL.PDF.cs.</summary>
public sealed partial class DALMemoria
{
    public Task RegistrarEnvioCorreo(Notificacion notificacion, CancellationToken ct) => InsertarNotificaciones([notificacion]);

    public Task<List<ParteContrato>> ObtenerPartesConNotificacionesActivas(int idContrato, CancellationToken ct)
    {
        // DEMO sin preferencia_notificacion: todas las partes no bloqueadas tienen el correo activo.
        lock (_bloqueo)
            return Task.FromResult(_partes
                .Where(p => p.IdContrato == idContrato && p.EstadoInvitacion != EstadosInvitacion.Bloqueado)
                .OrderBy(p => p.IdParte).Select(ArmarParte).ToList());
    }

    public Task EnviarNotificacionesCambioEstado(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct) =>
        InsertarNotificaciones(notificaciones);

    public Task EnviarNotificacionesComentario(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct) =>
        InsertarNotificaciones(notificaciones);

    public Task RegistrarEnvioNotificacion(Notificacion notificacion, CancellationToken ct) => InsertarNotificaciones([notificacion]);

    public Task RegistrarEnvioNotificacion(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct) =>
        InsertarNotificaciones(notificaciones);

    public Task RegistrarEnvioOTP(Notificacion notificacion, CancellationToken ct) => InsertarNotificaciones([notificacion]);

    public Task EnviarNotificacionContratoFirmado(Notificacion notificacion, CancellationToken ct) => InsertarNotificaciones([notificacion]);

    // ================= PDFBLL (CU-10) =================

    public Task<VersionContrato?> ObtenerContenidoVersionFinal(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
            return Task.FromResult(VersionActual(idContrato));
    }

    public Task<List<DatosFirma>> ObtenerDatosFirmas(int idContrato, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var hashVersion = VersionActual(idContrato)?.HashSha256 ?? "";
            return Task.FromResult(_partes
                .Where(p => p.IdContrato == idContrato && p.RolParte == RolesParte.Firmante)
                .OrderBy(p => p.IdParte)
                .Select(ArmarParte)
                .Where(p => p.EstadoFirma == EstadosFirma.Firmado && p.FechaHoraEvento is not null)
                .Select(p => new DatosFirma(p.NombreVisible, p.FechaHoraEvento!.Value, p.IpFirma, hashVersion, p.HashFirma))
                .ToList());
        }
    }

    private Task InsertarNotificaciones(IReadOnlyList<Notificacion> notificaciones)
    {
        lock (_bloqueo)
        {
            foreach (var notificacion in notificaciones)
            {
                notificacion.IdNotificacion = SiguienteId("notificacion");
                _notificaciones.Add(notificacion);
            }
        }
        return Task.CompletedTask;
    }
}
