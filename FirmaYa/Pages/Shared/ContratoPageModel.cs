using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Shared;

public abstract class ContratoPageModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int IdContrato { get; set; }

    public Contrato? Contrato { get; protected set; }

    protected UsuarioActual Usuario => UsuarioActual.Desde(User) ?? throw new AccesoDenegadoException();

    public bool EsTitular => Contrato?.EsTitular(Usuario.IdUsuario) == true;

    protected string? DireccionIp() => DireccionIpDe(HttpContext);

    public static string? DireccionIpDe(HttpContext contexto)
    {
        var ip = contexto.Connection.RemoteIpAddress;
        if (ip is null)
            return null;
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        var texto = ip.ToString();
        return texto.Length > 45 ? texto[..45] : texto;
    }
}
