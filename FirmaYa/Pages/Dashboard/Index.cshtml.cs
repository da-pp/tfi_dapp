using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Dashboard;

/// <summary>
/// Panel principal de Abogado y Agente Inmobiliario (mapa de navegación: "Dashboard, lista contratos").
/// Destino del CU-19 y punto de entrada a los CU-01 a CU-10. El CU-22 no está especificado en el documento.
/// </summary>
public class IndexModel(DashboardBLL dashboardBLL) : PageModel
{
    public List<Contrato> Contratos { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var usuario = UsuarioActual.Desde(User) ?? throw new AccesoDenegadoException();
        Contratos = await dashboardBLL.ObtenerContratos(usuario, ct);
    }
}
