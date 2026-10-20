using FirmaYa.BLL;
using FirmaYa.Models.Entidades;
using FirmaYa.Pages.Shared;
using FirmaYa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FirmaYa.Pages.Firmas;

public class FirmarContratoForm(AccesoBLL accesoBLL, ContratoBLL contratoBLL, HoraLocal hora) : PageModel
{
    public const string MensajeTokenInvalido =
        "El enlace de firma no es válido o ha expirado. Solicite un nuevo enlace al dueño del contrato.";
    public const string MensajeBloqueado = "El enlace de firma ha sido bloqueado por seguridad. Contacte al dueño del contrato.";
    public const string MensajeExito = "Su firma ha sido registrada exitosamente. Puede descargar el contrato firmado.";

    public enum Pasos
    {
        Lectura,
        CodigoOtp,
        Firmado,
        Error
    }

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = "";

    [BindProperty]
    public string? CodigoOTP { get; set; }

    public ParteContrato? Firmante { get; private set; }

    public Contrato? DatosContrato { get; private set; }

    public Pasos Paso { get; private set; } = Pasos.Lectura;
    public string? Mensaje { get; private set; }
    public bool OtpExpirado { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await Cargar(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostEnviarOtpAsync(CancellationToken ct)
    {
        if (!await Cargar(ct))
            return Page();

        if (await accesoBLL.GenerarYEnviarOTP(new TokenEnlace(Token), ct))
        {
            Paso = Pasos.CodigoOtp;
        }
        else
        {
            Mensaje = "No se pudo enviar el código. Intente nuevamente en unos minutos.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostValidarOtpAsync(CancellationToken ct)
    {
        if (!await Cargar(ct))
            return Page();

        var token = new TokenEnlace(Token);
        Paso = Pasos.CodigoOtp;
        var validacion = await accesoBLL.ValidarOTP(token, CodigoOTP?.Trim(), ct);
        switch (validacion.Resultado)
        {
            case ResultadoValidacionOTP.FormatoInvalido:
                Mensaje = "El código debe tener exactamente 6 dígitos numéricos.";
                break;
            case ResultadoValidacionOTP.Incorrecto:
                Mensaje = $"El código ingresado es incorrecto. Verifique el código e intente nuevamente. Intentos restantes: {validacion.IntentosRestantes}.";
                break;
            case ResultadoValidacionOTP.Expirado:
                OtpExpirado = true;
                Mensaje = "El código ha expirado. ¿Desea recibir un nuevo código?";
                break;
            case ResultadoValidacionOTP.EnlaceBloqueado:
                Paso = Pasos.Error;
                Mensaje = MensajeBloqueado;
                break;
            case ResultadoValidacionOTP.Correcto:
                await contratoBLL.RegistrarFirma(DatosContrato!.IdContrato, Firmante!, hora.AhoraUtc,
                    ContratoPageModel.DireccionIpDe(HttpContext), DatosContrato.IdVersion!.Value, DatosContrato.HashVersion!, ct);
                Paso = Pasos.Firmado;
                Mensaje = MensajeExito;
                break;
        }
        return Page();
    }

    private async Task<bool> Cargar(CancellationToken ct)
    {
        var token = new TokenEnlace(Token);
        Firmante = await accesoBLL.ValidarTokenFirma(token, ct);
        if (Firmante is null)
        {
            // Distingue el enlace bloqueado por intentos fallidos del enlace inválido.
            var parte = await accesoBLL.ObtenerTokenAcceso(token, ct);
            Paso = Pasos.Error;
            Mensaje = parte?.EstadoInvitacion == EstadosInvitacion.Bloqueado ? MensajeBloqueado : MensajeTokenInvalido;
            return false;
        }

        DatosContrato = await contratoBLL.ObtenerDatosParaFirma(token, ct);
        if (Firmante.EstadoFirma == EstadosFirma.Firmado)
        {
            Paso = Pasos.Firmado;
            Mensaje = "Su firma ya fue registrada para esta versión del contrato.";
            return false;
        }
        if (DatosContrato is null || DatosContrato.Estado != EstadosContrato.ListoParaFirmar || DatosContrato.IdVersion is null)
        {
            Paso = Pasos.Error;
            Mensaje = "El contrato no está disponible para firma.";
            return false;
        }
        return true;
    }
}
