namespace FirmaYa.Models.Entidades;

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

    public string? NombreRol { get; set; }

    // No es una columna de usuario: es la fecha_hora del último registro "Bloqueo de cuenta" en auditoria.
    // Permite saber cuándo vencen los 15 minutos de bloqueo sin agregar columnas al DER.
    public DateTime? FechaUltimoBloqueo { get; set; }

    public Usuario Copiar() => (Usuario)MemberwiseClone();
}

public static class EstadosUsuario
{
    public const string Activo = "Activo";
    public const string Inactivo = "Inactivo";
    public const string Bloqueado = "Bloqueado";
}

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Abogado = "Abogado";
    public const string AgenteInmobiliario = "Agente Inmobiliario";

    public static bool EsUsuarioInterno(string? rol) => rol is Administrador or Abogado or AgenteInmobiliario;
}
