using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace FirmaYa.Services;

public sealed class CorreoOptions
{
    /// <summary>Brevo: smtp-relay.brevo.com</summary>
    public string? Host { get; set; }
    public int Puerto { get; set; } = 587;
    /// <summary>Login SMTP de Brevo. Guardar con user-secrets o variables de entorno.</summary>
    public string? Usuario { get; set; }
    /// <summary>Clave SMTP de Brevo. Guardar con user-secrets o variables de entorno.</summary>
    public string? Contrasena { get; set; }
    public string? Remitente { get; set; }
    public string NombreRemitente { get; set; } = "FirmaYa";

    /// <summary>
    /// Solo desarrollo: si se define, los correos no se envían y se guardan como .eml en esta carpeta
    /// (relativa a la raíz del proyecto). Así se puede leer el código OTP sin escribirlo en los logs.
    /// </summary>
    public string? DirectorioPrueba { get; set; }
}

/// <summary>Envío de correo por SMTP (Brevo). Lo usa NotificacionBLL.</summary>
public sealed class CorreoService(IOptions<CorreoOptions> opciones, IHostEnvironment entorno, ILogger<CorreoService> logger)
{
    /// <summary>Devuelve false si el correo no pudo enviarse; el motivo queda en el log, sin el contenido del mensaje.</summary>
    public async Task<bool> EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken ct)
    {
        var configuracion = opciones.Value;
        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(configuracion.Remitente ?? "no-responder@firmaya.local", configuracion.NombreRemitente),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            mensaje.To.Add(new MailAddress(destinatario));

            using var cliente = CrearCliente(configuracion);
            if (cliente is null)
            {
                logger.LogWarning("Correo no configurado (Correo:Host o Correo:DirectorioPrueba). No se envió el mensaje '{Asunto}'.", asunto);
                return false;
            }

            await cliente.SendMailAsync(mensaje, ct);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException or IOException)
        {
            logger.LogWarning(ex, "No se pudo enviar el correo '{Asunto}'.", asunto);
            return false;
        }
    }

    private SmtpClient? CrearCliente(CorreoOptions configuracion)
    {
        if (!string.IsNullOrWhiteSpace(configuracion.DirectorioPrueba))
        {
            var directorio = Path.Combine(entorno.ContentRootPath, configuracion.DirectorioPrueba);
            Directory.CreateDirectory(directorio);
            return new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = directorio
            };
        }

        if (string.IsNullOrWhiteSpace(configuracion.Host))
            return null;

        return new SmtpClient(configuracion.Host, configuracion.Puerto)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(configuracion.Usuario, configuracion.Contrasena)
        };
    }
}
