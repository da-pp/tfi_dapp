using Microsoft.Extensions.Options;

namespace FirmaYa.Services;

public sealed class Enlaces(IOptions<AplicacionOptions> opciones, IHttpContextAccessor contexto)
{
    public string Acceso(string token) => $"{Base()}/Invitado/{Uri.EscapeDataString(token)}";

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
