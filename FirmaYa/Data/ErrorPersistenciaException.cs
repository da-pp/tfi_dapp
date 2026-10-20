namespace FirmaYa.Data;

/// <summary>
/// Falla de acceso a la base de datos. Oculta el detalle del proveedor a las capas superiores;
/// el error original queda en InnerException y en el log.
/// </summary>
public sealed class ErrorPersistenciaException(Exception interna)
    : Exception("No se pudo completar la operación en la base de datos.", interna);
