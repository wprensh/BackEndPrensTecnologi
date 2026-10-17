using Microsoft.Extensions.Options;
using MimeKit;

namespace PrensTecnologi.Api.Common.Email;

/// <summary>
/// Solo desarrollo: guarda cada correo como archivo .eml (se abre con Outlook o Thunderbird)
/// para probar el formulario sin credenciales SMTP.
/// </summary>
internal sealed class PickupDirectoryEmailSender(
    IOptions<EmailOptions> options,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    ILogger<PickupDirectoryEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(environment.ContentRootPath, options.Value.DevelopmentPickupDirectory);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{timeProvider.GetUtcNow():yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.eml");
        await message.WriteToAsync(path, cancellationToken);

        logger.LogInformation("Correo guardado en {Path} (desarrollo, sin SMTP configurado)", path);
    }
}
