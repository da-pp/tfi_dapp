using FirmaYa.Models.Entidades;

namespace FirmaYa.Data;

/// <summary>
/// Contrato del participante ":DAL" de los diagramas de secuencia.
/// Implementaciones: DAL (PostgreSQL en Supabase) y DALMemoria (modo DEMO).
/// Cada método lleva el nombre del mensaje que el diagrama envía a :DAL. Fecha y hora viajan juntas en un
/// DateTime UTC porque el DER las guarda en una sola columna TIMESTAMP.
/// </summary>
public partial interface IDAL
{
    // ---- CU-19 Iniciar Sesión (AccesoBLL) ----

    /// <summary>obtenerUsuarioPorCorreo(correoElectronico) → datosUsuario</summary>
    Task<Usuario?> ObtenerUsuarioPorCorreo(string correoElectronico, CancellationToken ct);

    /// <summary>registrarInicioSesion(correoElectronico, fecha, hora, ip)</summary>
    Task RegistrarInicioSesion(string correoElectronico, DateTime fechaHora, string? ip, CancellationToken ct);

    /// <summary>incrementarIntentosFallidos(correoElectronico) → intentosFallidos</summary>
    Task<int> IncrementarIntentosFallidos(string correoElectronico, CancellationToken ct);

    /// <summary>bloquearCuenta(correoElectronico, duracionMinutos=15)</summary>
    Task BloquearCuenta(string correoElectronico, int duracionMinutos, CancellationToken ct);

    // ---- CU-01 Crear contrato desde plantilla (PlantillaBLL, ContratoBLL) ----

    /// <summary>obtenerPlantillasActivas() → listaPlantillas</summary>
    Task<List<Plantilla>> ObtenerPlantillasActivas(CancellationToken ct);

    /// <summary>obtenerPlantillaPorTipo(tipoPlantilla) → datosPlantilla</summary>
    Task<Plantilla?> ObtenerPlantillaPorTipo(string tipoPlantilla, CancellationToken ct);

    /// <summary>
    /// guardarContrato(nombreContrato, partesInvolucradas, fechaInicio, fechaExpiracion, descripcionPropiedad,
    /// estado="Borrador", version=1, hash, usuarioCreador) → idContrato.
    /// Los datos del contrato viajan en <paramref name="contrato"/>; las partes involucradas quedan dentro del
    /// contenido de la versión 1 (el DER no tiene columna para ellas).
    /// </summary>
    Task<int> GuardarContrato(Contrato contrato, string estado, int version, string hash, string contenidoVersion1,
        int usuarioCreador, DateTime fechaHora, CancellationToken ct);

    // ---- CU-02 Editar contrato en línea (ContratoBLL) ----

    /// <summary>obtenerContrato(idContrato) → datosContrato</summary>
    Task<Contrato?> ObtenerContrato(int idContrato, CancellationToken ct);

    /// <summary>guardarVersion(idContrato, contenidoNuevo, numeroVersion, hash, autor, fecha, hora). False si el número ya existe.</summary>
    Task<bool> GuardarVersion(int idContrato, string contenidoNuevo, int numeroVersion, string hash, int autor,
        DateTime fechaHora, CancellationToken ct);

    /// <summary>
    /// actualizarHistorialVersiones(idContrato, numeroVersion, autor, fecha, hora, hash). Registra la versión en auditoría
    /// junto con el comentario de versión (version_contrato no tiene columna de comentario).
    /// </summary>
    Task ActualizarHistorialVersiones(int idContrato, int numeroVersion, int autor, DateTime fechaHora, string hash,
        string? comentarioVersion, CancellationToken ct);

    // ---- CU-03 Invitar a las partes al contrato (ContratoBLL, NotificacionBLL) ----

    /// <summary>obtenerPartesInvitadas(idContrato) → listaPartesInvitadas</summary>
    Task<List<ParteContrato>> ObtenerPartesInvitadas(int idContrato, CancellationToken ct);

    /// <summary>existeCorreoInvitado(idContrato, correoElectronico) → resultado</summary>
    Task<bool> ExisteCorreoInvitado(int idContrato, string correoElectronico, CancellationToken ct);

    /// <summary>registrarParteInvitada(idContrato, correoElectronico, rol, token, estadoInvitacion="Pendiente") → idParte</summary>
    Task<int> RegistrarParteInvitada(int idContrato, string correoElectronico, string rol, string token,
        string estadoInvitacion, DateTime fechaHora, CancellationToken ct);

