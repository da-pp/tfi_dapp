using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Models.ViewModels;
using FirmaYa.Pages.Shared;
using FirmaYa.Services;
using Microsoft.AspNetCore.Mvc;

namespace FirmaYa.Pages.Firmas;

public class SolicitarFirmasForm(ContratoBLL contratoBLL, HoraLocal hora) : ContratoPageModel
{
    public const string MensajeSinFirmantes =
        "No hay firmantes asignados. Debe invitar al menos a una parte con el rol de Firmante antes de solicitar firmas.";

    public List<ParteContrato> ListaFirmantes { get; private set; } = [];

    [BindProperty] public string? CanalNotificacion { get; set; }
    [BindProperty] public string? MensajePersonalizado { get; set; }
    [BindProperty] public string? FechaLimiteFirma { get; set; }
    [BindProperty] public List<int> FirmantesConError { get; set; } = [];

    public Dictionary<string, string> Errores { get; private set; } = [];

    public ResultadoSolicitudes? Resultado { get; private set; }

    public int FirmasCompletadas => ListaFirmantes.Count(f => f.EstadoFirma == EstadosFirma.Firmado);

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await Cargar(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await Cargar(ct);

        if (CanalNotificacion != CanalesNotificacion.CorreoElectronico)
            Errores[nameof(CanalNotificacion)] = "Seleccione un canal de notificación";
        DateOnly? fechaLimite = null;
        if (!string.IsNullOrWhiteSpace(FechaLimiteFirma))
        {
            if (!NuevoContratoViewModel.TryFecha(FechaLimiteFirma, out var fecha) || fecha <= hora.Hoy)
                Errores[nameof(FechaLimiteFirma)] = "La fecha límite debe ser posterior a la fecha actual";
            else
                fechaLimite = fecha;
        }
        if (MensajePersonalizado?.Length > 500)
            Errores[nameof(MensajePersonalizado)] = "Longitud máxima 500 caracteres";
        if (Errores.Count > 0)
            return Page();

        return await Responder(await contratoBLL.SolicitarFirmas(IdContrato, CanalNotificacion, MensajePersonalizado, fechaLimite,
            Usuario, ct), ct);
    }

    public async Task<IActionResult> OnPostReintentarAsync(CancellationToken ct)
    {
        await Cargar(ct);
        return await Responder(await contratoBLL.ReintentarEnviosFallidos(IdContrato, FirmantesConError,
            CanalNotificacion ?? CanalesNotificacion.CorreoElectronico, MensajePersonalizado, Usuario, ct), ct);
    }

    public string EnlaceFirma(ParteContrato firmante) => contratoBLL.GenerarEnlaceFirma(IdContrato, firmante);

    public string? ErrorDe(string campo) => Errores.GetValueOrDefault(campo);

    private async Task<IActionResult> Responder(ResultadoSolicitudes resultado, CancellationToken ct)
    {
        if (resultado.Error is not null)
        {
            Errores[""] = resultado.Error;
            return Page();
        }
        if (resultado.FirmantesConError.Count == 0)
        {
            TempData["Mensaje"] = "Solicitudes de firma enviadas exitosamente";
            return RedirectToPage(new { IdContrato });
        }

        Resultado = resultado;
        await Cargar(ct);
        return Page();
    }

    private async Task Cargar(CancellationToken ct)
    {
        ListaFirmantes = await contratoBLL.ObtenerFirmantes(IdContrato, Usuario, ct);
        Contrato = await contratoBLL.ObtenerContrato(IdContrato, Usuario, ct);
    }
}
