using System.Security.Claims;

namespace FirmaYa.Models.Entidades;

public sealed record UsuarioActual(int IdUsuario, string NombreCompleto, string Rol)
{
    public static UsuarioActual? Desde(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? new UsuarioActual(id, principal.FindFirstValue(ClaimTypes.Name) ?? "", principal.FindFirstValue(ClaimTypes.Role) ?? "")
            : null;
}
