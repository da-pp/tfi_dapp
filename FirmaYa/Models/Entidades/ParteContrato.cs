namespace FirmaYa.Models.Entidades;

public sealed class ParteContrato
{
    public int IdParte { get; set; }
    public int? IdUsuario { get; set; }
    public int IdContrato { get; set; }
    public string? RolParte { get; set; }
    public string? CorreoInvitado { get; set; }
    public string? EstadoInvitacion { get; set; }
    public DateTime? FechaInvitacion { get; set; }
    public string? TokenInvitacion { get; set; }

    // Nombre del usuario interno vinculado (usuario.nombre + apellido). El DER no tiene columna para el
    // nombre de una parte externa, por eso se muestra el correo.
    public string? Nombre { get; set; }

    public string NombreVisible => string.IsNullOrWhiteSpace(Nombre) ? CorreoInvitado ?? "" : Nombre;

    public string? EstadoFirma { get; set; }
    public DateTime? FechaHoraEvento { get; set; }
    public string? HashFirma { get; set; }
    public string? IpFirma { get; set; }
}

public static class RolesParte
{
    public const string Firmante = "Firmante";
    public const string SoloLectura = "Solo lectura";
    public const string Revisor = "Revisor";

    public static readonly string[] Todos = [Firmante, SoloLectura, Revisor];

    public const string Titular = "Titular";
}

public static class EstadosInvitacion
{
    public const string Pendiente = "Pendiente";
    public const string InvitacionEnviada = "Invitación enviada";
    public const string Bloqueado = "Bloqueado";
}

public static class EstadosFirma
{
    public const string Pendiente = "Pendiente";
    public const string Notificado = "Notificado";
    public const string ReNotificado = "Re-notificado";
    public const string Firmado = "Firmado";
}
