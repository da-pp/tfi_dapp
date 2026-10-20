using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Contratos;

public class DescargarPDFForm(ContratoBLL contratoBLL, AccesoBLL accesoBLL, PDFBLL pdfBLL, HoraLocal hora,
    ILogger<DescargarPDFForm> logger) : PageModel
{
    public const string MensajeNoFirmado =
        "El PDF completo solo está disponible una vez que todos los firmantes hayan completado la firma";
    public const string MensajeErrorGeneracion = "El PDF no pudo ser generado en este momento.";

    [BindProperty(SupportsGet = true)] public int? IdContrato { get; set; }
    [BindProperty(SupportsGet = true)] public string? Token { get; set; }

    public Contrato? DatosContrato { get; private set; }

    public bool EsTitular { get; private set; }
    public bool EsInterno => string.IsNullOrEmpty(Token);
    public bool Firmado => DatosContrato?.Estado == EstadosContrato.Firmado;

    private int? _usuarioAuditoria;
    private string? _detalleAuditoria;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!await VerificarAccesoYEstado(ct))
            return AccesoRechazado();
        return Page();
    }

    public async Task<IActionResult> OnGetArchivoAsync(bool sinFirmas, CancellationToken ct)
    {
        if (!await VerificarAccesoYEstado(ct))
            return AccesoRechazado();
        if (!sinFirmas && !Firmado)
            return StatusCode(StatusCodes.Status409Conflict, MensajeNoFirmado);

        ArchivoPDF archivoPDF;
        try
        {
            archivoPDF = sinFirmas
                ? await pdfBLL.GenerarPDFSinFirmas(DatosContrato!.IdContrato, DatosContrato.Nombre ?? "Contrato", ct)
                : await pdfBLL.GenerarPDFFirmado(DatosContrato!.IdContrato, DatosContrato.Nombre ?? "Contrato", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Error al generar el PDF del contrato {IdContrato}.", DatosContrato!.IdContrato);
            return StatusCode(StatusCodes.Status500InternalServerError, MensajeErrorGeneracion);
        }

        await contratoBLL.RegistrarDescarga(DatosContrato.IdContrato, _usuarioAuditoria, hora.AhoraUtc, archivoPDF.Version,
            $"{_detalleAuditoria} Versión v{archivoPDF.Version}{(sinFirmas ? " sin firmas" : " firmada")}.", ct);
        return File(archivoPDF.Contenido, "application/pdf", archivoPDF.NombreArchivo);
    }

    public string NombreArchivo => PDFBLL.GenerarArchivo(DatosContrato?.Nombre ?? "Contrato", DatosContrato?.NumeroVersion ?? 0, Firmado);

    private async Task<bool> VerificarAccesoYEstado(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(Token))
        {
            var token = new TokenEnlace(Token);
            var parte = await accesoBLL.ValidarToken(token, ct);
            if (parte is null)
                return false;
            DatosContrato = await contratoBLL.ObtenerContratoPorToken(token, ct);
            _usuarioAuditoria = parte.IdUsuario;
            _detalleAuditoria = $"Descarga de {parte.CorreoInvitado} mediante enlace tokenizado.";
            return DatosContrato is not null;
        }

        var usuario = UsuarioActual.Desde(User);
        if (usuario is null || IdContrato is null)
            return false;

        var (datosContrato, accesoUsuario) = await contratoBLL.VerificarAccesoYEstado(IdContrato.Value, usuario, ct);
        DatosContrato = datosContrato;
        EsTitular = datosContrato?.EsTitular(usuario.IdUsuario) == true;
        _usuarioAuditoria = usuario.IdUsuario;
        _detalleAuditoria = $"Descarga de {usuario.NombreCompleto}.";
        return accesoUsuario;
    }

    private IActionResult AccesoRechazado() =>
        string.IsNullOrEmpty(Token) && User.Identity?.IsAuthenticated != true
            ? Challenge()
            : StatusCode(StatusCodes.Status403Forbidden);
}
