using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Codigo { get; set; }

    public void OnGet()
    {
    }

    // UseStatusCodePagesWithReExecute reejecuta con el mismo método HTTP (p. ej. un POST rechazado).
    public void OnPost()
    {
    }
}
