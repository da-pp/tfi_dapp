namespace FirmaYa.Models.Entidades;

/// <summary>Tabla auditoria del DER. fecha_hora se maneja en UTC.</summary>
public sealed class RegistroAuditoria
{
    public int IdRegistro { get; set; }
    public int? IdUsuario { get; set; }
    public string TipoAccion { get; set; } = "";
    public string? EntidadAfectada { get; set; }
    public int? IdEntidadAfectada { get; set; }
    public DateTime FechaHora { get; set; }
    public string? DireccionIp { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>Valores de auditoria.tipo_accion usados por los casos de uso implementados.</summary>
public static class TiposAccion
{
    public const string InicioSesion = "Inicio de sesión";
    public const string BloqueoCuenta = "Bloqueo de cuenta";
    public const string Creacion = "Creación";
    public const string Edicion = "Edición";
    public const string Invitacion = "Invitación";
    public const string AccesoInvitado = "Acceso de parte invitada";
    public const string RenovacionSesion = "Renovación de sesión";
    public const string CambioEstado = "Cambio de estado";
    public const string SolicitudFirma = "Solicitud de firma";
    public const string ReenvioSolicitudFirma = "Reenvío de solicitud de firma";
    public const string OtpIncorrecto = "OTP incorrecto";
    public const string BloqueoEnlace = "Bloqueo de enlace de firma";
    public const string Firma = "Firma";
    public const string Descarga = "Descarga";
}
