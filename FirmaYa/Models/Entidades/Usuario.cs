namespace FirmaYa.Models.Entidades;

/// <summary>
/// Tabla usuario del DER. Es el "datosUsuario" que DAL.obtenerUsuarioPorCorreo devuelve a AccesoBLL en el CU-19.
/// </summary>
public sealed class Usuario
{
    public int IdUsuario { get; set; }
    public int? IdRol { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? ContrasenaHash { get; set; }
    public string? Estado { get; set; }
    public int IntentosFallidos { get; set; }

    /// <summary>rol.nombre (JOIN con la tabla rol).</summary>
    public string? NombreRol { get; set; }

    /// <summary>
    /// No es una columna de usuario: es la fecha_hora del último registro "Bloqueo de cuenta" en auditoria.
    /// Permite saber cuándo vencen los 15 minutos de bloqueo sin agregar columnas al DER.
    /// </summary>
    public DateTime? FechaUltimoBloqueo { get; set; }

    public Usuario Copiar() => (Usuario)MemberwiseClone();
}

/// <summary>Valores de usuario.estado.</summary>
public static class EstadosUsuario
{
    public const string Activo = "Activo";
    public const string Inactivo = "Inactivo";
    public const string Bloqueado = "Bloqueado";
}

/// <summary>Valores de rol.nombre para usuarios internos (CU-15 paso 8, CU-19 actores).</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Abogado = "Abogado";
    public const string AgenteInmobiliario = "Agente Inmobiliario";

    public static bool EsUsuarioInterno(string? rol) => rol is Administrador or Abogado or AgenteInmobiliario;
}
