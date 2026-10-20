using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Dashboard;

public class IndexModel(DashboardBLL dashboardBLL) : PageModel
{
    public List<Contrato> Contratos { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var usuario = UsuarioActual.Desde(User) ?? throw new AccesoDenegadoException();
        Contratos = await dashboardBLL.ObtenerContratos(usuario, ct);
    }
}