    /// <summary>registrarEnvioCorreo(correoElectronico, idContrato, token) — los datos viajan en <paramref name="notificacion"/>.</summary>
    Task RegistrarEnvioCorreo(Notificacion notificacion, CancellationToken ct);

    /// <summary>actualizarEstadoInvitacion(idContrato, correoElectronico, estado)</summary>
    Task ActualizarEstadoInvitacion(int idContrato, string correoElectronico, string estado, CancellationToken ct);

    // ---- CU-04 Ver contrato como parte invitada (AccesoBLL, ContratoBLL) ----

    /// <summary>obtenerTokenAcceso(token) → datosToken</summary>
    Task<ParteContrato?> ObtenerTokenAcceso(TokenEnlace token, CancellationToken ct);

    /// <summary>obtenerContrato(token) → datosContrato</summary>
    Task<Contrato?> ObtenerContrato(TokenEnlace token, CancellationToken ct);

    /// <summary>registrarAcceso(token, fecha, hora, ip)</summary>
    Task RegistrarAcceso(TokenEnlace token, DateTime fechaHora, string? ip, CancellationToken ct);

    /// <summary>renovarToken(token)</summary>
    Task RenovarToken(TokenEnlace token, DateTime fechaHora, CancellationToken ct);

    // ---- CU-05 Cambiar estado del contrato (ContratoBLL, NotificacionBLL) ----

    /// <summary>existeFirmanteAsignado(idContrato) → resultado</summary>
    Task<bool> ExisteFirmanteAsignado(int idContrato, CancellationToken ct);

    /// <summary>actualizarEstadoContrato(idContrato, nuevoEstado)</summary>
    Task ActualizarEstadoContrato(int idContrato, string nuevoEstado, CancellationToken ct);

    /// <summary>registrarAuditoria(idContrato, usuario, estadoActual, nuevoEstado, fecha, hora) + razón del cambio.</summary>
    Task RegistrarAuditoria(int idContrato, int usuario, string estadoActual, string nuevoEstado, DateTime fechaHora,
        string? razonCambio, CancellationToken ct);

    /// <summary>obtenerPartesConNotificacionesActivas(idContrato) → listaPartes</summary>
    Task<List<ParteContrato>> ObtenerPartesConNotificacionesActivas(int idContrato, CancellationToken ct);

    /// <summary>enviarNotificacionesCambioEstado(listaPartes, nuevoEstado): registra una notificación por parte.</summary>
    Task EnviarNotificacionesCambioEstado(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct);

    // ---- CU-06 Añadir comentarios y observaciones (ContratoBLL, NotificacionBLL) ----

    /// <summary>obtenerRolUsuario(idContrato, usuario) → rolUsuario ("Titular", rol_parte o null sin acceso)</summary>
    Task<string?> ObtenerRolUsuario(int idContrato, int usuario, CancellationToken ct);

    /// <summary>registrarComentario(idContrato, autor, fecha, hora, textoComentario, fragmentoReferenciado) → idComentario</summary>
    Task<int> RegistrarComentario(int idContrato, int autor, DateTime fechaHora, string textoComentario, CancellationToken ct);

    /// <summary>No figura en el diagrama del CU-06: lista los comentarios del panel lateral (paso 2 y 15).</summary>
    Task<List<Comentario>> ObtenerComentarios(int idContrato, CancellationToken ct);

    /// <summary>enviarNotificacionesComentario(listaPartes, idContrato, autor, textoComentario)</summary>
    Task EnviarNotificacionesComentario(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct);

    // ---- CU-07 Solicitar firma de las partes (ContratoBLL, NotificacionBLL) ----

    /// <summary>obtenerFirmantes(idContrato) → listaFirmantes(nombre, correoElectronico, estadoFirma)</summary>
    Task<List<ParteContrato>> ObtenerFirmantes(int idContrato, CancellationToken ct);

    /// <summary>registrarSolicitudFirma(idContrato, firmante, fecha, hora, canalNotificacion, fechaLimiteFirma, enlaceFirma)</summary>
    Task RegistrarSolicitudFirma(int idContrato, int firmante, DateTime fechaHora, string canalNotificacion,
        DateOnly? fechaLimiteFirma, int usuario, CancellationToken ct);

    /// <summary>registrarEnvioNotificacion(firmante, idContrato[, enlaceFirma])</summary>
    Task RegistrarEnvioNotificacion(Notificacion notificacion, CancellationToken ct);

    /// <summary>registrarEnvioNotificacion(firmantesConError, idContrato) — reintento del CU-07.</summary>
    Task RegistrarEnvioNotificacion(IReadOnlyList<Notificacion> notificaciones, CancellationToken ct);

