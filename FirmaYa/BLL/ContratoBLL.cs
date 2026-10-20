using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FirmaYa.Data;
using FirmaYa.Models;
using FirmaYa.Models.Entidades;
using FirmaYa.Services;

namespace FirmaYa.BLL;

public sealed partial class ContratoBLL(IDAL dal, NotificacionBLL notificacionBLL, HoraLocal hora, Enlaces enlaces)
{
    public const int LongitudMinimaContenido = 100;
    public const int LongitudMaximaComentario = 1000;

    public async Task<ContratoCreado> CrearContrato(string nombreContrato, string partesInvolucradas, DateOnly fechaInicio,
        DateOnly? fechaExpiracion, string? descripcionPropiedad, string tipoPlantilla, Plantilla datosPlantilla,
        UsuarioActual usuarioCreador, CancellationToken ct)
    {
        if (datosPlantilla.TipoContrato != tipoPlantilla)
            throw new ArgumentException("La plantilla no corresponde al tipo seleccionado.", nameof(datosPlantilla));

        var contenidoVersion1 = GenerarVersion1(datosPlantilla, nombreContrato, partesInvolucradas, fechaInicio,
            fechaExpiracion, descripcionPropiedad);
        var hash = CalcularHashSHA256(contenidoVersion1);

        var contrato = new Contrato
        {
            Nombre = nombreContrato,
            DescripcionPropiedad = descripcionPropiedad,
            FechaInicio = fechaInicio,
            FechaExpiracion = fechaExpiracion,
            IdPlantilla = datosPlantilla.IdPlantilla
        };
        var idContrato = await dal.GuardarContrato(contrato, EstadosContrato.Borrador, 1, hash, contenidoVersion1,
            usuarioCreador.IdUsuario, hora.AhoraUtc, ct);

        return new ContratoCreado(idContrato, 1, hash, EstadosContrato.Borrador);
    }

