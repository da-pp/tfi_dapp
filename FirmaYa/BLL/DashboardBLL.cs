using FirmaYa.Data;
using FirmaYa.Models.Entidades;

namespace FirmaYa.BLL;

public sealed class DashboardBLL(IDAL dal)
{
    public Task<List<Contrato>> ObtenerContratos(UsuarioActual usuario, CancellationToken ct) =>
        dal.ObtenerContratosPorUsuario(usuario.IdUsuario, ct);
}
