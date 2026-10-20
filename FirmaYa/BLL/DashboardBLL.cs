using FirmaYa.Data;
using FirmaYa.Models.Entidades;

namespace FirmaYa.BLL;

/// <summary>
/// Panel principal (mapa de navegación: "Dashboard, lista contratos"). El CU-22 no tiene caso de uso ni diagramas
/// en el documento FirmaYA-145-213; por ahora solo lista los contratos del usuario y da acceso a los CU-01 a CU-10.
/// </summary>
public sealed class DashboardBLL(IDAL dal)
{
    public Task<List<Contrato>> ObtenerContratos(UsuarioActual usuario, CancellationToken ct) =>
        dal.ObtenerContratosPorUsuario(usuario.IdUsuario, ct);
}