    // Las partes involucradas quedan en este contenido porque el DER no tiene una columna para ellas.
    public static string GenerarVersion1(Plantilla datosPlantilla, string nombreContrato, string partesInvolucradas,
        DateOnly fechaInicio, DateOnly? fechaExpiracion, string? descripcionPropiedad)
    {
        var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["nombre_contrato"] = nombreContrato,
            ["partes_involucradas"] = partesInvolucradas,
            ["fecha_inicio"] = fechaInicio.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            ["fecha_expiracion"] = fechaExpiracion?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "(sin fecha de expiración)",
            ["descripcion_propiedad"] = string.IsNullOrWhiteSpace(descripcionPropiedad) ? "(sin descripción)" : descripcionPropiedad
        };
        var contenido = Marcador().Replace(datosPlantilla.Cuerpo ?? "",
            m => valores.TryGetValue(m.Groups[1].Value, out var valor) ? valor : m.Value);
        return NormalizarContenido(contenido);
    }

    public static string CalcularHashSHA256(string contenido) => HashSha256.Calcular(contenido);

    public async Task<Contrato?> ObtenerContrato(int idContrato, UsuarioActual usuario, CancellationToken ct)
    {
        var datosContrato = await dal.ObtenerContrato(idContrato, ct);
        if (datosContrato is null || datosContrato.EsTitular(usuario.IdUsuario))
            return datosContrato;

        return await dal.ObtenerRolUsuario(idContrato, usuario.IdUsuario, ct) is null ? null : datosContrato;
    }

    public async Task<ResultadoGuardarVersion> GuardarNuevaVersion(int idContrato, string? contenidoNuevo, string? comentarioVersion,
        UsuarioActual autor, CancellationToken ct)
    {
        var contrato = await ObtenerContratoDelTitular(idContrato, autor, ct);
        if (!EstadosContrato.EsEditable(contrato.Estado))
            return new ContratoNoEditable();

        var contenido = NormalizarContenido(contenidoNuevo ?? "");
        if (contenido.Trim().Length < LongitudMinimaContenido)
            return new ContenidoInsuficiente();

        var numeroVersion = await CalcularSiguienteNumeroVersion(idContrato, ct);
        var hash = CalcularHashSHA256(contenido);
        var ahora = hora.AhoraUtc;

        if (!await dal.GuardarVersion(idContrato, contenido, numeroVersion, hash, autor.IdUsuario, ahora, ct))
            return new VersionEnConflicto();

        await dal.ActualizarHistorialVersiones(idContrato, numeroVersion, autor.IdUsuario, ahora, hash,
            string.IsNullOrWhiteSpace(comentarioVersion) ? null : comentarioVersion.Trim(), ct);

        return new VersionGuardada(numeroVersion, hash);
    }

    public async Task<int> CalcularSiguienteNumeroVersion(int idContrato, CancellationToken ct) =>
        ((await dal.ObtenerContrato(idContrato, ct))?.NumeroVersion ?? 0) + 1;

    public async Task<List<ParteContrato>> ObtenerPartesInvitadas(int idContrato, UsuarioActual usuario, CancellationToken ct)
    {
        await ObtenerContratoDelTitular(idContrato, usuario, ct);
        return await dal.ObtenerPartesInvitadas(idContrato, ct);
    }

    public async Task<ResultadoValidacionCorreo> ValidarCorreoFormatoYDuplicado(int idContrato, string? correoElectronico,
        UsuarioActual usuario, CancellationToken ct)
    {
        await ObtenerContratoDelTitular(idContrato, usuario, ct);
        if (!ValidacionCorreo.EsValido(correoElectronico))
            return ResultadoValidacionCorreo.FormatoInvalido;

        return await ExisteCorreoInvitado(idContrato, correoElectronico!.Trim(), ct)
            ? ResultadoValidacionCorreo.YaInvitado
            : ResultadoValidacionCorreo.Valido;
    }

    public Task<bool> ExisteCorreoInvitado(int idContrato, string correoElectronico, CancellationToken ct) =>
        dal.ExisteCorreoInvitado(idContrato, correoElectronico, ct);

    public async Task<ResultadoInvitacion> InvitarParte(int idContrato, string? correoElectronico, string? nombreParte, string? rol,
        string? mensajePersonalizado, UsuarioActual usuario, CancellationToken ct)
    {
        var contrato = await ObtenerContratoDelTitular(idContrato, usuario, ct);
        if (contrato.Estado == EstadosContrato.Archivado)
            return new InvitacionRechazada("", "No se pueden invitar partes a un contrato archivado.");

        switch (await ValidarCorreoFormatoYDuplicado(idContrato, correoElectronico, usuario, ct))
        {
            case ResultadoValidacionCorreo.FormatoInvalido:
                return new InvitacionRechazada("Correo", "Ingrese un correo electrónico válido (ej. nombre@dominio.com)");
            case ResultadoValidacionCorreo.YaInvitado:
                return new InvitacionRechazada("Correo", "Esta dirección ya ha sido invitada a este contrato");
        }
        if (string.IsNullOrWhiteSpace(nombreParte) || nombreParte.Trim().Length > 150)
            return new InvitacionRechazada("NombreParte", "Este campo es obligatorio (máximo 150 caracteres)");
        if (rol is null || !RolesParte.Todos.Contains(rol))
            return new InvitacionRechazada("Rol", "Seleccione un rol");
        if (mensajePersonalizado?.Length > 500)
            return new InvitacionRechazada("MensajePersonalizado", "El mensaje no puede superar los 500 caracteres");

        var correo = correoElectronico!.Trim();
        var token = GenerarTokenAcceso(idContrato, correo);
        await dal.RegistrarParteInvitada(idContrato, correo, rol, token, EstadosInvitacion.Pendiente, hora.AhoraUtc, ct);

        var enlaceTokenizado = enlaces.Acceso(token);
        var correoEnviado = await notificacionBLL.EnviarInvitacionPorCorreo(correo, nombreParte.Trim(), contrato.Nombre ?? "",
            enlaceTokenizado, mensajePersonalizado, idContrato, ct);
        if (!correoEnviado)
            return new ErrorEnvioCorreo(enlaceTokenizado, correo);

        await dal.ActualizarEstadoInvitacion(idContrato, correo, EstadosInvitacion.InvitacionEnviada, ct);
        return new ParteInvitada(correo, EstadosInvitacion.InvitacionEnviada);
    }

    public static string GenerarTokenAcceso(int idContrato, string correoElectronico) =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public async Task ActualizarEstadoInvitacion(int idContrato, string correoElectronico, string estado, UsuarioActual usuario,
        CancellationToken ct)
    {
        await ObtenerContratoDelTitular(idContrato, usuario, ct);
        await dal.ActualizarEstadoInvitacion(idContrato, correoElectronico, estado, ct);
    }

    public string GenerarEnlaceAcceso(ParteContrato parte) => enlaces.Acceso(parte.TokenInvitacion ?? "");

    public Task<Contrato?> ObtenerContratoPorToken(TokenEnlace token, CancellationToken ct) => dal.ObtenerContrato(token, ct);

    public async Task<Contrato> ObtenerEstadoYTransicionesDisponibles(int idContrato, UsuarioActual usuario, CancellationToken ct) =>
        await ObtenerContratoDelTitular(idContrato, usuario, ct);

    public async Task<ResultadoCambioEstado> CambiarEstado(int idContrato, string estadoActual, string nuevoEstado,
        string? razonCambio, UsuarioActual usuario, CancellationToken ct)
    {
        var contrato = await ObtenerContratoDelTitular(idContrato, usuario, ct);
        if (!TransicionPermitida(contrato, estadoActual, nuevoEstado))
            return new TransicionNoPermitida();

        if (nuevoEstado == EstadosContrato.ListoParaFirmar && !await ExisteFirmanteAsignado(idContrato, ct))
            return new AvisoSinFirmantes();

        return await AplicarCambioEstado(contrato, nuevoEstado, razonCambio, usuario, ct);
    }

    public async Task<ResultadoCambioEstado> ConfirmarCambioEstado(int idContrato, string estadoActual, string nuevoEstado,
        string? razonCambio, UsuarioActual usuario, CancellationToken ct)
    {
        var contrato = await ObtenerContratoDelTitular(idContrato, usuario, ct);
        if (!TransicionPermitida(contrato, estadoActual, nuevoEstado))
            return new TransicionNoPermitida();

        return await AplicarCambioEstado(contrato, nuevoEstado, razonCambio, usuario, ct);
    }

    public Task<bool> ExisteFirmanteAsignado(int idContrato, CancellationToken ct) => dal.ExisteFirmanteAsignado(idContrato, ct);

    private async Task<ResultadoCambioEstado> AplicarCambioEstado(Contrato contrato, string nuevoEstado, string? razonCambio,
        UsuarioActual usuario, CancellationToken ct)
    {
        await dal.ActualizarEstadoContrato(contrato.IdContrato, nuevoEstado, ct);
        await dal.RegistrarAuditoria(contrato.IdContrato, usuario.IdUsuario, contrato.Estado ?? "", nuevoEstado, hora.AhoraUtc,
            string.IsNullOrWhiteSpace(razonCambio) ? null : razonCambio.Trim(), ct);
        await notificacionBLL.NotificarCambioEstado(contrato.IdContrato, contrato.Nombre ?? "", nuevoEstado, ct);
        return new EstadoActualizado(nuevoEstado);
    }

    private static bool TransicionPermitida(Contrato contrato, string estadoActual, string nuevoEstado) =>
        contrato.Estado == estadoActual && contrato.TransicionesDisponibles.Contains(nuevoEstado);

    public Task<string?> ObtenerRolUsuario(int idContrato, UsuarioActual usuario, CancellationToken ct) =>
        dal.ObtenerRolUsuario(idContrato, usuario.IdUsuario, ct);

    public static bool RolPermiteComentar(string? rolUsuario) =>
        rolUsuario is RolesParte.Titular or RolesParte.Firmante or RolesParte.Revisor;

    public async Task<ResultadoComentario> PublicarComentario(int idContrato, UsuarioActual autor, string? textoComentario,
        string? fragmentoReferenciado, CancellationToken ct)
    {
        if (!RolPermiteComentar(await ObtenerRolUsuario(idContrato, autor, ct)))
            return new ComentarioRechazado("No puede añadir comentarios con su rol actual.");

        var contrato = await dal.ObtenerContrato(idContrato, ct) ?? throw new AccesoDenegadoException();
        if (contrato.Estado == EstadosContrato.Archivado)
            return new ComentarioRechazado("El contrato está archivado y no admite comentarios.");

        var texto = textoComentario?.Trim() ?? "";
        if (texto.Length == 0)
            return new ComentarioRechazado("El comentario no puede estar vacío");
        if (texto.Length > LongitudMaximaComentario)
            return new ComentarioRechazado("0 caracteres restantes");

        var ahora = hora.AhoraUtc;
        await dal.RegistrarComentario(idContrato, autor.IdUsuario, ahora, texto, ct);
        await notificacionBLL.NotificarNuevoComentario(idContrato, contrato.Nombre ?? "", autor, texto, ct);

        return new ComentarioPublicado(autor.NombreCompleto, ahora, texto, fragmentoReferenciado);
    }

    public async Task<List<Comentario>> ObtenerComentarios(int idContrato, UsuarioActual usuario, CancellationToken ct)
    {
        if (await ObtenerRolUsuario(idContrato, usuario, ct) is null)
            throw new AccesoDenegadoException();
        return await dal.ObtenerComentarios(idContrato, ct);
    }

    public async Task<List<ParteContrato>> ObtenerFirmantes(int idContrato, UsuarioActual usuario, CancellationToken ct)
    {
        await ObtenerContratoDelTitular(idContrato, usuario, ct);
        return await dal.ObtenerFirmantes(idContrato, ct);
    }

    // La lista de firmantes se vuelve a leer de la base; no se confía en la enviada por el formulario.
    public async Task<ResultadoSolicitudes> SolicitarFirmas(int idContrato, string? canalNotificacion, string? mensajePersonalizado,
        DateOnly? fechaLimiteFirma, UsuarioActual usuario, CancellationToken ct)
    {
        var contrato = await ObtenerContratoDelTitular(idContrato, usuario, ct);
        var listaFirmantes = await dal.ObtenerFirmantes(idContrato, ct);

        var error = contrato.Estado != EstadosContrato.ListoParaFirmar ? "El contrato debe estar en estado \"Listo para firmar\"."
            : listaFirmantes.Count == 0 ? "No hay firmantes asignados."
            : canalNotificacion != CanalesNotificacion.CorreoElectronico ? "Seleccione un canal de notificación."
            : fechaLimiteFirma is { } limite && limite <= hora.Hoy ? "La fecha límite debe ser posterior a la fecha actual"
            : mensajePersonalizado?.Length > 500 ? "El mensaje no puede superar los 500 caracteres."
            : null;
        if (error is not null)
            return new ResultadoSolicitudes([], [], listaFirmantes.Count, error);

        var notificados = new List<ParteContrato>();
        var conError = new List<ParteContrato>();
        // Los que ya firmaron o tienen el enlace bloqueado no se notifican.
        foreach (var firmante in listaFirmantes.Where(f => f.EstadoFirma != EstadosFirma.Firmado
                                                         && f.EstadoInvitacion != EstadosInvitacion.Bloqueado))
        {
            var enlaceFirma = GenerarEnlaceFirma(idContrato, firmante);
            await dal.RegistrarSolicitudFirma(idContrato, firmante.IdParte, hora.AhoraUtc, canalNotificacion!, fechaLimiteFirma,
                usuario.IdUsuario, ct);

            if (await notificacionBLL.EnviarSolicitudFirma(firmante, canalNotificacion!, enlaceFirma, mensajePersonalizado,
                    contrato.Nombre ?? "", fechaLimiteFirma, ct))
            {
                await dal.ActualizarEstadoFirmante(idContrato, firmante.IdParte, EstadosFirma.Notificado, ct);
                notificados.Add(firmante);
            }
            else
            {
                conError.Add(firmante);
            }
        }
        return new ResultadoSolicitudes(notificados, conError, listaFirmantes.Count);
    }

    public async Task<ResultadoSolicitudes> ReintentarEnviosFallidos(int idContrato, IReadOnlyCollection<int> firmantesConError,
        string canalNotificacion, string? mensajePersonalizado, UsuarioActual usuario, CancellationToken ct)
    {
        var contrato = await ObtenerContratoDelTitular(idContrato, usuario, ct);
        var listaFirmantes = await dal.ObtenerFirmantes(idContrato, ct);
        var pendientes = listaFirmantes
            .Where(f => firmantesConError.Contains(f.IdParte) && f.EstadoFirma != EstadosFirma.Firmado)
            .Select(f => (f, GenerarEnlaceFirma(idContrato, f)))
            .ToList();

        var resultado = await notificacionBLL.EnviarSolicitudFirma(pendientes, canalNotificacion, mensajePersonalizado,
            contrato.Nombre ?? "", ct);
        foreach (var (firmante, _) in resultado.Where(r => r.Enviado))
            await dal.ActualizarEstadoFirmante(idContrato, firmante.IdParte, EstadosFirma.Notificado, ct);

        return new ResultadoSolicitudes(
            resultado.Where(r => r.Enviado).Select(r => r.Firmante).ToList(),
            resultado.Where(r => !r.Enviado).Select(r => r.Firmante).ToList(),
            listaFirmantes.Count);
    }

    public string GenerarEnlaceFirma(int idContrato, ParteContrato firmante) => enlaces.Firma(firmante.TokenInvitacion ?? "");

    public Task<Contrato?> ObtenerDatosParaFirma(TokenEnlace token, CancellationToken ct) => dal.ObtenerContratoPorToken(token, ct);

    public async Task<FirmaRegistrada> RegistrarFirma(int idContrato, ParteContrato firmante, DateTime fechaHora, string? ip,
        int versionContrato, string hashVersion, CancellationToken ct)
    {
        var hashFirma = CalcularHashSHA256(
            $"{hashVersion}|{firmante.IdParte}|{firmante.CorreoInvitado}|{fechaHora.ToString("O", CultureInfo.InvariantCulture)}|{ip}");

        await dal.RegistrarFirma(idContrato, firmante.IdParte, fechaHora, ip, versionContrato, hashVersion, hashFirma, ct);
        await dal.ActualizarEstadoFirmante(idContrato, firmante.IdParte, EstadosFirma.Firmado, ct);

        if (!await VerificarTodosFirmaron(idContrato, ct))
            return new FirmaRegistrada(false);

        await dal.ActualizarEstadoContrato(idContrato, EstadosContrato.Firmado, ct);
        var parteResponsable = await dal.ObtenerContrato(idContrato, ct);
        if (parteResponsable is not null)
            await notificacionBLL.NotificarContratoFirmado(idContrato, parteResponsable, ct);
        return new FirmaRegistrada(true);
    }

    public Task<bool> VerificarTodosFirmaron(int idContrato, CancellationToken ct) => dal.VerificarTodosFirmaron(idContrato, ct);

    public async Task<EstadoFirmas> ObtenerEstadoFirmas(int idContrato, UsuarioActual usuario, CancellationToken ct)
    {
        await ObtenerContratoDelTitular(idContrato, usuario, ct);
        if (!await dal.ExistenSolicitudesFirma(idContrato, ct))
            return new EstadoFirmas(false, []);
        return new EstadoFirmas(true, await dal.ObtenerFirmantesConEstado(idContrato, ct));
    }

    public async Task<ResultadoReenvio> ReenviarSolicitudFirma(int idContrato, int firmante, UsuarioActual usuario, CancellationToken ct)
    {
        var contrato = await ObtenerContratoDelTitular(idContrato, usuario, ct);
        var parte = (await dal.ObtenerFirmantesConEstado(idContrato, ct)).FirstOrDefault(f => f.IdParte == firmante);
        if (parte is null || parte.EstadoFirma == EstadosFirma.Firmado)
            return new ReenvioRechazado("El firmante ya completó la firma.");
        if (parte.EstadoInvitacion == EstadosInvitacion.Bloqueado)
            return new ReenvioRechazado("El enlace de firma de esta parte está bloqueado por seguridad.");

        var enlaceFirmante = GenerarEnlaceFirma(idContrato, parte);
        if (!await notificacionBLL.ReenviarNotificacionFirma(parte, idContrato, enlaceFirmante, contrato.Nombre ?? "", ct))
            return new ErrorReenvio(enlaceFirmante);

        var ahora = hora.AhoraUtc;
        await dal.ActualizarEstadoFirmante(idContrato, parte.IdParte, EstadosFirma.ReNotificado, ahora, usuario.IdUsuario, ct);
        return new SolicitudReenviada(parte, ahora);
    }

    public Task<(Contrato? DatosContrato, bool AccesoUsuario)> VerificarAccesoYEstado(int idContrato, UsuarioActual usuario,
        CancellationToken ct) =>
        dal.ObtenerContrato(idContrato, usuario.IdUsuario, ct);

    public Task RegistrarDescarga(int idContrato, int? usuario, DateTime fechaHora, int version, string? detalle, CancellationToken ct) =>
        dal.RegistrarAuditoria(idContrato, usuario, fechaHora, version, TiposAccion.Descarga, detalle, ct);

    // Permisos administrativos sobre el contrato: solo el usuario creador.
    private async Task<Contrato> ObtenerContratoDelTitular(int idContrato, UsuarioActual usuario, CancellationToken ct)
    {
        var contrato = await dal.ObtenerContrato(idContrato, ct);
        return contrato is not null && contrato.EsTitular(usuario.IdUsuario) ? contrato : throw new AccesoDenegadoException();
    }

    // El contenido se guarda con saltos de línea \n para que el hash no dependa del navegador.
    public static string NormalizarContenido(string contenido) => contenido.Replace("\r\n", "\n").Replace('\r', '\n');

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}")]
    private static partial Regex Marcador();
}
