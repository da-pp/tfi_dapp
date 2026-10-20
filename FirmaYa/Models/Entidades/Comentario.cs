namespace FirmaYa.Models.Entidades;

public sealed class Comentario
{
    public int IdComentario { get; set; }
    public int IdContrato { get; set; }
    public int? IdUsuario { get; set; }
    public string? NombreAutor { get; set; }
    public string Texto { get; set; } = "";
    public DateTime FechaPublicacion { get; set; }
}
