using MimeKit;

namespace PrensTecnologi.Api.Common.Email;

/// <summary>Envía un correo ya armado. Lanza <see cref="EmailDeliveryException"/> si el servidor lo rechaza o no responde.</summary>
public interface IEmailSender
{
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
}

public sealed class EmailDeliveryException(string message, Exception innerException) : Exception(message, innerException);
