namespace FirmaYa.Models.Entidades;

/// <summary>
/// Tabla token_seguridad del DER. En el CU-08 guarda el código OTP:
/// valor = SHA-256(token del enlace) + ":" + SHA-256(token del enlace + ":" + código). El código nunca se guarda en texto plano.
/// </summary>
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

/// <summary>"otpRegistrado, fechaEnvio" que :DAL devuelve en obtenerOTPRegistrado (CU-08).</summary>
public sealed record OtpRegistrado(string HashCodigo, DateTime FechaEnvio, DateTime FechaExpiracion, bool Usado);

/// <summary>
/// Token de un enlace tokenizado (usuario_contrato.token_invitacion). Es un tipo propio para que :DAL pueda tener
/// las mismas operaciones por token y por correo con el nombre del diagrama (p. ej. incrementarIntentosFallidos).
/// </summary>
public readonly record struct TokenEnlace(string Valor);
