using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace FirmaYa.Pages.Contratos;

/// <summary>Participante "EditarContratoForm" del diagrama de secuencia del CU-02 Editar contrato en línea.</summary>
public class EditarContratoForm(ContratoBLL contratoBLL) : ContratoPageModel
{
    public const string MensajeNoEditable = "Este contrato no puede ser editado en su estado actual.";
    public const string MensajeContenidoInsuficiente = "El contenido del contrato debe tener al menos 100 caracteres";

    [BindProperty]
    public string? ContenidoNuevo { get; set; }

    [BindProperty]
    public string? ComentarioVersion { get; set; }

    /// <summary>"Ver Contrato": modo solo lectura.</summary>
    [BindProperty(SupportsGet = true)]
    public bool SoloLectura { get; set; }

    /// <summary>Número de versión recién guardada: el editor descarta el borrador local.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Guardado { get; set; }

    public string? ErrorContenido { get; private set; }
    public string? ErrorComentario { get; private set; }
    public string? ErrorGeneral { get; private set; }

    /// <summary>alt [Contrato en estado Borrador o En Revisión] y el usuario es el titular.</summary>
    public bool Editable => Contrato is not null && EsTitular && EstadosContrato.EsEditable(Contrato.Estado);

    /// <summary>clic "Editar" → obtenerContrato(idContrato)</summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Contrato = await contratoBLL.ObtenerContrato(IdContrato, Usuario, ct) ?? throw new AccesoDenegadoException();
        ContenidoNuevo = Contrato.ContenidoActual;
        if (!EsTitular)
            SoloLectura = true;
        return Page();
    }

    /// <summary>clic "Guardar Versión" → guardarNuevaVersion(idContrato, contenidoNuevo, comentarioVersion, autor)</summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Contrato = await contratoBLL.ObtenerContrato(IdContrato, Usuario, ct) ?? throw new AccesoDenegadoException();

        if (ComentarioVersion?.Length > 500)
        {
            ErrorComentario = "Longitud máxima 500 caracteres";
            return Page();
        }

        switch (await contratoBLL.GuardarNuevaVersion(IdContrato, ContenidoNuevo, ComentarioVersion, Usuario, ct))
        {
            case VersionGuardada guardada:
                TempData["Mensaje"] = $"Versión guardada exitosamente: v{guardada.NumeroVersion} · hash {guardada.Hash}";
                return RedirectToPage(new { IdContrato, guardado = guardada.NumeroVersion });
            case ContenidoInsuficiente:
                ErrorContenido = MensajeContenidoInsuficiente;
                return Page();
            case VersionEnConflicto:
                ErrorGeneral = "Otra versión se guardó al mismo tiempo. Copie sus cambios, recargue la página e intente nuevamente.";
                return Page();
            default:
                return RedirectToPage(new { IdContrato });
        }
    }
}
