using System.Security.Claims;
using System.Security.Cryptography;
using FirmaYa.Data;
using FirmaYa.Models.Entidades;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FirmaYa.BLL;

public sealed class AccesoOptions
{
    public int DuracionSesionMinutos { get; set; } = 60;
}

public abstract record ResultadoAutenticacion;

public sealed record SesionAutenticada(
    string NombreUsuario, string RolUsuario, ClaimsPrincipal DatosSesion, DateTime FechaExpiracion)
    : ResultadoAutenticacion;

public sealed record CredencialesInvalidas(int IntentosFallidos) : ResultadoAutenticacion;

public sealed record CuentaBloqueada : ResultadoAutenticacion;

public enum ResultadoValidacionOTP
{
    Correcto,
    Incorrecto,
    Expirado,
    FormatoInvalido,
    EnlaceBloqueado
}

public sealed record ValidacionOTP(ResultadoValidacionOTP Resultado, int IntentosRestantes = 0);

public sealed class AccesoBLL(
    IDAL dal,
    IPasswordHasher<Usuario> hasher,
    IOptions<AccesoOptions> opciones,
    TimeProvider reloj,
    NotificacionBLL notificacionBLL)
{
    public const int MaxIntentosFallidos = 5;
    public const int DuracionBloqueoMinutos = 15;

    public const int MinutosExpiracionOTP = 10;
    public const int MaxIntentosOTP = 3;

    // Hash de referencia: un correo inexistente cuesta lo mismo que uno existente,
    // así el tiempo de respuesta no revela qué cuentas están registradas.
    private static readonly Usuario UsuarioFicticio = new();
    private static readonly Lazy<string> HashFicticio =
        new(() => new PasswordHasher<Usuario>().HashPassword(UsuarioFicticio, Guid.NewGuid().ToString()));

    public async Task<ResultadoAutenticacion> Autenticar(
        string correoElectronico, string contrasenia, string? ip, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;
        var datosUsuario = await ObtenerUsuarioPorCorreo(correoElectronico, ct);

        if (datosUsuario is null)
        {
            hasher.VerifyHashedPassword(UsuarioFicticio, HashFicticio.Value, contrasenia);
            return new CredencialesInvalidas(0);
        }

        if (BloqueoVigente(datosUsuario, ahora))
            return new CuentaBloqueada();

        // Una cuenta Inactiva no suma intentos: si no, al vencer el bloqueo quedaría Activa.
        if (!CuentaActiva(datosUsuario))
            return new CredencialesInvalidas(0);

        if (!ContraseniaCorrecta(datosUsuario, contrasenia))
        {
            var intentosFallidos = await IncrementarIntentosFallidos(correoElectronico, ct);
            if (intentosFallidos < MaxIntentosFallidos)
                return new CredencialesInvalidas(intentosFallidos);

            await BloquearCuenta(correoElectronico, DuracionBloqueoMinutos, ct);
            return new CuentaBloqueada();
        }

        await RegistrarInicioSesion(correoElectronico, ahora, ip, ct);
        return GenerarSesion(datosUsuario, ahora);
    }

    public Task<Usuario?> ObtenerUsuarioPorCorreo(string correoElectronico, CancellationToken ct) =>
        dal.ObtenerUsuarioPorCorreo(correoElectronico, ct);

    public Task RegistrarInicioSesion(string correoElectronico, DateTime fechaHora, string? ip, CancellationToken ct) =>
        dal.RegistrarInicioSesion(correoElectronico, fechaHora, ip, ct);

    public Task<int> IncrementarIntentosFallidos(string correoElectronico, CancellationToken ct) =>
        dal.IncrementarIntentosFallidos(correoElectronico, ct);

    public Task BloquearCuenta(string correoElectronico, int duracionMinutos, CancellationToken ct) =>
        dal.BloquearCuenta(correoElectronico, duracionMinutos, ct);

    public SesionAutenticada GenerarSesion(Usuario datosUsuario, DateTime ahora)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, datosUsuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, $"{datosUsuario.Nombre} {datosUsuario.Apellido}".Trim()),
            new(ClaimTypes.GivenName, datosUsuario.Nombre ?? ""),
            new(ClaimTypes.Email, datosUsuario.Email ?? ""),
            new(ClaimTypes.Role, datosUsuario.NombreRol!)
        ];
        var datosSesion = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        return new SesionAutenticada(
            datosUsuario.Nombre ?? "",
            datosUsuario.NombreRol!,
            datosSesion,
            ahora.AddMinutes(opciones.Value.DuracionSesionMinutos));
    }

    // El DER no define vencimiento por tiempo para usuario_contrato.token_invitacion.
    public async Task<ParteContrato?> ValidarToken(TokenEnlace token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token.Valor))
            return null;
        var datosToken = await ObtenerTokenAcceso(token, ct);
        return datosToken is null || datosToken.EstadoInvitacion == EstadosInvitacion.Bloqueado ? null : datosToken;
    }

    public Task<ParteContrato?> ObtenerTokenAcceso(TokenEnlace token, CancellationToken ct) => dal.ObtenerTokenAcceso(token, ct);

    public Task RegistrarAcceso(TokenEnlace token, DateTime fechaHora, string? ip, CancellationToken ct) =>
        dal.RegistrarAcceso(token, fechaHora, ip, ct);

    public async Task<bool> RenovarToken(TokenEnlace token, CancellationToken ct)
    {
        if (await ValidarToken(token, ct) is null)
            return false;
        await dal.RenovarToken(token, reloj.GetUtcNow().UtcDateTime, ct);
        return true;
    }

    public async Task<ParteContrato?> ValidarTokenFirma(TokenEnlace token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token.Valor))
            return null;
        var datosToken = await dal.ObtenerTokenFirma(token, ct);
        return datosToken is null
               || datosToken.EstadoInvitacion == EstadosInvitacion.Bloqueado
               || datosToken.RolParte != RolesParte.Firmante
            ? null
            : datosToken;
    }

    public async Task<bool> GenerarYEnviarOTP(TokenEnlace token, CancellationToken ct)
    {
        var firmante = await ValidarTokenFirma(token, ct);
        if (firmante?.CorreoInvitado is null)
            return false;

        var codigoOTP = GenerarCodigoOTP();
        var fechaEnvio = reloj.GetUtcNow().UtcDateTime;
        await dal.RegistrarOTP(token, codigoOTP, fechaEnvio, fechaEnvio.AddMinutes(MinutosExpiracionOTP), ct);
        return await notificacionBLL.EnviarCodigoOTP(firmante.CorreoInvitado, codigoOTP, firmante.IdContrato, ct);
    }

    public static string GenerarCodigoOTP() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public async Task<ValidacionOTP> ValidarOTP(TokenEnlace token, string? codigoOTP, CancellationToken ct)
    {
        if (codigoOTP is null || codigoOTP.Length != 6 || !codigoOTP.All(char.IsAsciiDigit))
            return new ValidacionOTP(ResultadoValidacionOTP.FormatoInvalido);
        if (await ValidarTokenFirma(token, ct) is null)
            return new ValidacionOTP(ResultadoValidacionOTP.EnlaceBloqueado);

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var otpRegistrado = await dal.ObtenerOTPRegistrado(token, ct);
        if (otpRegistrado is null || otpRegistrado.Usado || ahora >= otpRegistrado.FechaExpiracion)
            return new ValidacionOTP(ResultadoValidacionOTP.Expirado);

        var hashIngresado = Models.HashSha256.CodigoOtp(token.Valor, codigoOTP);
        if (CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(hashIngresado), System.Text.Encoding.ASCII.GetBytes(otpRegistrado.HashCodigo)))
            return new ValidacionOTP(ResultadoValidacionOTP.Correcto);

        var intentosRestantes = await IncrementarIntentosFallidos(token, ct);
        return intentosRestantes == 0
            ? new ValidacionOTP(ResultadoValidacionOTP.EnlaceBloqueado)
            : new ValidacionOTP(ResultadoValidacionOTP.Incorrecto, intentosRestantes);
    }

    public Task<int> IncrementarIntentosFallidos(TokenEnlace token, CancellationToken ct) =>
        dal.IncrementarIntentosFallidos(token, MaxIntentosOTP, reloj.GetUtcNow().UtcDateTime, ct);

    private static bool BloqueoVigente(Usuario datosUsuario, DateTime ahora) =>
        datosUsuario.Estado == EstadosUsuario.Bloqueado
        && (datosUsuario.FechaUltimoBloqueo is null
            || ahora < datosUsuario.FechaUltimoBloqueo.Value.AddMinutes(DuracionBloqueoMinutos));

    // Un estado Bloqueado que llega hasta aquí ya venció y cuenta como activo.
    private static bool CuentaActiva(Usuario datosUsuario) =>
        (datosUsuario.Estado is EstadosUsuario.Activo or EstadosUsuario.Bloqueado)
        && Roles.EsUsuarioInterno(datosUsuario.NombreRol);

    private bool ContraseniaCorrecta(Usuario datosUsuario, string contrasenia)
    {
        if (string.IsNullOrEmpty(datosUsuario.ContrasenaHash))
            return false;

        try
        {
            return hasher.VerifyHashedPassword(datosUsuario, datosUsuario.ContrasenaHash, contrasenia)
                is not PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
