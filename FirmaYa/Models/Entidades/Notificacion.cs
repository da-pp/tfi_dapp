namespace FirmaYa.Models.Entidades;

/// <summary>
/// Tabla notificacion del DER. NotificacionBLL envía el correo y luego :DAL registra el envío con este objeto
/// (registrarEnvioCorreo, registrarEnvioNotificacion, registrarEnvioOTP, enviarNotificaciones…).
/// </summary>
public sealed class Notificacion
{
    public int IdNotificacion { get; set; }
    public int? IdUsuario { get; set; }
    public int? IdContrato { get; set; }
    public string Canal { get; set; } = CanalesNotificacion.CorreoElectronico;
    public string? CorreoDestino { get; set; }
    public string Tipo { get; set; } = "";
    public string? Mensaje { get; set; }
    public string EstadoEnvio { get; set; } = EstadosEnvio.Enviado;
    public DateTime FechaEnvio { get; set; }
}

public static class CanalesNotificacion
{
    /// <summary>CU-07 paso 8: única opción disponible.</summary>
    public const string CorreoElectronico = "Correo electrónico";
}

public static class EstadosEnvio
{
    public const string Enviado = "Enviado";
    public const string Error = "Error";
}

/// <summary>Valores de notificacion.tipo.</summary>
public static class TiposNotificacion
{
    public const string Invitacion = "Invitación";
    public const string CambioEstado = "Cambio de estado";
    public const string NuevoComentario = "Nuevo comentario";
    public const string SolicitudFirma = "Solicitud de firma";
    public const string CodigoOtp = "Código OTP";
    public const string ContratoFirmado = "Contrato firmado";
}
