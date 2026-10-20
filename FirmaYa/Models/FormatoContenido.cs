using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FirmaYa.Models;

public static partial class FormatoContenido
{
    // HTML seguro: primero se codifica todo el texto y recién después se aplican las marcas.
    public static string AHtml(string? contenido)
    {
        var html = new StringBuilder();
        foreach (var linea in (contenido ?? "").Replace("\r", "").Split('\n'))
        {
            if (linea.StartsWith("# "))
                html.Append("<h3>").Append(EnLinea(linea[2..])).Append("</h3>");
            else if (linea.StartsWith("- "))
                html.Append("<p class=\"contenido__item\">• ").Append(EnLinea(linea[2..])).Append("</p>");
            else if (linea.StartsWith("> "))
                html.Append("<blockquote>").Append(EnLinea(linea[2..])).Append("</blockquote>");
            else if (string.IsNullOrWhiteSpace(linea))
                html.Append("<p class=\"contenido__vacio\"></p>");
            else
                html.Append("<p>").Append(EnLinea(linea)).Append("</p>");
        }
        return html.ToString();
    }

    public static IEnumerable<(string Texto, bool EsTitulo)> ATexto(string? contenido)
    {
        foreach (var linea in (contenido ?? "").Replace("\r", "").Split('\n'))
        {
            if (linea.StartsWith("# "))
                yield return (Limpiar(linea[2..]), true);
            else if (linea.StartsWith("- "))
                yield return ("• " + Limpiar(linea[2..]), false);
            else if (linea.StartsWith("> "))
                yield return ("\"" + Limpiar(linea[2..]) + "\"", false);
            else
                yield return (Limpiar(linea), false);
        }
    }

    private static string EnLinea(string texto)
    {
        var codificado = WebUtility.HtmlEncode(texto);
        codificado = Negrita().Replace(codificado, "<strong>$1</strong>");
        return Cursiva().Replace(codificado, "<em>$1</em>");
    }

    private static string Limpiar(string texto) => Cursiva().Replace(Negrita().Replace(texto, "$1"), "$1");

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex Negrita();

    [GeneratedRegex(@"(?<![\w])_(.+?)_(?![\w])")]
    private static partial Regex Cursiva();
}
