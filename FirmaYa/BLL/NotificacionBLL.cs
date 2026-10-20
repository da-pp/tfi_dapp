using FirmaYa.Data;
using FirmaYa.Models.Entidades;
using FirmaYa.Services;

namespace FirmaYa.BLL;

// Lo que se registra en notificacion.mensaje nunca incluye tokens ni códigos OTP.
public sealed class NotificacionBLL(IDAL dal, CorreoService correo, HoraLocal hora)
{
    public async Task<bool> EnviarInvitacionPorCorreo(string correoElectronico, string nombreParte, string nombreContrato,
        string enlaceTokenizado, string? mensajePersonalizado, int idContrato, CancellationToken ct)
    {
        var cuerpo = $"""
            Hola {nombreParte}:

            Fuiste invitado/a al contrato "{nombreContrato}" en FirmaYa.
            {Personalizado(mensajePersonalizado)}
            Accedé al contrato desde este enlace:
            {enlaceTokenizado}
            """;
        var enviado = await correo.EnviarAsync(correoElectronico, $"Invitación al contrato {nombreContrato}", cuerpo, ct);

        await dal.RegistrarEnvioCorreo(Nueva(idContrato, null, correoElectronico, TiposNotificacion.Invitacion,
            $"Invitación al contrato {nombreContrato}.", enviado), ct);
        return enviado;
    }

    public async Task<int> NotificarCambioEstado(int idContrato, string nombreContrato, string nuevoEstado, CancellationToken ct)
    {
        var listaPartes = await ObtenerPartesConNotificacionesActivas(idContrato, ct);
        var mensaje = $"El contrato \"{nombreContrato}\" cambió al estado \"{nuevoEstado}\".";

        var notificaciones = await EnviarATodas(listaPartes, idContrato, TiposNotificacion.CambioEstado,
            $"Cambio de estado: {nombreContrato}", mensaje, ct);
        await dal.EnviarNotificacionesCambioEstado(notificaciones, ct);
        return notificaciones.Count(n => n.EstadoEnvio == EstadosEnvio.Enviado);
    }

    public async Task<int> NotificarNuevoComentario(int idContrato, string nombreContrato, UsuarioActual autor,
        string textoComentario, CancellationToken ct)
    {
        // "las otras partes": se excluye al autor si también es parte del contrato.
        var listaPartes = (await ObtenerPartesConNotificacionesActivas(idContrato, ct))
            .Where(p => p.IdUsuario != autor.IdUsuario).ToList();
        var mensaje = $"{autor.NombreCompleto} comentó en el contrato \"{nombreContrato}\":\n\n{textoComentario}";

        var notificaciones = await EnviarATodas(listaPartes, idContrato, TiposNotificacion.NuevoComentario,
            $"Nuevo comentario: {nombreContrato}", mensaje, ct);
        await dal.EnviarNotificacionesComentario(notificaciones, ct);
        return notificaciones.Count(n => n.EstadoEnvio == EstadosEnvio.Enviado);
    }

    public Task<List<ParteContrato>> ObtenerPartesConNotificacionesActivas(int idContrato, CancellationToken ct) =>
        dal.ObtenerPartesConNotificacionesActivas(idContrato, ct);

    public async Task<bool> EnviarSolicitudFirma(ParteContrato firmante, string canalNotificacion, string enlaceFirma,
        string? mensajePersonalizado, string nombreContrato, DateOnly? fechaLimiteFirma, CancellationToken ct)
    {
        var notificacion = await EnviarSolicitud(firmante, canalNotificacion, enlaceFirma, mensajePersonalizado,
            nombreContrato, fechaLimiteFirma, ct);
        await dal.RegistrarEnvioNotificacion(notificacion, ct);
        return notificacion.EstadoEnvio == EstadosEnvio.Enviado;
    }

    public async Task<List<(ParteContrato Firmante, bool Enviado)>> EnviarSolicitudFirma(
        IReadOnlyList<(ParteContrato Firmante, string EnlaceFirma)> firmantesConError, string canalNotificacion,
        string? mensajePersonalizado, string nombreContrato, CancellationToken ct)
    {
        var notificaciones = new List<Notificacion>();
        var resultado = new List<(ParteContrato, bool)>();
        foreach (var (firmante, enlace) in firmantesConError)
        {
            var notificacion = await EnviarSolicitud(firmante, canalNotificacion, enlace, mensajePersonalizado, nombreContrato, null, ct);
            notificaciones.Add(notificacion);
            resultado.Add((firmante, notificacion.EstadoEnvio == EstadosEnvio.Enviado));
        }
        await dal.RegistrarEnvioNotificacion(notificaciones, ct);
        return resultado;
    }

