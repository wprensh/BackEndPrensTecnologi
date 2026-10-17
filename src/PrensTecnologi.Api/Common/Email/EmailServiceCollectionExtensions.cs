namespace PrensTecnologi.Api.Common.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Registra el envío de correo. Con contraseña configurada usa SMTP; sin ella, en desarrollo
    /// guarda los correos en disco y en cualquier otro entorno falla al arrancar.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(EmailOptions.SectionName);
        var hasPassword = !string.IsNullOrWhiteSpace(section[nameof(EmailOptions.Password)]);

        services.AddOptions<EmailOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(
                options => environment.IsDevelopment() || !string.IsNullOrWhiteSpace(options.Password),
                "Falta Email:Password. Defínala con la variable de entorno Email__Password.")
            .ValidateOnStart();

        if (hasPassword || !environment.IsDevelopment())
        {
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddTransient<IEmailSender, PickupDirectoryEmailSender>();
        }

        return services;
    }
}
