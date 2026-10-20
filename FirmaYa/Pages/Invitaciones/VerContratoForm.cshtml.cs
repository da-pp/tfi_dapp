using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using FirmaYa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Invitaciones;

public class VerContratoForm(AccesoBLL accesoBLL, ContratoBLL contratoBLL, HoraLocal hora) : PageModel
{
    public const string MensajeTokenInvalido =
        "El enlace de acceso no es válido o ha expirado. Solicite un nuevo enlace al dueño del contrato.";
    public const string MensajeNoDisponible = "Este contrato ya no está disponible.";

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = "";

    public ParteContrato? DatosToken { get; private set; }

    public Contrato? DatosContrato { get; private set; }

    public string? Error { get; private set; }

    public bool PuedeFirmar => DatosToken?.RolParte == RolesParte.Firmante
                               && DatosContrato?.Estado == EstadosContrato.ListoParaFirmar
                               && DatosToken.EstadoFirma != EstadosFirma.Firmado;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var token = new TokenEnlace(Token);

        DatosToken = await accesoBLL.ValidarToken(token, ct);
        if (DatosToken is null)
        {
            Error = MensajeTokenInvalido;
            return Page();
        }

        DatosContrato = await contratoBLL.ObtenerContratoPorToken(token, ct);
        if (DatosContrato is null || DatosContrato.Estado == EstadosContrato.Archivado)
        {
            DatosContrato = null;
            Error = MensajeNoDisponible;
            return Page();
        }

        await accesoBLL.RegistrarAcceso(token, hora.AhoraUtc, ContratoPageModel.DireccionIpDe(HttpContext), ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRenovarAsync(CancellationToken ct) =>
        new JsonResult(new { renovado = await accesoBLL.RenovarToken(new TokenEnlace(Token), ct) });
}
