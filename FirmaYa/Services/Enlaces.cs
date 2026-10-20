using Microsoft.Extensions.Options;

namespace FirmaYa.Services;

/// <summary>URLs absolutas de los enlaces tokenizados que se envían por correo (CU-03, CU-07, CU-09).</summary>
public sealed class Enlaces(IOptions<AplicacionOptions> opciones, IHttpContextAccessor contexto)
{
    /// <summary>Enlace de acceso de la parte invitada (CU-04).</summary>
    public string Acceso(string token) => $"{Base()}/Invitado/{Uri.EscapeDataString(token)}";

    /// <summary>Enlace de firma (CU-08).</summary>
    public string Firma(string token) => $"{Base()}/Firmar/{Uri.EscapeDataString(token)}";

    private string Base()
    {
        if (!string.IsNullOrWhiteSpace(opciones.Value.UrlBase))
            return opciones.Value.UrlBase.TrimEnd('/');

        var request = contexto.HttpContext?.Request
            ?? throw new InvalidOperationException("Configurar Aplicacion:UrlBase para generar enlaces fuera de un request.");
        return $"{request.Scheme}://{request.Host}{request.PathBase}";
    }
}
