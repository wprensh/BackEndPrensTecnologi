using MimeKit;

namespace PrensTecnologi.Api.Common.Email;

/// <summary>
/// Se usa cuando falta Email:Password fuera de desarrollo: la API arranca igual y el formulario
/// de contacto responde 503 hasta que se configure la contraseña.
/// </summary>
internal sealed class UnconfiguredEmailSender(ILogger<UnconfiguredEmailSender> logger) : IEmailSender
{
    public Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        logger.LogWarning("Correo no enviado: falta Email:Password (variable de entorno Email__Password).");
        throw new EmailDeliveryException("El envío de correo no está configurado.", new InvalidOperationException("Falta Email:Password."));
    }
}
