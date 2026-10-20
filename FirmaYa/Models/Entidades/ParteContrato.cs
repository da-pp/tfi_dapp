namespace FirmaYa.Models.Entidades;

/// <summary>
/// Tabla usuario_contrato del DER (partes invitadas a un contrato), más el estado de firma
/// de la versión actual (tabla firma) cuando la parte es Firmante.
/// Es el "datosToken" de CU-04/CU-08 y el "firmante" de CU-07/CU-08/CU-09.
/// </summary>
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

    /// <summary>
    /// Nombre del usuario interno vinculado (usuario.nombre + apellido). El DER no tiene columna para el
    /// nombre de una parte externa, por eso se muestra el correo.
    /// </summary>
    public string? Nombre { get; set; }

    public string NombreVisible => string.IsNullOrWhiteSpace(Nombre) ? CorreoInvitado ?? "" : Nombre;

    // Firma de la versión actual (CU-07, CU-09).
    public string? EstadoFirma { get; set; }
    public DateTime? FechaHoraEvento { get; set; }
    public string? HashFirma { get; set; }
    public string? IpFirma { get; set; }
}

/// <summary>Valores de usuario_contrato.rol_parte (CU-03 paso 6).</summary>
public static class RolesParte
{
    public const string Firmante = "Firmante";
    public const string SoloLectura = "Solo lectura";
    public const string Revisor = "Revisor";

    public static readonly string[] Todos = [Firmante, SoloLectura, Revisor];

    /// <summary>Rol que devuelve obtenerRolUsuario para el creador del contrato.</summary>
    public const string Titular = "Titular";
}

/// <summary>Valores de usuario_contrato.estado_invitacion (CU-03 pasos 13 y 15, CU-08 camino C).</summary>
public static class EstadosInvitacion
{
    public const string Pendiente = "Pendiente";
    public const string InvitacionEnviada = "Invitación enviada";
    public const string Bloqueado = "Bloqueado";
}

/// <summary>Valores de firma.estado_firma (CU-07 paso 6, CU-09 paso 16).</summary>
public static class EstadosFirma
{
    public const string Pendiente = "Pendiente";
    public const string Notificado = "Notificado";
    public const string ReNotificado = "Re-notificado";
    public const string Firmado = "Firmado";
}
