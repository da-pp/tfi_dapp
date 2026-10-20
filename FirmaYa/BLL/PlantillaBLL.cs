using FirmaYa.Data;
using FirmaYa.Models.Entidades;

namespace FirmaYa.BLL;

public sealed class PlantillaBLL(IDAL dal)
{
    public Task<List<Plantilla>> ObtenerPlantillasActivas(CancellationToken ct) => dal.ObtenerPlantillasActivas(ct);

    public Task<Plantilla?> ObtenerPlantillaPorTipo(string tipoPlantilla, CancellationToken ct) =>
        dal.ObtenerPlantillaPorTipo(tipoPlantilla, ct);
}
