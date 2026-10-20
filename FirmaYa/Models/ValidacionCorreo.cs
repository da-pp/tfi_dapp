using System.Text.RegularExpressions;

namespace FirmaYa.Models;

public static partial class ValidacionCorreo
{
    public const int LongitudMaxima = 254;

    public static bool EsValido(string? correo) =>
        !string.IsNullOrWhiteSpace(correo)
        && correo.Trim().Length <= LongitudMaxima
        && Formato().IsMatch(correo.Trim());

    // La misma expresión se usa en login.js e invitar.js.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Formato();
}