    public async Task<bool> ReenviarNotificacionFirma(ParteContrato firmante, int idContrato, string enlaceFirma,
        string nombreContrato, CancellationToken ct)
    {
        var notificacion = await EnviarSolicitud(firmante, CanalesNotificacion.CorreoElectronico, enlaceFirma, null,
            nombreContrato, null, ct);
        await dal.RegistrarEnvioNotificacion(notificacion, ct);
        return notificacion.EstadoEnvio == EstadosEnvio.Enviado;
    }

    public async Task<bool> EnviarCodigoOTP(string correoFirmante, string codigoOTP, int idContrato, CancellationToken ct)
    {
        var cuerpo = $"""
            Tu código para firmar el contrato en FirmaYa es: {codigoOTP}

            El código expira en 10 minutos. Si no solicitaste firmar un contrato, ignorá este mensaje.
            """;
        var enviado = await correo.EnviarAsync(correoFirmante, "Código de verificación para firmar", cuerpo, ct);

        await dal.RegistrarEnvioOTP(Nueva(idContrato, null, correoFirmante, TiposNotificacion.CodigoOtp,
            "Código OTP de firma enviado.", enviado), ct);
        return enviado;
    }

    public async Task<bool> NotificarContratoFirmado(int idContrato, Contrato parteResponsable, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(parteResponsable.EmailCreador))
            return false;

        var mensaje = $"Todas las partes firmaron el contrato \"{parteResponsable.Nombre}\". El contrato está en estado Firmado.";
        var enviado = await correo.EnviarAsync(parteResponsable.EmailCreador, $"Contrato firmado: {parteResponsable.Nombre}", mensaje, ct);

        await dal.EnviarNotificacionContratoFirmado(Nueva(idContrato, parteResponsable.IdUsuarioCreador,
            parteResponsable.EmailCreador, TiposNotificacion.ContratoFirmado, mensaje, enviado), ct);
        return enviado;
    }

    private async Task<Notificacion> EnviarSolicitud(ParteContrato firmante, string canalNotificacion, string enlaceFirma,
        string? mensajePersonalizado, string nombreContrato, DateOnly? fechaLimiteFirma, CancellationToken ct)
    {
        var lineaLimite = fechaLimiteFirma is { } limite ? $"Fecha límite para firmar: {limite:dd/MM/yyyy}.\n" : "";
        var cuerpo = $"""
            Hola {firmante.NombreVisible}:

            Se solicita tu firma para el contrato "{nombreContrato}".
            {Personalizado(mensajePersonalizado)}{lineaLimite}
            Leé y firmá el contrato desde este enlace:
            {enlaceFirma}
            """;
        var enviado = await correo.EnviarAsync(firmante.CorreoInvitado ?? "", $"Solicitud de firma: {nombreContrato}", cuerpo, ct);

        var registro = $"Solicitud de firma del contrato {nombreContrato}."
            + (fechaLimiteFirma is { } fecha ? $" Fecha límite: {fecha:dd/MM/yyyy}." : "");
        var notificacion = Nueva(firmante.IdContrato, firmante.IdUsuario, firmante.CorreoInvitado, TiposNotificacion.SolicitudFirma,
            registro, enviado);
        notificacion.Canal = canalNotificacion;
        return notificacion;
    }

    private async Task<List<Notificacion>> EnviarATodas(IEnumerable<ParteContrato> partes, int idContrato, string tipo,
        string asunto, string mensaje, CancellationToken ct)
    {
        var notificaciones = new List<Notificacion>();
        foreach (var parte in partes.Where(p => !string.IsNullOrWhiteSpace(p.CorreoInvitado)))
        {
            var enviado = await correo.EnviarAsync(parte.CorreoInvitado!, asunto, mensaje, ct);
            notificaciones.Add(Nueva(idContrato, parte.IdUsuario, parte.CorreoInvitado, tipo, mensaje, enviado));
        }
        return notificaciones;
    }

    private Notificacion Nueva(int idContrato, int? idUsuario, string? correoDestino, string tipo, string mensaje, bool enviado) => new()
    {
        IdContrato = idContrato,
        IdUsuario = idUsuario,
        CorreoDestino = correoDestino,
        Tipo = tipo,
        Mensaje = mensaje,
        EstadoEnvio = enviado ? EstadosEnvio.Enviado : EstadosEnvio.Error,
        FechaEnvio = hora.AhoraUtc
    };

    private static string Personalizado(string? mensaje) =>
        string.IsNullOrWhiteSpace(mensaje) ? "" : $"\nMensaje: {mensaje}\n";
}
