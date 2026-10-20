using System.Security.Cryptography;
using System.Text;

namespace FirmaYa.Models;

public static class HashSha256
{
    public static string Calcular(string texto) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();

    // Hash con el que se guarda y se compara un código OTP. El token del enlace actúa como sal.
    public static string CodigoOtp(string tokenEnlace, string codigo) => Calcular($"{tokenEnlace}:{codigo}");
}
