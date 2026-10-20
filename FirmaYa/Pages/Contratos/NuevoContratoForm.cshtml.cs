using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Models.ViewModels;
using FirmaYa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Contratos;

/// <summary>Participante "NuevoContratoForm" del diagrama de secuencia del CU-01 Crear contrato desde plantilla.</summary>
public class NuevoContratoForm(PlantillaBLL plantillaBLL, ContratoBLL contratoBLL, HoraLocal hora) : PageModel
{
    public const string MensajeSinPlantillas = "No hay plantillas disponibles. Contacte al administrador del sistema.";

    /// <summary>listaPlantillas</summary>
    public List<Plantilla> ListaPlantillas { get; private set; } = [];

    /// <summary>datosPlantilla del tipo seleccionado.</summary>
    public Plantilla? DatosPlantilla { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string? TipoPlantilla { get; set; }

    [BindProperty]
    public NuevoContratoViewModel Entrada { get; set; } = new();

    public Dictionary<string, string> Errores { get; private set; } = [];

    /// <summary>Usuario: clic "Nuevo Contrato" y, después, seleccionar tipoPlantilla.</summary>
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        // NuevoContratoForm → PlantillaBLL: obtenerPlantillasActivas() → listaPlantillas
        ListaPlantillas = await plantillaBLL.ObtenerPlantillasActivas(ct);

        // alt [No hay plantillas activas]
        if (ListaPlantillas.Count == 0)
            return SinPlantillas();

        // alt [Hay plantillas activas] — seleccionar tipoPlantilla:
        // NuevoContratoForm → PlantillaBLL: obtenerPlantillaPorTipo(tipoPlantilla) → datosPlantilla
        if (!string.IsNullOrWhiteSpace(TipoPlantilla))
            DatosPlantilla = await plantillaBLL.ObtenerPlantillaPorTipo(TipoPlantilla, ct);

        // mostrar formulario precargado (nombreContrato, partesInvolucradas, fechaInicio, fechaExpiracion, descripcionPropiedad)
        return Page();
    }

    /// <summary>Usuario: clic "Crear Contrato".</summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        // Se vuelven a leer de la base: no se confía en la plantilla que envía el navegador.
        ListaPlantillas = await plantillaBLL.ObtenerPlantillasActivas(ct);
        if (ListaPlantillas.Count == 0)
            return SinPlantillas();
        if (!string.IsNullOrWhiteSpace(TipoPlantilla))
            DatosPlantilla = await plantillaBLL.ObtenerPlantillaPorTipo(TipoPlantilla, ct);

        // alt [Campos obligatorios completos y formato de fecha válido] — paso 16.
        Errores = Entrada.Validar(hora.Hoy);
        if (DatosPlantilla is null)
            Errores[nameof(TipoPlantilla)] = "Seleccione el tipo de plantilla";
        if (Errores.Count > 0)
            return Page();

        // NuevoContratoForm → ContratoBLL: crearContrato(nombreContrato, partesInvolucradas, fechaInicio, fechaExpiracion,
        //     descripcionPropiedad, tipoPlantilla, datosPlantilla, usuarioCreador) → contratoCreado(idContrato, version=1, hash, estado)
        var usuario = UsuarioActual.Desde(User) ?? throw new AccesoDenegadoException();
        var contratoCreado = await contratoBLL.CrearContrato(Entrada.NombreContrato!.Trim(), Entrada.PartesInvolucradas!.Trim(),
            Entrada.FechaInicioValor, Entrada.FechaExpiracionValor, Entrada.DescripcionPropiedad?.Trim(), TipoPlantilla!,
            DatosPlantilla!, usuario, ct);

        // mostrar "Contrato creado exitosamente" y redirigir al editor
        TempData["Mensaje"] = "Contrato creado exitosamente";
        return RedirectToPage("/Contratos/EditarContratoForm", new { idContrato = contratoCreado.IdContrato });
    }

    public string? ErrorDe(string campo) => Errores.GetValueOrDefault(campo);

    /// <summary>Tipos de contrato con al menos una plantilla activa, en el orden del CU-01 paso 3.</summary>
    public IEnumerable<string> TiposDisponibles =>
        TiposContrato.Todos.Where(t => ListaPlantillas.Any(p => p.TipoContrato == t))
            .Concat(ListaPlantillas.Select(p => p.TipoContrato!).Where(t => !TiposContrato.Todos.Contains(t)).Distinct());

    // alt [No hay plantillas activas]: mostrar "No hay plantillas disponibles…" y redirigir al panel de control.
    private RedirectToPageResult SinPlantillas()
    {
        TempData["Error"] = MensajeSinPlantillas;
        return RedirectToPage("/Dashboard/Index");
    }
}
