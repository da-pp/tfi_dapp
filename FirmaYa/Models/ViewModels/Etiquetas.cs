using FirmaYa.Models.Entidades;

namespace FirmaYa.Models.ViewModels;

public static class Etiquetas
{
    public static string Clase(string? estado) => estado switch
    {
        EstadosContrato.Firmado or EstadosFirma.Firmado or EstadosInvitacion.InvitacionEnviada or EstadosUsuario.Activo
            => "etiqueta etiqueta--exito",
        EstadosContrato.EnRevision or EstadosContrato.ListoParaFirmar or EstadosFirma.Pendiente or EstadosFirma.Notificado
            or EstadosFirma.ReNotificado
            => "etiqueta etiqueta--alerta",
        EstadosInvitacion.Bloqueado or EstadosUsuario.Inactivo
            => "etiqueta etiqueta--peligro",
        _ => "etiqueta"
    };

    public static string HashCorto(string? hash) =>
        string.IsNullOrEmpty(hash) ? "—" : hash.Length <= 32 ? hash : hash[..32] + "…";
}

public sealed record PestanasContrato(int IdContrato, string Activa, bool EsTitular = true);
