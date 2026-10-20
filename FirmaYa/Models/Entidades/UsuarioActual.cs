using System.Security.Claims;

namespace FirmaYa.Models.Entidades;

/// <summary>Usuario interno autenticado (cookie del CU-19). Es el "usuario" / "autor" de los diagramas.</summary>
public sealed record UsuarioActual(int IdUsuario, string NombreCompleto, string Rol)
{
    public static UsuarioActual? Desde(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? new UsuarioActual(id, principal.FindFirstValue(ClaimTypes.Name) ?? "", principal.FindFirstValue(ClaimTypes.Role) ?? "")
            : null;
}
