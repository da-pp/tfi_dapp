using FirmaYa.BLL;
using FirmaYa.Data;
using FirmaYa.Models.Entidades;
using FirmaYa.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Login;

public class LoginForm(AccesoBLL accesoBLL) : PageModel
{
    [BindProperty]
    public LoginViewModel Entrada { get; set; } = new();

    public LoginRespuesta? Respuesta { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var respuesta = await IniciarSesion(ct);

        // login.js envía el formulario con fetch y espera JSON; sin JavaScript se renderiza la página.
        if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return new JsonResult(respuesta);

        if (respuesta.Exito)
            return LocalRedirect(respuesta.Redireccion!);

        Respuesta = respuesta;
        return Page();
    }

    public static string? PanelPrincipalSegunRol(string? rol) => rol switch
    {
        Roles.Abogado or Roles.AgenteInmobiliario => "/Dashboard",
        Roles.Administrador => "/Administrador",
        _ => null
    };

    public string? ErrorDe(string campo) =>
        Respuesta?.Errores is { } errores && errores.TryGetValue(campo, out var mensaje) ? mensaje : null;

    private async Task<LoginRespuesta> IniciarSesion(CancellationToken ct)
    {
        var errores = Entrada.Validar();
        if (errores.Count > 0)
            return LoginRespuesta.ConErrores(errores);

        ResultadoAutenticacion resultado;
        try
        {
            resultado = await accesoBLL.Autenticar(Entrada.Correo!.Trim(), Entrada.Contrasena!, DireccionIp(), ct);
        }
        catch (ErrorPersistenciaException)
        {
            return LoginRespuesta.SinConexion();
        }

        switch (resultado)
        {
            case SesionAutenticada sesion:
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    sesion.DatosSesion,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = new DateTimeOffset(sesion.FechaExpiracion, TimeSpan.Zero)
                    });
                TempData["Bienvenida"] = $"Bienvenido, {sesion.NombreUsuario}.";
                return LoginRespuesta.Exitosa(PanelPrincipalSegunRol(sesion.RolUsuario)!);

            case CuentaBloqueada:
                return LoginRespuesta.Bloqueada();

            default:
                return LoginRespuesta.CredencialesInvalidas();
        }
    }

    // auditoria.direccion_ip es VARCHAR(45), el largo máximo de una IPv6 en texto.
    private string? DireccionIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress;
        if (ip is null)
            return null;
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();
        var texto = ip.ToString();
        return texto.Length > 45 ? texto[..45] : texto;
    }
}
