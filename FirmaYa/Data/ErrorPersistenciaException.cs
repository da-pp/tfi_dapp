namespace FirmaYa.Data;

public sealed class ErrorPersistenciaException(Exception interna)
    : Exception("No se pudo completar la operación en la base de datos.", interna);
