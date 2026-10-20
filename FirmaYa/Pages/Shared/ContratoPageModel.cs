using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Shared;

/// <summary>Base de los formularios que trabajan sobre un contrato (cabecera CONTRATO / ESTADO / VERSIÓN / HASH).</summary>
public abstract class ContratoPageModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int IdContrato { get; set; }

    /// <summary>datosContrato que muestra la cabecera.</summary>
    public Contrato? Contrato { get; protected set; }

    /// <summary>Usuario interno autenticado (cookie del CU-19).</summary>
    protected UsuarioActual Usuario => UsuarioActual.Desde(User) ?? throw new AccesoDenegadoException();

    public bool EsTitular => Contrato?.EsTitular(Usuario.IdUsuario) == true;

    /// <summary>Dirección IP del cliente, recortada a VARCHAR(45).</summary>
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
