using System.Security.Claims;
using FirmaYa.Pages.Login;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages;

/// <summary>Raíz del sitio: envía al panel del rol autenticado o a LoginForm.</summary>
public class IndexModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToPage("/Login/LoginForm");

        var panel = LoginForm.PanelPrincipalSegunRol(User.FindFirstValue(ClaimTypes.Role));
        if (panel is not null)
            return LocalRedirect(panel);

        await HttpContext.SignOutAsync();
        return RedirectToPage("/Login/LoginForm");
    }
}
