namespace FirmaYa.Models.ViewModels;

public sealed class LoginViewModel
{
    public const int LongitudMaximaCorreo = ValidacionCorreo.LongitudMaxima;
    public const int LongitudMinimaContrasena = 8;

    public string? Correo { get; set; }
    public string? Contrasena { get; set; }

    public Dictionary<string, string> Validar()
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(Correo))
            errores[nameof(Correo)] = "El campo Email es obligatorio";
        else if (!ValidacionCorreo.EsValido(Correo))
            errores[nameof(Correo)] = "Ingrese un correo electrónico válido";

        if (string.IsNullOrEmpty(Contrasena))
            errores[nameof(Contrasena)] = "El campo Contraseña es obligatorio";
        else if (Contrasena.Length < LongitudMinimaContrasena)
            errores[nameof(Contrasena)] = $"La contraseña debe tener al menos {LongitudMinimaContrasena} caracteres";

        return errores;
    }
}

public sealed record LoginRespuesta(
    bool Exito,
    string? Redireccion = null,
    string? Mensaje = null,
    bool CuentaBloqueada = false,
    bool ErrorConexion = false,
    IReadOnlyDictionary<string, string>? Errores = null)
{
    public const string MensajeCredencialesInvalidas = "El correo electrónico o la contraseña son incorrectos.";
    public const string MensajeCuentaBloqueada =
        "Tu cuenta ha sido bloqueada temporalmente. Puedes intentarlo de nuevo en 15 minutos o recuperar tu contraseña.";
    public const string MensajeErrorConexion = "No se pudo conectar al servidor. Verifica tu conexión a internet.";

    public static LoginRespuesta Exitosa(string redireccion) => new(true, Redireccion: redireccion);
    public static LoginRespuesta ConErrores(IReadOnlyDictionary<string, string> errores) => new(false, Errores: errores);
    public static LoginRespuesta CredencialesInvalidas() => new(false, Mensaje: MensajeCredencialesInvalidas);
    public static LoginRespuesta Bloqueada() => new(false, Mensaje: MensajeCuentaBloqueada, CuentaBloqueada: true);
    public static LoginRespuesta SinConexion() => new(false, Mensaje: MensajeErrorConexion, ErrorConexion: true);
}
