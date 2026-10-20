namespace FirmaYa.Models.Entidades;

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

public static class TiposContrato
{
    public static readonly string[] Todos = ["Arrendamiento", "Venta", "Mandato", "Otro"];
}
