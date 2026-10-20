using System.Text;
using FirmaYa.Data;
using FirmaYa.Models;
using FirmaYa.Models.Entidades;
using FirmaYa.Services;

namespace FirmaYa.BLL;

public sealed record ArchivoPDF(string NombreArchivo, byte[] Contenido, int Version);

public sealed class PDFBLL(IDAL dal, HoraLocal hora)
{
    public const string Leyenda = "Documento con firma digital verificada por FirmaYA";

    public async Task<ArchivoPDF> GenerarPDFFirmado(int idContrato, string nombreContrato, CancellationToken ct)
    {
        var contenidoVersionFinal = await ObtenerContenidoVersionFinal(idContrato, ct)
                                    ?? throw new InvalidOperationException("El contrato no tiene versiones.");
        var datosFirmas = await ObtenerDatosFirmas(idContrato, ct);

        var lineas = CompilarContenido(nombreContrato, contenidoVersionFinal);
        lineas.AddRange(IncorporarDatosFirma(datosFirmas));
        lineas.Add(new LineaPdf(Leyenda, Negrita: true, EspacioAntes: 12));

        var hashDocumentoCompleto = HashSha256.Calcular(string.Join("\n", lineas.Select(l => l.Texto)));
        var bytes = GeneradorPdf.Generar(lineas, IncorporarHashEnPiePagina(hashDocumentoCompleto));
        return new ArchivoPDF(GenerarArchivo(nombreContrato, contenidoVersionFinal.NumeroVersion, firmado: true), bytes,
            contenidoVersionFinal.NumeroVersion);
    }

    public async Task<ArchivoPDF> GenerarPDFSinFirmas(int idContrato, string nombreContrato, CancellationToken ct)
    {
        var contenidoVersionFinal = await ObtenerContenidoVersionFinal(idContrato, ct)
                                    ?? throw new InvalidOperationException("El contrato no tiene versiones.");
        var lineas = CompilarContenido(nombreContrato, contenidoVersionFinal);
        lineas.Add(new LineaPdf("Versión sin firmas: el documento aún no fue firmado por todas las partes.", EspacioAntes: 12));

        var hashDocumentoCompleto = HashSha256.Calcular(string.Join("\n", lineas.Select(l => l.Texto)));
        var bytes = GeneradorPdf.Generar(lineas, IncorporarHashEnPiePagina(hashDocumentoCompleto));
        return new ArchivoPDF(GenerarArchivo(nombreContrato, contenidoVersionFinal.NumeroVersion, firmado: false), bytes,
            contenidoVersionFinal.NumeroVersion);
    }

    public Task<VersionContrato?> ObtenerContenidoVersionFinal(int idContrato, CancellationToken ct) =>
        dal.ObtenerContenidoVersionFinal(idContrato, ct);

    public Task<List<DatosFirma>> ObtenerDatosFirmas(int idContrato, CancellationToken ct) => dal.ObtenerDatosFirmas(idContrato, ct);

    public List<LineaPdf> CompilarContenido(string nombreContrato, VersionContrato contenidoVersionFinal)
    {
        var lineas = new List<LineaPdf>
        {
            new(nombreContrato, Negrita: true, Tamano: 15),
            new($"Versión v{contenidoVersionFinal.NumeroVersion} · {hora.Formatear(contenidoVersionFinal.FechaCreacion)}", Tamano: 9, EspacioAntes: 4),
            new($"Hash de la versión: {contenidoVersionFinal.HashSha256}", Tamano: 8)
        };
        var primera = true;
        foreach (var (texto, esTitulo) in FormatoContenido.ATexto(contenidoVersionFinal.Contenido))
        {
            lineas.Add(new LineaPdf(texto, Negrita: esTitulo, Tamano: esTitulo ? 12 : 10, EspacioAntes: primera ? 16 : 0));
            primera = false;
        }
        return lineas;
    }

    public List<LineaPdf> IncorporarDatosFirma(List<DatosFirma> datosFirmas)
    {
        var lineas = new List<LineaPdf> { new("Datos de firma", Negrita: true, Tamano: 12, EspacioAntes: 20) };
        foreach (var firma in datosFirmas)
        {
            lineas.Add(new LineaPdf($"Firmante: {firma.NombreFirmante}", Negrita: true, EspacioAntes: 8));
            lineas.Add(new LineaPdf($"Fecha y hora de la firma: {hora.Formatear(firma.FechaHoraFirma)}"));
            lineas.Add(new LineaPdf($"Dirección IP del firmante: {firma.IpFirmante ?? "—"}"));
            lineas.Add(new LineaPdf($"Hash de la versión firmada: {firma.HashVersion}", Tamano: 9));
            if (!string.IsNullOrEmpty(firma.HashFirma))
                lineas.Add(new LineaPdf($"Hash de la firma: {firma.HashFirma}", Tamano: 9));
        }
        return lineas;
    }

    public static Func<int, int, string[]> IncorporarHashEnPiePagina(string hashDocumentoCompleto) =>
        (pagina, total) => [$"Hash del documento: {hashDocumentoCompleto}", $"{Leyenda} · Página {pagina} de {total}"];

    public static string GenerarArchivo(string nombreContrato, int version, bool firmado)
    {
        var nombre = new StringBuilder();
        foreach (var c in nombreContrato.Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
                nombre.Append(c);
            else if (char.IsWhiteSpace(c))
                nombre.Append('_');
        }
        var baseNombre = nombre.Length == 0 ? "Contrato" : nombre.ToString();
        return firmado ? $"{baseNombre}_v{version}_firmado.pdf" : $"{baseNombre}_v{version}.pdf";
    }
}
