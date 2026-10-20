using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace FirmaYa.Pages.Firmas;

/// <summary>Participante "EstadoFirmasForm" del diagrama de secuencia del CU-09 Consultar estado de firmas pendientes.</summary>
public class EstadoFirmasForm(ContratoBLL contratoBLL) : ContratoPageModel
{
    public const string MensajeSinSolicitudes = "Aún no se han enviado solicitudes de firma para este contrato.";
    public const string MensajeTodasFirmadas = "Todas las firmas han sido completadas. El contrato está firmado.";
    public const string MensajeErrorReenvio = "No se pudo reenviar la solicitud. ¿Desea reintentar o copiar el enlace manualmente?";

    public EstadoFirmas? Estado { get; private set; }

    /// <summary>errorReenvio(enlaceFirmante)</summary>
    public ErrorReenvio? ErrorReenvio { get; private set; }

    [BindProperty]
    public int Firmante { get; set; }

    public int FirmasCompletadas => Estado?.ListaFirmantes.Count(f => f.EstadoFirma == EstadosFirma.Firmado) ?? 0;
    public bool TodasFirmadas => Estado is { ListaFirmantes.Count: > 0 } && FirmasCompletadas == Estado.ListaFirmantes.Count;

    /// <summary>clic en pestaña "Estado de Firmas" → obtenerEstadoFirmas(idContrato)</summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await Cargar(ct);
        return Page();
    }

    /// <summary>"Confirmar" el reenvío (o "Reintentar") → reenviarSolicitudFirma(idContrato, firmante)</summary>
    public async Task<IActionResult> OnPostReenviarAsync(CancellationToken ct)
    {
        switch (await contratoBLL.ReenviarSolicitudFirma(IdContrato, Firmante, Usuario, ct))
        {
            case SolicitudReenviada:
                TempData["Mensaje"] = "Solicitud reenviada exitosamente";
                return RedirectToPage(new { IdContrato });
            case ErrorReenvio error:
                ErrorReenvio = error;
                break;
            case ReenvioRechazado rechazado:
                TempData["Error"] = rechazado.Mensaje;
                return RedirectToPage(new { IdContrato });
        }
        await Cargar(ct);
        return Page();
    }

    private async Task Cargar(CancellationToken ct)
    {
        Estado = await contratoBLL.ObtenerEstadoFirmas(IdContrato, Usuario, ct);
        Contrato = await contratoBLL.ObtenerContrato(IdContrato, Usuario, ct);
    }
}
