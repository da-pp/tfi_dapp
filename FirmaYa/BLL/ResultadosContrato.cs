using FirmaYa.Models.Entidades;

namespace FirmaYa.BLL;

public sealed class AccesoDenegadoException() : Exception("El usuario no tiene permisos sobre el contrato.");

public sealed record ContratoCreado(int IdContrato, int Version, string Hash, string Estado);

public abstract record ResultadoGuardarVersion;

public sealed record VersionGuardada(int NumeroVersion, string Hash) : ResultadoGuardarVersion;

public sealed record ContenidoInsuficiente : ResultadoGuardarVersion;

public sealed record ContratoNoEditable : ResultadoGuardarVersion;

public sealed record VersionEnConflicto : ResultadoGuardarVersion;

public enum ResultadoValidacionCorreo
{
    Valido,
    FormatoInvalido,
    YaInvitado
}

public abstract record ResultadoInvitacion;

public sealed record ParteInvitada(string CorreoElectronico, string Estado) : ResultadoInvitacion;

public sealed record ErrorEnvioCorreo(string EnlaceTokenizado, string CorreoElectronico) : ResultadoInvitacion;

public sealed record InvitacionRechazada(string Campo, string Mensaje) : ResultadoInvitacion;

public abstract record ResultadoCambioEstado;

public sealed record EstadoActualizado(string NuevoEstado) : ResultadoCambioEstado;

public sealed record AvisoSinFirmantes : ResultadoCambioEstado;

public sealed record TransicionNoPermitida : ResultadoCambioEstado;

public abstract record ResultadoComentario;

public sealed record ComentarioPublicado(string Autor, DateTime FechaHora, string TextoComentario, string? FragmentoReferenciado)
    : ResultadoComentario;

public sealed record ComentarioRechazado(string Mensaje) : ResultadoComentario;

public sealed record ResultadoSolicitudes(
    List<ParteContrato> FirmantesNotificados, List<ParteContrato> FirmantesConError, int TotalFirmantes, string? Error = null);

public sealed record FirmaRegistrada(bool ContratoFirmado);

public sealed record EstadoFirmas(bool ExistenSolicitudes, List<ParteContrato> ListaFirmantes);

public abstract record ResultadoReenvio;

public sealed record SolicitudReenviada(ParteContrato Firmante, DateTime FechaHora) : ResultadoReenvio;

public sealed record ErrorReenvio(string EnlaceFirmante) : ResultadoReenvio;

public sealed record ReenvioRechazado(string Mensaje) : ResultadoReenvio;
