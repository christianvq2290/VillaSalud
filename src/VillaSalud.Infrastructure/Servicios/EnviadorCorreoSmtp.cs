using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using VillaSalud.Application.Abstracciones;

namespace VillaSalud.Infrastructure.Servicios;

/// <summary>Sección "Smtp" de la configuración. Si Host está vacío, el envío de correos queda desactivado.</summary>
public class OpcionesSmtp
{
    public string? Host { get; set; }
    public int Puerto { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Clave { get; set; }
    public string Remitente { get; set; } = "citas@villasalud.pe";
    public string NombreRemitente { get; set; } = "Clínica Villa Salud";
}

public class EnviadorCorreoSmtp : IEnviadorCorreo
{
    private readonly OpcionesSmtp _opciones;

    public EnviadorCorreoSmtp(IOptions<OpcionesSmtp> opciones) => _opciones = opciones.Value;

    public bool Configurado => !string.IsNullOrWhiteSpace(_opciones.Host);

    public async Task EnviarAsync(string destino, string asunto, string cuerpo, CancellationToken ct = default)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(_opciones.NombreRemitente, _opciones.Remitente));
        mensaje.To.Add(MailboxAddress.Parse(destino));
        mensaje.Subject = asunto;
        mensaje.Body = new TextPart("plain") { Text = cuerpo };

        using var cliente = new SmtpClient();
        await cliente.ConnectAsync(_opciones.Host!, _opciones.Puerto, SecureSocketOptions.StartTlsWhenAvailable, ct);
        if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
            await cliente.AuthenticateAsync(_opciones.Usuario, _opciones.Clave ?? "", ct);
        await cliente.SendAsync(mensaje, ct);
        await cliente.DisconnectAsync(true, ct);
    }
}
