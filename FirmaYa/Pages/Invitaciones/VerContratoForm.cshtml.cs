using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using FirmaYa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Invitaciones;

/// <summary>Participante "VerContratoForm" del diagrama de secuencia del CU-04 Ver contrato como parte invitada.</summary>
public class VerContratoForm(AccesoBLL accesoBLL, ContratoBLL contratoBLL, HoraLocal hora) : PageModel
{
    public const string MensajeTokenInvalido =
        "El enlace de acceso no es válido o ha expirado. Solicite un nuevo enlace al dueño del contrato.";
    public const string MensajeNoDisponible = "Este contrato ya no está disponible.";

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = "";

    /// <summary>datosToken: la parte invitada (rol asignado).</summary>
    public ParteContrato? DatosToken { get; private set; }

    /// <summary>datosContrato</summary>
    public Contrato? DatosContrato { get; private set; }

    public string? Error { get; private set; }

    /// <summary>alt [Rol es Firmante y estado es "Listo para firmar"] → botón "Firmar contrato".</summary>
    public bool PuedeFirmar => DatosToken?.RolParte == RolesParte.Firmante
                               && DatosContrato?.Estado == EstadosContrato.ListoParaFirmar
                               && DatosToken.EstadoFirma != EstadosFirma.Firmado;

    /// <summary>clic en el enlace de invitación (token)</summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var token = new TokenEnlace(Token);

        // validarToken(token)
        DatosToken = await accesoBLL.ValidarToken(token, ct);
        if (DatosToken is null)
        {
            Error = MensajeTokenInvalido;
            return Page();
        }

        // obtenerContratoPorToken(token) — alt [Contrato existe y está activo]
        DatosContrato = await contratoBLL.ObtenerContratoPorToken(token, ct);
        if (DatosContrato is null || DatosContrato.Estado == EstadosContrato.Archivado)
        {
            DatosContrato = null;
            Error = MensajeNoDisponible;
            return Page();
        }

        // registrarAcceso(token, fecha, hora, ip)
        await accesoBLL.RegistrarAcceso(token, hora.AhoraUtc, ContratoPageModel.DireccionIpDe(HttpContext), ct);
        return Page();
    }

    /// <summary>alt [Usuario clic "Continuar"] → renovarToken(token) → tokenRenovado</summary>
    public async Task<IActionResult> OnPostRenovarAsync(CancellationToken ct) =>
        new JsonResult(new { renovado = await accesoBLL.RenovarToken(new TokenEnlace(Token), ct) });
}
