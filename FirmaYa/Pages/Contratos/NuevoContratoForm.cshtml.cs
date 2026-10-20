using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Models.ViewModels;
using FirmaYa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Contratos;

public class NuevoContratoForm(PlantillaBLL plantillaBLL, ContratoBLL contratoBLL, HoraLocal hora) : PageModel
{
    public const string MensajeSinPlantillas = "No hay plantillas disponibles. Contacte al administrador del sistema.";

    public List<Plantilla> ListaPlantillas { get; private set; } = [];

    public Plantilla? DatosPlantilla { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? TipoPlantilla { get; set; }

    [BindProperty]
    public NuevoContratoViewModel Entrada { get; set; } = new();

    public Dictionary<string, string> Errores { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        ListaPlantillas = await plantillaBLL.ObtenerPlantillasActivas(ct);

        if (ListaPlantillas.Count == 0)
            return SinPlantillas();

        if (!string.IsNullOrWhiteSpace(TipoPlantilla))
            DatosPlantilla = await plantillaBLL.ObtenerPlantillaPorTipo(TipoPlantilla, ct);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        // Se vuelven a leer de la base: no se confía en la plantilla que envía el navegador.
        ListaPlantillas = await plantillaBLL.ObtenerPlantillasActivas(ct);
        if (ListaPlantillas.Count == 0)
            return SinPlantillas();
        if (!string.IsNullOrWhiteSpace(TipoPlantilla))
            DatosPlantilla = await plantillaBLL.ObtenerPlantillaPorTipo(TipoPlantilla, ct);

        Errores = Entrada.Validar(hora.Hoy);
        if (DatosPlantilla is null)
            Errores[nameof(TipoPlantilla)] = "Seleccione el tipo de plantilla";
        if (Errores.Count > 0)
            return Page();

        var usuario = UsuarioActual.Desde(User) ?? throw new AccesoDenegadoException();
        var contratoCreado = await contratoBLL.CrearContrato(Entrada.NombreContrato!.Trim(), Entrada.PartesInvolucradas!.Trim(),
            Entrada.FechaInicioValor, Entrada.FechaExpiracionValor, Entrada.DescripcionPropiedad?.Trim(), TipoPlantilla!,
            DatosPlantilla!, usuario, ct);

        TempData["Mensaje"] = "Contrato creado exitosamente";
        return RedirectToPage("/Contratos/EditarContratoForm", new { idContrato = contratoCreado.IdContrato });
    }

    public string? ErrorDe(string campo) => Errores.GetValueOrDefault(campo);

    public IEnumerable<string> TiposDisponibles =>
        TiposContrato.Todos.Where(t => ListaPlantillas.Any(p => p.TipoContrato == t))
            .Concat(ListaPlantillas.Select(p => p.TipoContrato!).Where(t => !TiposContrato.Todos.Contains(t)).Distinct());

    private RedirectToPageResult SinPlantillas()
    {
        TempData["Error"] = MensajeSinPlantillas;
        return RedirectToPage("/Dashboard/Index");
    }
}
