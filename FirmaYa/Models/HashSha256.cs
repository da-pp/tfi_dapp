using System.Security.Cryptography;
using System.Text;

namespace FirmaYa.Models;

/// <summary>SHA-256 en hexadecimal minúscula (64 caracteres), igual que encode(sha256(...), 'hex') de PostgreSQL.</summary>
public static class HashSha256
{
    public static string Calcular(string texto) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();

    /// <summary>Hash con el que se guarda y se compara un código OTP (CU-08). El token del enlace actúa como sal.</summary>
    public static string CodigoOtp(string tokenEnlace, string codigo) => Calcular($"{tokenEnlace}:{codigo}");
}
