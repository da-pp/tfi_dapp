using FirmaYa.Models;
using FirmaYa.Models.Entidades;

namespace FirmaYa.Data.Memory;

public sealed partial class DALMemoria
{
    public Task<Usuario?> ObtenerUsuarioPorCorreo(string correoElectronico, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var usuario = BuscarPorCorreo(correoElectronico)?.Copiar();
            if (usuario is not null)
            {
                usuario.FechaUltimoBloqueo = _auditoria
                    .Where(a => a.IdUsuario == usuario.IdUsuario && a.TipoAccion == TiposAccion.BloqueoCuenta)
                    .Select(a => (DateTime?)a.FechaHora)
                    .Max();
            }
            return Task.FromResult(usuario);
        }
    }

    public Task RegistrarInicioSesion(string correoElectronico, DateTime fechaHora, string? ip, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var usuario = BuscarPorCorreo(correoElectronico);
            if (usuario?.Estado is EstadosUsuario.Activo or EstadosUsuario.Bloqueado)
            {
                usuario.IntentosFallidos = 0;
                usuario.Estado = EstadosUsuario.Activo;
            }
            InsertarAuditoria(usuario?.IdUsuario, TiposAccion.InicioSesion, "usuario", usuario?.IdUsuario, fechaHora, ip,
                "Inicio de sesión exitoso.");
        }
        return Task.CompletedTask;
    }

    public Task<int> IncrementarIntentosFallidos(string correoElectronico, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var usuario = BuscarPorCorreo(correoElectronico);
            if (usuario is null || usuario.Estado is not (EstadosUsuario.Activo or EstadosUsuario.Bloqueado))
                return Task.FromResult(0);

            usuario.IntentosFallidos = usuario.Estado == EstadosUsuario.Bloqueado ? 1 : usuario.IntentosFallidos + 1;
            usuario.Estado = EstadosUsuario.Activo;
            return Task.FromResult(usuario.IntentosFallidos);
        }
    }

    public Task BloquearCuenta(string correoElectronico, int duracionMinutos, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var usuario = BuscarPorCorreo(correoElectronico);
            if (usuario is not null)
                usuario.Estado = EstadosUsuario.Bloqueado;
            InsertarAuditoria(usuario?.IdUsuario, TiposAccion.BloqueoCuenta, "usuario", usuario?.IdUsuario,
                _reloj.GetUtcNow().UtcDateTime, null, $"Cuenta bloqueada por {duracionMinutos} minutos por intentos fallidos.");
        }
        return Task.CompletedTask;
    }

    public Task<ParteContrato?> ObtenerTokenAcceso(TokenEnlace token, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var fila = BuscarParte(token);
            return Task.FromResult(fila is null ? null : ArmarParte(fila));
        }
    }

    public Task RegistrarAcceso(TokenEnlace token, DateTime fechaHora, string? ip, CancellationToken ct) =>
        AuditoriaPorToken(token, TiposAccion.AccesoInvitado, fechaHora, ip, "Acceso al contrato mediante enlace tokenizado.");

    public Task RenovarToken(TokenEnlace token, DateTime fechaHora, CancellationToken ct) =>
        AuditoriaPorToken(token, TiposAccion.RenovacionSesion, fechaHora, null, "La parte extendió la sesión.");

    public Task<ParteContrato?> ObtenerTokenFirma(TokenEnlace token, CancellationToken ct) => ObtenerTokenAcceso(token, ct);

    public Task RegistrarOTP(TokenEnlace token, string codigoOTP, DateTime fechaEnvio, DateTime fechaExpiracion, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var parte = BuscarParte(token);
            if (parte is null)
                return Task.CompletedTask;

            var prefijo = HashSha256.Calcular(token.Valor);
            foreach (var anterior in _tokens.Where(t => t.TipoToken == TiposToken.Otp && !t.Usado && t.Valor.StartsWith(prefijo + ":")))
                anterior.Usado = true;

            _tokens.Add(new TokenSeguridad
            {
                IdToken = SiguienteId("token_seguridad"),
                IdUsuario = parte.IdUsuario,
                Valor = $"{prefijo}:{HashSha256.CodigoOtp(token.Valor, codigoOTP)}",
                TipoToken = TiposToken.Otp,
                FechaCreacion = fechaEnvio,
                FechaExpiracion = fechaExpiracion
            });
        }
        return Task.CompletedTask;
    }

    public Task<OtpRegistrado?> ObtenerOTPRegistrado(TokenEnlace token, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var prefijo = HashSha256.Calcular(token.Valor) + ":";
            var otp = _tokens
                .Where(t => t.TipoToken == TiposToken.Otp && t.Valor.StartsWith(prefijo))
                .OrderByDescending(t => t.FechaCreacion).ThenByDescending(t => t.IdToken)
                .FirstOrDefault();
            return Task.FromResult(otp is null
                ? null
                : new OtpRegistrado(otp.Valor[prefijo.Length..], otp.FechaCreacion, otp.FechaExpiracion, otp.Usado));
        }
    }

    public Task<int> IncrementarIntentosFallidos(TokenEnlace token, int maxIntentos, DateTime fechaHora, CancellationToken ct)
    {
        lock (_bloqueo)
        {
            var parte = BuscarParte(token);
            if (parte is null)
                return Task.FromResult(0);

            InsertarAuditoria(parte.IdUsuario, TiposAccion.OtpIncorrecto, "usuario_contrato", parte.IdParte, fechaHora, null,
                "Código OTP incorrecto.");
            var fallidos = _auditoria.Count(a => a.TipoAccion == TiposAccion.OtpIncorrecto
                                                 && a.EntidadAfectada == "usuario_contrato" && a.IdEntidadAfectada == parte.IdParte);
            var restantes = Math.Max(maxIntentos - fallidos, 0);
            if (restantes == 0)
            {
                parte.EstadoInvitacion = EstadosInvitacion.Bloqueado;
                InsertarAuditoria(parte.IdUsuario, TiposAccion.BloqueoEnlace, "usuario_contrato", parte.IdParte, fechaHora, null,
                    $"Enlace de firma bloqueado tras {maxIntentos} intentos fallidos.");
            }
            return Task.FromResult(restantes);
        }
    }

    private Task AuditoriaPorToken(TokenEnlace token, string tipoAccion, DateTime fechaHora, string? ip, string descripcion)
    {
        lock (_bloqueo)
        {
            var parte = BuscarParte(token);
            if (parte is not null)
                InsertarAuditoria(parte.IdUsuario, tipoAccion, "usuario_contrato", parte.IdParte, fechaHora, ip, descripcion);
        }
        return Task.CompletedTask;
    }

    private Usuario? BuscarPorCorreo(string correoElectronico) =>
        _usuarios.FirstOrDefault(u => string.Equals(u.Email, correoElectronico, StringComparison.OrdinalIgnoreCase));
}
