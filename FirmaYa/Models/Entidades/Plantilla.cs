namespace FirmaYa.Models.Entidades;

/// <summary>Tabla plantilla del DER. Es el "datosPlantilla" del CU-01.</summary>
public sealed class Plantilla
{
    public int IdPlantilla { get; set; }
    public string? Nombre { get; set; }
    public string? TipoContrato { get; set; }
    public string? DescripcionUso { get; set; }
    public string? Cuerpo { get; set; }
    public string? Estado { get; set; }
    public int Version { get; set; }
}

public static class EstadosPlantilla
{
    public const string Activa = "Activa";
    public const string Inactiva = "Inactiva";
}

/// <summary>Valores de plantilla.tipo_contrato (CU-01 paso 3, CU-16 paso 6).</summary>
public static class TiposContrato
{
    public static readonly string[] Todos = ["Arrendamiento", "Venta", "Mandato", "Otro"];
}
