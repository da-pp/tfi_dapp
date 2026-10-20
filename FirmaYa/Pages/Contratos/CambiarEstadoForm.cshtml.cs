using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace FirmaYa.Pages.Contratos;

public class CambiarEstadoForm(ContratoBLL contratoBLL) : ContratoPageModel
{
    public const string MensajeTransicionNoPermitida = "Esta transición de estado no es posible. Verifique el flujo permitido.";
    public const string MensajeSinFirmantes = "El contrato no tiene firmantes asignados. ¿Desea continuar o ir a invitar partes?";

    public static readonly (string Origen, string Destino)[] FlujoPermitido =
    [
        (EstadosContrato.Borrador, EstadosContrato.EnRevision),
        (EstadosContrato.EnRevision, EstadosContrato.ListoParaFirmar),
        (EstadosContrato.Firmado, EstadosContrato.Archivado)
    ];

    public static readonly Dictionary<string, string> Descripciones = new()
    {
        [EstadosContrato.EnRevision] = "El contrato sigue siendo editable y las partes pueden revisarlo y comentarlo.",
        [EstadosContrato.ListoParaFirmar] = "El contenido deja de ser editable y se pueden solicitar las firmas de las partes.",
        [EstadosContrato.Archivado] = "El contrato queda solo para consulta: no admite invitaciones ni comentarios.",
    };

    [BindProperty] public string? EstadoActual { get; set; }
    [BindProperty] public string? NuevoEstado { get; set; }
    [BindProperty] public string? RazonCambio { get; set; }

    public string? Error { get; private set; }
    public bool MostrarAvisoSinFirmantes { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Contrato = await contratoBLL.ObtenerEstadoYTransicionesDisponibles(IdContrato, Usuario, ct);
        EstadoActual = Contrato.Estado;
        return Page();
    }

    public Task<IActionResult> OnPostAsync(CancellationToken ct) =>
        Procesar((estadoActual, nuevoEstado) =>
            contratoBLL.CambiarEstado(IdContrato, estadoActual, nuevoEstado, RazonCambio, Usuario, ct), ct);

    public Task<IActionResult> OnPostConfirmarAsync(CancellationToken ct) =>
        Procesar((estadoActual, nuevoEstado) =>
            contratoBLL.ConfirmarCambioEstado(IdContrato, estadoActual, nuevoEstado, RazonCambio, Usuario, ct), ct);

    private async Task<IActionResult> Procesar(Func<string, string, Task<ResultadoCambioEstado>> cambiar, CancellationToken ct)
    {
        Contrato = await contratoBLL.ObtenerEstadoYTransicionesDisponibles(IdContrato, Usuario, ct);

        if (string.IsNullOrWhiteSpace(NuevoEstado))
        {
            Error = "Seleccione el nuevo estado.";
            return Page();
        }
        if (RazonCambio?.Length > 500)
        {
            Error = "La razón del cambio admite hasta 500 caracteres.";
            return Page();
        }

        switch (await cambiar(EstadoActual ?? "", NuevoEstado))
        {
            case EstadoActualizado:
                TempData["Mensaje"] = "Estado actualizado exitosamente";
                return RedirectToPage(new { IdContrato });
            case AvisoSinFirmantes:
                MostrarAvisoSinFirmantes = true;
                return Page();
            default:
                Error = MensajeTransicionNoPermitida;
                return Page();
        }
    }
}
