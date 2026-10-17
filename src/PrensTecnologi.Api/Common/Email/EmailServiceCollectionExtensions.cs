namespace PrensTecnologi.Api.Common.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Registra el envío de correo. Con contraseña configurada usa SMTP; sin ella, en desarrollo
    /// guarda los correos en disco y en cualquier otro entorno responde que el envío no está disponible.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(EmailOptions.SectionName);
        var hasPassword = !string.IsNullOrWhiteSpace(section[nameof(EmailOptions.Password)]);

        services.AddOptions<EmailOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (hasPassword)
        {
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        }
        else if (environment.IsDevelopment())
        {
            services.AddTransient<IEmailSender, PickupDirectoryEmailSender>();
        }
        else
        {
            services.AddTransient<IEmailSender, UnconfiguredEmailSender>();
        }

        return services;
    }
}
