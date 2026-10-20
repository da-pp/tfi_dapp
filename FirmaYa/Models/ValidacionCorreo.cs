using System.Text.RegularExpressions;

namespace FirmaYa.Models;

/// <summary>Formato de correo de los CU-03, CU-15 y CU-19: contiene "@" y dominio, máximo 254 caracteres.</summary>
public static partial class ValidacionCorreo
{
    public const int LongitudMaxima = 254;

    public static bool EsValido(string? correo) =>
        !string.IsNullOrWhiteSpace(correo)
        && correo.Trim().Length <= LongitudMaxima
        && Formato().IsMatch(correo.Trim());

    // La misma expresión se usa en wwwroot/js/site.js.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Formato();
}
