namespace FirmaYa.Models.Entidades;

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
    public const string CorreoElectronico = "Correo electrónico";
}

public static class EstadosEnvio
{
    public const string Enviado = "Enviado";
    public const string Error = "Error";
}

public static class TiposNotificacion
{
    public const string Invitacion = "Invitación";
    public const string CambioEstado = "Cambio de estado";
    public const string NuevoComentario = "Nuevo comentario";
    public const string SolicitudFirma = "Solicitud de firma";
    public const string CodigoOtp = "Código OTP";
    public const string ContratoFirmado = "Contrato firmado";
}
