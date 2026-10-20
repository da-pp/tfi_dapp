namespace FirmaYa.Models.Entidades;

public sealed class TokenSeguridad
{
    public int IdToken { get; set; }
    public int? IdUsuario { get; set; }
    public string Valor { get; set; } = "";
    public string TipoToken { get; set; } = "";
    public bool Usado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaExpiracion { get; set; }
}

public static class TiposToken
{
    public const string Otp = "OTP";
}

public sealed record OtpRegistrado(string HashCodigo, DateTime FechaEnvio, DateTime FechaExpiracion, bool Usado);

// Tipo propio para que DAL tenga la misma operación por token y por correo (p. ej. IncrementarIntentosFallidos).
public readonly record struct TokenEnlace(string Valor);
