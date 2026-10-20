using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace FirmaYa.Pages.Contratos;

public class ComentariosForm(ContratoBLL contratoBLL) : ContratoPageModel
{
    public const string MensajeSoloLectura = "No puede añadir comentarios con su rol actual.";

    public string? RolUsuario { get; private set; }

    public List<Comentario> Comentarios { get; private set; } = [];

    [BindProperty] public string? TextoComentario { get; set; }
    [BindProperty] public string? FragmentoReferenciado { get; set; }

    public string? Error { get; private set; }

    public bool PuedeComentar => ContratoBLL.RolPermiteComentar(RolUsuario) && Contrato?.Estado != EstadosContrato.Archivado;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await Cargar(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        switch (await contratoBLL.PublicarComentario(IdContrato, Usuario, TextoComentario, FragmentoReferenciado, ct))
        {
            case ComentarioPublicado:
                TempData["Mensaje"] = "Comentario publicado con éxito";
                return RedirectToPage(new { IdContrato });
            case ComentarioRechazado rechazado:
                Error = rechazado.Mensaje;
                break;
        }
        await Cargar(ct);
        return Page();
    }

    private async Task Cargar(CancellationToken ct)
    {
        RolUsuario = await contratoBLL.ObtenerRolUsuario(IdContrato, Usuario, ct) ?? throw new AccesoDenegadoException();
        Contrato = await contratoBLL.ObtenerContrato(IdContrato, Usuario, ct) ?? throw new AccesoDenegadoException();
        Comentarios = await contratoBLL.ObtenerComentarios(IdContrato, Usuario, ct);
    }
}
