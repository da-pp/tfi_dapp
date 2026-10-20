using FirmaYa.Models.Entidades;

namespace FirmaYa.Data;

public partial interface IDAL
{
    Task<Usuario?> ObtenerUsuarioPorCorreo(string correoElectronico, CancellationToken ct);

    Task RegistrarInicioSesion(string correoElectronico, DateTime fechaHora, string? ip, CancellationToken ct);

    Task<int> IncrementarIntentosFallidos(string correoElectronico, CancellationToken ct);

    Task BloquearCuenta(string correoElectronico, int duracionMinutos, CancellationToken ct);

    Task<List<Plantilla>> ObtenerPlantillasActivas(CancellationToken ct);

    Task<Plantilla?> ObtenerPlantillaPorTipo(string tipoPlantilla, CancellationToken ct);

    Task<int> GuardarContrato(Contrato contrato, string estado, int version, string hash, string contenidoVersion1,
        int usuarioCreador, DateTime fechaHora, CancellationToken ct);

    Task<Contrato?> ObtenerContrato(int idContrato, CancellationToken ct);

    Task<bool> GuardarVersion(int idContrato, string contenidoNuevo, int numeroVersion, string hash, int autor,
        DateTime fechaHora, CancellationToken ct);

    Task ActualizarHistorialVersiones(int idContrato, int numeroVersion, int autor, DateTime fechaHora, string hash,
        string? comentarioVersion, CancellationToken ct);

    Task<List<ParteContrato>> ObtenerPartesInvitadas(int idContrato, CancellationToken ct);

    Task<bool> ExisteCorreoInvitado(int idContrato, string correoElectronico, CancellationToken ct);

    Task<int> RegistrarParteInvitada(int idContrato, string correoElectronico, string rol, string token,
        string estadoInvitacion, DateTime fechaHora, CancellationToken ct);

    Task RegistrarEnvioCorreo(Notificacion notificacion, CancellationToken ct);

    Task ActualizarEstadoInvitacion(int idContrato, string correoElectronico, string estado, CancellationToken ct);

    Task<ParteContrato?> ObtenerTokenAcceso(TokenEnlace token, CancellationToken ct);

    Task<Contrato?> ObtenerContrato(TokenEnlace token, CancellationToken ct);

    Task RegistrarAcceso(TokenEnlace token, DateTime fechaHora, string? ip, CancellationToken ct);

    Task RenovarToken(TokenEnlace token, DateTime fechaHora, CancellationToken ct);

    Task<bool> ExisteFirmanteAsignado(int idContrato, CancellationToken ct);

    Task ActualizarEstadoContrato(int idContrato, string nuevoEstado, CancellationToken ct);

    Task RegistrarAuditoria(int idContrato, int usuario, string estadoActual, string nuevoEstado, DateTime fechaHora,
        string? razonCambio, CancellationToken ct);

    Task<List<ParteContrato>> ObtenerPartesConNotificacionesActivas(int idContrato, CancellationToken ct);

    Task EnviarNotificacionesCambioEstado(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct);

    Task<string?> ObtenerRolUsuario(int idContrato, int usuario, CancellationToken ct);

    Task<int> RegistrarComentario(int idContrato, int autor, DateTime fechaHora, string textoComentario, CancellationToken ct);

    Task<List<Comentario>> ObtenerComentarios(int idContrato, CancellationToken ct);

    Task EnviarNotificacionesComentario(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct);

    Task<List<ParteContrato>> ObtenerFirmantes(int idContrato, CancellationToken ct);

    Task RegistrarSolicitudFirma(int idContrato, int firmante, DateTime fechaHora, string canalNotificacion,
        DateOnly? fechaLimiteFirma, int usuario, CancellationToken ct);

    Task RegistrarEnvioNotificacion(Notificacion notificacion, CancellationToken ct);

    Task RegistrarEnvioNotificacion(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct);

    Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, CancellationToken ct);

    Task<ParteContrato?> ObtenerTokenFirma(TokenEnlace token, CancellationToken ct);

    Task<Contrato?> ObtenerContratoPorToken(TokenEnlace token, CancellationToken ct);

    Task RegistrarOTP(TokenEnlace token, string codigoOTP, DateTime fechaEnvio, DateTime fechaExpiracion, CancellationToken ct);

    Task RegistrarEnvioOTP(Notificacion notificacion, CancellationToken ct);

    Task<OtpRegistrado?> ObtenerOTPRegistrado(TokenEnlace token, CancellationToken ct);

    Task<int> IncrementarIntentosFallidos(TokenEnlace token, int maxIntentos, DateTime fechaHora, CancellationToken ct);

    Task RegistrarFirma(int idContrato, int firmante, DateTime fechaHora, string? ip, int versionContrato,
        string hashVersion, string hashFirma, CancellationToken ct);

    Task<bool> VerificarTodosFirmaron(int idContrato, CancellationToken ct);

    Task EnviarNotificacionContratoFirmado(Notificacion notificacion, CancellationToken ct);

    Task<bool> ExistenSolicitudesFirma(int idContrato, CancellationToken ct);

    Task<List<ParteContrato>> ObtenerFirmantesConEstado(int idContrato, CancellationToken ct);

    Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, DateTime fechaHora, int usuario, CancellationToken ct);

    Task<(Contrato? DatosContrato, bool AccesoUsuario)> ObtenerContrato(int idContrato, int usuario, CancellationToken ct);

    Task<VersionContrato?> ObtenerContenidoVersionFinal(int idContrato, CancellationToken ct);

    Task<List<DatosFirma>> ObtenerDatosFirmas(int idContrato, CancellationToken ct);

    Task RegistrarAuditoria(int idContrato, int? usuario, DateTime fechaHora, int version, string accion,
        string? descripcion, CancellationToken ct);

    Task<List<Contrato>> ObtenerContratosPorUsuario(int usuario, CancellationToken ct);
}
