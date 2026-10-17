using System.ComponentModel.DataAnnotations;

namespace PrensTecnologi.Api.Common.Email;

/// <summary>
/// Configuración de la sección "Email". La contraseña nunca va en appsettings:
/// use user-secrets en desarrollo y la variable de entorno Email__Password en producción.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Servidor SMTP. Para Hotmail/Outlook.com: smtp-mail.outlook.com.</summary>
    [Required]
    public string Host { get; init; } = "smtp-mail.outlook.com";

    /// <summary>587 con STARTTLS.</summary>
    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    /// <summary>Cuenta que se autentica y figura como remitente.</summary>
    [Required, EmailAddress]
    public string From { get; init; } = "wprensh@hotmail.com";

    /// <summary>Buzón que recibe los mensajes del formulario de contacto.</summary>
    [Required, EmailAddress]
    public string To { get; init; } = "wprensh@hotmail.com";

    /// <summary>Usuario SMTP; si se deja vacío se usa <see cref="From"/>.</summary>
    public string? UserName { get; init; }

    public string? Password { get; init; }

    /// <summary>
    /// Solo desarrollo: si no hay contraseña, los correos se guardan como .eml en esta carpeta en lugar de enviarse.
    /// </summary>
    public string DevelopmentPickupDirectory { get; init; } = "correos-dev";
}
