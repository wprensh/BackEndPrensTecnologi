using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace PrensTecnologi.Api.Common.Email;

/// <summary>Envío por SMTP con MailKit (STARTTLS en el puerto 587).</summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new SmtpClient { Timeout = (int)Timeout.TotalMilliseconds };

        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTls, cancellationToken);
            await client.AuthenticateAsync(settings.UserName ?? settings.From, settings.Password ?? string.Empty, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpCommandException or SmtpProtocolException or AuthenticationException
                                       or ServiceNotConnectedException or IOException or TimeoutException
                                       or System.Net.Sockets.SocketException)
        {
            logger.LogError(ex, "No se pudo enviar el correo por {Host}:{Port}", settings.Host, settings.Port);
            throw new EmailDeliveryException("El servidor de correo no aceptó el mensaje.", ex);
        }
    }
}
