using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace FirmaYa.Pages.Invitaciones;

public class InvitarPartesForm(ContratoBLL contratoBLL, NotificacionBLL notificacionBLL) : ContratoPageModel
{
    public const string MensajeErrorEnvio = "No se pudo enviar el correo de invitación. ¿Desea reintentar o copiar el enlace manualmente?";

    public List<ParteContrato> ListaPartesInvitadas { get; private set; } = [];

    [BindProperty] public string? CorreoElectronico { get; set; }
    [BindProperty] public string? NombreParte { get; set; }
    [BindProperty] public string? Rol { get; set; }
    [BindProperty] public string? MensajePersonalizado { get; set; }

    public Dictionary<string, string> Errores { get; private set; } = [];
    public string? ErrorGeneral { get; private set; }

    public ErrorEnvioCorreo? ErrorEnvio { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await Cargar(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostValidarCorreoAsync(string? correoElectronico, CancellationToken ct)
    {
        var resultado = await contratoBLL.ValidarCorreoFormatoYDuplicado(IdContrato, correoElectronico, Usuario, ct);
        return new JsonResult(new
        {
            valido = resultado == ResultadoValidacionCorreo.Valido,
            mensaje = resultado switch
            {
                ResultadoValidacionCorreo.FormatoInvalido => "Ingrese un correo electrónico válido (ej. nombre@dominio.com)",
                ResultadoValidacionCorreo.YaInvitado => "Esta dirección ya ha sido invitada a este contrato",
                _ => null
            }
        });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var resultado = await contratoBLL.InvitarParte(IdContrato, CorreoElectronico, NombreParte, Rol, MensajePersonalizado, Usuario, ct);
        switch (resultado)
        {
            case ParteInvitada invitada:
                TempData["Mensaje"] = $"Invitación enviada exitosamente a {invitada.CorreoElectronico}";
                return RedirectToPage(new { IdContrato });
            case ErrorEnvioCorreo error:
                ErrorEnvio = error;
                break;
            case InvitacionRechazada rechazada when rechazada.Campo == "":
                ErrorGeneral = rechazada.Mensaje;
                break;
            case InvitacionRechazada rechazada:
                Errores[rechazada.Campo] = rechazada.Mensaje;
                break;
        }
        await Cargar(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostReintentarAsync(CancellationToken ct)
    {
        await Cargar(ct);
        var parte = ListaPartesInvitadas.FirstOrDefault(p =>
            string.Equals(p.CorreoInvitado, CorreoElectronico?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (parte is null)
            return RedirectToPage(new { IdContrato });

        var enlaceTokenizado = contratoBLL.GenerarEnlaceAcceso(parte);
        var correoEnviado = await notificacionBLL.EnviarInvitacionPorCorreo(parte.CorreoInvitado!, NombreParte ?? parte.NombreVisible,
            Contrato!.Nombre ?? "", enlaceTokenizado, MensajePersonalizado, IdContrato, ct);
        if (!correoEnviado)
        {
            ErrorEnvio = new ErrorEnvioCorreo(enlaceTokenizado, parte.CorreoInvitado!);
            return Page();
        }

        await contratoBLL.ActualizarEstadoInvitacion(IdContrato, parte.CorreoInvitado!, EstadosInvitacion.InvitacionEnviada, Usuario, ct);
        TempData["Mensaje"] = $"Invitación enviada exitosamente a {parte.CorreoInvitado}";
        return RedirectToPage(new { IdContrato });
    }

    public string EnlaceDe(ParteContrato parte) => contratoBLL.GenerarEnlaceAcceso(parte);

    public string? ErrorDe(string campo) => Errores.GetValueOrDefault(campo);

    private async Task Cargar(CancellationToken ct)
    {
        ListaPartesInvitadas = await contratoBLL.ObtenerPartesInvitadas(IdContrato, Usuario, ct);
        Contrato = await contratoBLL.ObtenerContrato(IdContrato, Usuario, ct);
    }
}
