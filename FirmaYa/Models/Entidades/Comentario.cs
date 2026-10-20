namespace FirmaYa.Models.Entidades;

/// <summary>Tabla comentario del DER, con el nombre del autor (usuario).</summary>
public sealed class Comentario
{
    public int IdComentario { get; set; }
    public int IdContrato { get; set; }
    public int? IdUsuario { get; set; }
    public string? NombreAutor { get; set; }
    public string Texto { get; set; } = "";
    public DateTime FechaPublicacion { get; set; }
}
