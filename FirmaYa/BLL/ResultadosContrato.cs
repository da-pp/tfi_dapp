using FirmaYa.Models.Entidades;

namespace FirmaYa.BLL;

// Mensajes de retorno de :ContratoBLL hacia los formularios, con el nombre que tienen en los diagramas de secuencia.

/// <summary>El usuario no tiene permisos sobre el contrato (o el contrato no existe). Se responde 403.</summary>
public sealed class AccesoDenegadoException() : Exception("El usuario no tiene permisos sobre el contrato.");

// ---- CU-01 ----

/// <summary>contratoCreado(idContrato, version=1, hash, estado="Borrador")</summary>
public sealed record ContratoCreado(int IdContrato, int Version, string Hash, string Estado);

// ---- CU-02 ----

public abstract record ResultadoGuardarVersion;

/// <summary>versionGuardada(numeroVersion, hash)</summary>
public sealed record VersionGuardada(int NumeroVersion, string Hash) : ResultadoGuardarVersion;

/// <summary>alt [Contenido vacío o con menos de 100 caracteres]</summary>
public sealed record ContenidoInsuficiente : ResultadoGuardarVersion;

/// <summary>alt [Contrato en estado Firmado o Archivado]</summary>
public sealed record ContratoNoEditable : ResultadoGuardarVersion;

/// <summary>Otra versión con el mismo número se guardó al mismo tiempo.</summary>
public sealed record VersionEnConflicto : ResultadoGuardarVersion;

// ---- CU-03 ----

/// <summary>resultadoValidacion de validarCorreoFormatoYDuplicado</summary>
public enum ResultadoValidacionCorreo
{
    Valido,
    FormatoInvalido,
    YaInvitado
}

public abstract record ResultadoInvitacion;

/// <summary>parteInvitada(correoElectronico, estado="Invitación enviada")</summary>
public sealed record ParteInvitada(string CorreoElectronico, string Estado) : ResultadoInvitacion;

/// <summary>errorEnvioCorreo(enlaceTokenizado)</summary>
public sealed record ErrorEnvioCorreo(string EnlaceTokenizado, string CorreoElectronico) : ResultadoInvitacion;

/// <summary>alt [Formato de correo inválido] / [Correo ya invitado al contrato] / precondiciones.</summary>
public sealed record InvitacionRechazada(string Campo, string Mensaje) : ResultadoInvitacion;

// ---- CU-05 ----

public abstract record ResultadoCambioEstado;

/// <summary>estadoActualizado(nuevoEstado)</summary>
public sealed record EstadoActualizado(string NuevoEstado) : ResultadoCambioEstado;

/// <summary>avisoSinFirmantes</summary>
public sealed record AvisoSinFirmantes : ResultadoCambioEstado;

/// <summary>alt [Transición de estado no permitida]</summary>
public sealed record TransicionNoPermitida : ResultadoCambioEstado;

// ---- CU-06 ----

public abstract record ResultadoComentario;

/// <summary>comentarioPublicado(autor, fecha, hora, textoComentario, fragmentoReferenciado)</summary>
public sealed record ComentarioPublicado(string Autor, DateTime FechaHora, string TextoComentario, string? FragmentoReferenciado)
    : ResultadoComentario;

/// <summary>alt [textoComentario vacío] / [Rol es Solo lectura] / contrato archivado.</summary>
public sealed record ComentarioRechazado(string Mensaje) : ResultadoComentario;

// ---- CU-07 ----

/// <summary>resultadoSolicitudes(firmantesNotificados, firmantesConError, totalFirmantes)</summary>
public sealed record ResultadoSolicitudes(
    List<ParteContrato> FirmantesNotificados, List<ParteContrato> FirmantesConError, int TotalFirmantes, string? Error = null);

// ---- CU-08 ----

/// <summary>firmaRegistrada</summary>
public sealed record FirmaRegistrada(bool ContratoFirmado);

// ---- CU-09 ----

/// <summary>listaFirmantes(nombre, correoElectronico, estadoFirma, fechaHoraEvento, hashFirma, ip)</summary>
public sealed record EstadoFirmas(bool ExistenSolicitudes, List<ParteContrato> ListaFirmantes);

public abstract record ResultadoReenvio;

/// <summary>solicitudReenviada(firmante, fecha, hora)</summary>
public sealed record SolicitudReenviada(ParteContrato Firmante, DateTime FechaHora) : ResultadoReenvio;

/// <summary>errorReenvio(enlaceFirmante)</summary>
public sealed record ErrorReenvio(string EnlaceFirmante) : ResultadoReenvio;

public sealed record ReenvioRechazado(string Mensaje) : ResultadoReenvio;
