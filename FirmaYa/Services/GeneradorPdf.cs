using System.Globalization;
using System.Text;

namespace FirmaYa.Services;

/// <summary>Línea de texto del PDF: estilo y separación previa.</summary>
public sealed record LineaPdf(string Texto, bool Negrita = false, double Tamano = 10, double EspacioAntes = 0);

/// <summary>
/// Generador mínimo de PDF 1.4 (A4, Helvetica, WinAnsiEncoding) sin librerías externas.
/// Ajusta el texto al ancho, pagina y escribe un pie de página en cada hoja. Lo usa PDFBLL.
/// </summary>
public static class GeneradorPdf
{
    private const double AnchoPagina = 595;
    private const double AltoPagina = 842;
    private const double Margen = 50;
    private const double MargenInferior = 70;

    public static byte[] Generar(IEnumerable<LineaPdf> lineas, Func<int, int, string[]> piePagina)
    {
        var paginas = Paginar(Ajustar(lineas));
        var total = paginas.Count;

        var objetos = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "", // 2: Pages, se completa al final
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
        };

        var idsPaginas = new List<int>();
        for (var i = 0; i < total; i++)
        {
            var contenido = ContenidoPagina(paginas[i], piePagina(i + 1, total));
            objetos.Add($"<< /Length {Latin1(contenido).Length} >>\nstream\n{contenido}\nendstream");
            var idContenido = objetos.Count;
            objetos.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {AnchoPagina} {AltoPagina}] " +
                        $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {idContenido} 0 R >>");
            idsPaginas.Add(objetos.Count);
        }
        objetos[1] = $"<< /Type /Pages /Kids [{string.Join(' ', idsPaginas.Select(id => $"{id} 0 R"))}] /Count {total} >>";

        using var salida = new MemoryStream();
        Escribir(salida, "%PDF-1.4\n%âãÏÓ\n");
        var posiciones = new List<long>();
        for (var i = 0; i < objetos.Count; i++)
        {
            posiciones.Add(salida.Position);
            Escribir(salida, $"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
        }

        var inicioXref = salida.Position;
        var xref = new StringBuilder($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var posicion in posiciones)
            xref.Append(posicion.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        xref.Append($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{inicioXref}\n%%EOF\n");
        Escribir(salida, xref.ToString());

        return salida.ToArray();
    }

    private static List<LineaPdf> Ajustar(IEnumerable<LineaPdf> lineas)
    {
        var resultado = new List<LineaPdf>();
        foreach (var linea in lineas)
        {
            // Aproximación conservadora del ancho medio de Helvetica.
            var maximo = (int)((AnchoPagina - 2 * Margen) / (linea.Tamano * (linea.Negrita ? 0.58 : 0.53)));
            var primera = true;
            foreach (var parrafo in linea.Texto.Replace("\r", "").Split('\n'))
            {
                var actual = new StringBuilder();
                foreach (var palabra in parrafo.Split(' '))
                {
                    if (actual.Length > 0 && actual.Length + 1 + palabra.Length > maximo)
                    {
                        resultado.Add(linea with { Texto = actual.ToString(), EspacioAntes = primera ? linea.EspacioAntes : 0 });
                        primera = false;
                        actual.Clear();
                    }
                    if (actual.Length > 0)
                        actual.Append(' ');
                    actual.Append(palabra);
                    while (actual.Length > maximo)
                    {
                        resultado.Add(linea with { Texto = actual.ToString(0, maximo), EspacioAntes = primera ? linea.EspacioAntes : 0 });
                        primera = false;
                        actual.Remove(0, maximo);
                    }
                }
                resultado.Add(linea with { Texto = actual.ToString(), EspacioAntes = primera ? linea.EspacioAntes : 0 });
                primera = false;
            }
        }
        return resultado;
    }

    private static List<List<(LineaPdf Linea, double Y)>> Paginar(List<LineaPdf> lineas)
    {
        var paginas = new List<List<(LineaPdf, double)>> { new() };
        var y = AltoPagina - Margen;
        foreach (var linea in lineas)
        {
            var alto = linea.Tamano * 1.45 + linea.EspacioAntes;
            if (y - alto < MargenInferior)
            {
                paginas.Add([]);
                y = AltoPagina - Margen;
                alto = linea.Tamano * 1.45;
            }
            y -= alto;
            paginas[^1].Add((linea, y));
        }
        return paginas;
    }

    private static string ContenidoPagina(List<(LineaPdf Linea, double Y)> lineas, string[] pie)
    {
        var contenido = new StringBuilder();
        foreach (var (linea, y) in lineas)
            contenido.Append(Texto(linea.Texto, linea.Negrita ? "F2" : "F1", linea.Tamano, Margen, y));

        contenido.Append($"0.8 w {F(Margen)} {F(MargenInferior - 12)} m {F(AnchoPagina - Margen)} {F(MargenInferior - 12)} l S\n");
        var yPie = MargenInferior - 24;
        foreach (var texto in pie)
        {
            contenido.Append(Texto(texto, "F1", 7, Margen, yPie));
            yPie -= 10;
        }
        return contenido.ToString();
    }

    private static string Texto(string texto, string fuente, double tamano, double x, double y) =>
        $"BT /{fuente} {F(tamano)} Tf {F(x)} {F(y)} Td ({Escapar(texto)}) Tj ET\n";

    private static string F(double valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Escapa la cadena PDF y reemplaza lo que no existe en WinAnsi (Latin-1) por "?".</summary>
    private static string Escapar(string texto)
    {
        var resultado = new StringBuilder(texto.Length);
        foreach (var c in texto)
        {
            resultado.Append(c switch
            {
                '\\' => "\\\\",
                '(' => "\\(",
                ')' => "\\)",
                '\t' => "    ",
                '“' or '”' => "\"",
                '‘' or '’' => "'",
                '–' or '—' => "-",
                '•' => "\u0095", // viñeta en WinAnsiEncoding
                _ when c < 32 => "",
                _ when c > 255 => "?",
                _ => c.ToString()
            });
        }
        return resultado.ToString();
    }

    private static byte[] Latin1(string texto) => Encoding.Latin1.GetBytes(texto);

    private static void Escribir(Stream salida, string texto) => salida.Write(Latin1(texto));
}
