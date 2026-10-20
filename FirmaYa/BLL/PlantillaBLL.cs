using FirmaYa.Data;
using FirmaYa.Models.Entidades;

namespace FirmaYa.BLL;

/// <summary>Participante ":PlantillaBLL" del CU-01 — clase Plantilla del diagrama de clases.</summary>
public sealed class PlantillaBLL(IDAL dal)
{
    /// <summary>obtenerPlantillasActivas() → listaPlantillas</summary>
    public Task<List<Plantilla>> ObtenerPlantillasActivas(CancellationToken ct) => dal.ObtenerPlantillasActivas(ct);

    /// <summary>obtenerPlantillaPorTipo(tipoPlantilla) → datosPlantilla</summary>
    public Task<Plantilla?> ObtenerPlantillaPorTipo(string tipoPlantilla, CancellationToken ct) =>
        dal.ObtenerPlantillaPorTipo(tipoPlantilla, ct);
}
