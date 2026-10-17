using System.Threading.RateLimiting;

namespace PrensTecnologi.Api.Common.Http;

public static class CorsPolicies
{
    /// <summary>Orígenes del sitio Angular, leídos de Cors:AllowedOrigins.</summary>
    public const string Frontend = "Frontend";
}

public static class RateLimitPolicies
{
    /// <summary>Límite por IP para el formulario de contacto: evita que se use para enviar correo masivo.</summary>
    public const string Contacto = "Contacto";
}

public static class WebPolicyServiceCollectionExtensions
{
    public static IServiceCollection AddWebPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(cors => cors.AddPolicy(CorsPolicies.Frontend, policy => policy
            .WithOrigins(origins)
            .WithMethods("POST")
            .WithHeaders("Content-Type")));

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(RateLimitPolicies.Contacto, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) }));
        });

        return services;
    }
}