    /// <summary>actualizarEstadoFirmante(idContrato, firmante, estado)</summary>
    Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, CancellationToken ct);

    // ---- CU-08 Firmar contrato vía OTP (AccesoBLL, ContratoBLL, NotificacionBLL) ----

    /// <summary>obtenerTokenFirma(token) → datosToken</summary>
    Task<ParteContrato?> ObtenerTokenFirma(TokenEnlace token, CancellationToken ct);

    /// <summary>obtenerContratoPorToken(token) → datosContrato</summary>
    Task<Contrato?> ObtenerContratoPorToken(TokenEnlace token, CancellationToken ct);

    /// <summary>registrarOTP(token, codigoOTP, fechaEnvio). Guarda solo el hash del código.</summary>
    Task RegistrarOTP(TokenEnlace token, string codigoOTP, DateTime fechaEnvio, DateTime fechaExpiracion, CancellationToken ct);

    /// <summary>registrarEnvioOTP(correoFirmante, codigoOTP) — se registra el envío sin el código.</summary>
    Task RegistrarEnvioOTP(Notificacion notificacion, CancellationToken ct);

    /// <summary>obtenerOTPRegistrado(token) → otpRegistrado, fechaEnvio</summary>
    Task<OtpRegistrado?> ObtenerOTPRegistrado(TokenEnlace token, CancellationToken ct);

    /// <summary>incrementarIntentosFallidos(token) → intentosRestantes. Con 0 restantes bloquea el enlace.</summary>
    Task<int> IncrementarIntentosFallidos(TokenEnlace token, int maxIntentos, DateTime fechaHora, CancellationToken ct);

    /// <summary>registrarFirma(idContrato, firmante, fecha, hora, ip, versionContrato, hashVersion)</summary>
    Task RegistrarFirma(int idContrato, int firmante, DateTime fechaHora, string? ip, int versionContrato,
        string hashVersion, string hashFirma, CancellationToken ct);

    /// <summary>verificarTodosFirmaron(idContrato) → resultado</summary>
    Task<bool> VerificarTodosFirmaron(int idContrato, CancellationToken ct);

    /// <summary>enviarNotificacionContratoFirmado(idContrato, parteResponsable)</summary>
    Task EnviarNotificacionContratoFirmado(Notificacion notificacion, CancellationToken ct);

    // ---- CU-09 Consultar estado de firmas pendientes (ContratoBLL, NotificacionBLL) ----

    /// <summary>existenSolicitudesFirma(idContrato) → resultado</summary>
    Task<bool> ExistenSolicitudesFirma(int idContrato, CancellationToken ct);

    /// <summary>obtenerFirmantesConEstado(idContrato) → listaFirmantes</summary>
    Task<List<ParteContrato>> ObtenerFirmantesConEstado(int idContrato, CancellationToken ct);

    /// <summary>actualizarEstadoFirmante(idContrato, firmante, estado="Re-notificado", fecha, hora)</summary>
    Task ActualizarEstadoFirmante(int idContrato, int firmante, string estado, DateTime fechaHora, int usuario, CancellationToken ct);

    // ---- CU-10 Descargar contrato firmado en PDF (ContratoBLL, PDFBLL) ----

    /// <summary>obtenerContrato(idContrato, usuario) → datosContrato, accesoUsuario</summary>
    Task<(Contrato? DatosContrato, bool AccesoUsuario)> ObtenerContrato(int idContrato, int usuario, CancellationToken ct);

    /// <summary>obtenerContenidoVersionFinal(idContrato) → contenidoVersionFinal</summary>
    Task<VersionContrato?> ObtenerContenidoVersionFinal(int idContrato, CancellationToken ct);

    /// <summary>obtenerDatosFirmas(idContrato) → datosFirmas(nombreFirmante, fechaHoraFirma, ipFirmante, hashVersion)</summary>
    Task<List<DatosFirma>> ObtenerDatosFirmas(int idContrato, CancellationToken ct);

    /// <summary>registrarAuditoria(idContrato, usuario, fecha, hora, version, accion="Descarga")</summary>
    Task RegistrarAuditoria(int idContrato, int? usuario, DateTime fechaHora, int version, string accion,
        string? descripcion, CancellationToken ct);

    // ---- Panel principal (mapa de navegación: "Dashboard, lista contratos"). CU-22 sin diagrama en el documento. ----

    /// <summary>Contratos creados por el usuario o en los que participa como parte interna.</summary>
    Task<List<Contrato>> ObtenerContratosPorUsuario(int usuario, CancellationToken ct);
}
