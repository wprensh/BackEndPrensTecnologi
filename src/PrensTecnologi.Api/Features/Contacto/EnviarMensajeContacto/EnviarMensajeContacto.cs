using System.Text;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using MimeKit;
using PrensTecnologi.Api.Common.Email;
using PrensTecnologi.Api.Common.Http;
using PrensTecnologi.Api.Common.Results;

namespace PrensTecnologi.Api.Features.Contacto.EnviarMensajeContacto;

// e) Respuesta que recibe el cliente.
public sealed record EnviarMensajeContactoResponse(DateTimeOffset FechaEnvio);

// b) Comando (CQRS: escritura). Los campos coinciden con el formulario de contacto del sitio.
public sealed record EnviarMensajeContactoCommand(
    string Nombre,
    string Correo,
    string? Empresa,
    string? Telefono,
    string Interes,
    string Mensaje) : IRequest<Result<EnviarMensajeContactoResponse>>;

// c) Validador: mismas reglas que el formulario, para no depender solo del navegador.
public sealed class EnviarMensajeContactoValidator : AbstractValidator<EnviarMensajeContactoCommand>
{
    public EnviarMensajeContactoValidator()
    {
        RuleFor(c => c.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100);

        RuleFor(c => c.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .MaximumLength(254)
            .Matches(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$").WithMessage("El correo no tiene un formato válido.");

        RuleFor(c => c.Empresa).MaximumLength(100);

        RuleFor(c => c.Telefono)
            .Matches(@"^[+\d][\d\s-]{6,19}$").WithMessage("El teléfono no tiene un formato válido.")
            .When(c => !string.IsNullOrWhiteSpace(c.Telefono));

        RuleFor(c => c.Interes)
            .NotEmpty().WithMessage("Elija el servicio de interés.")
            .MaximumLength(60);

        RuleFor(c => c.Mensaje)
            .NotEmpty().WithMessage("El mensaje es obligatorio.")
            .MinimumLength(20).WithMessage("El mensaje debe tener al menos 20 caracteres.")
            .MaximumLength(1500);
    }
}

// d) Handler: arma el correo y lo entrega a wprensh@hotmail.com (configurable en Email:To).
internal sealed class EnviarMensajeContactoHandler(
    IEmailSender emailSender,
    IOptions<EmailOptions> options,
    TimeProvider timeProvider)
    : IRequestHandler<EnviarMensajeContactoCommand, Result<EnviarMensajeContactoResponse>>
{
    public static readonly Error EnvioNoDisponible = Error.Unavailable(
        "Contacto.EnvioNoDisponible",
        "No pudimos enviar su mensaje en este momento. Intente de nuevo en unos minutos.");

    public async Task<Result<EnviarMensajeContactoResponse>> Handle(
        EnviarMensajeContactoCommand command,
        CancellationToken cancellationToken)
    {
        var message = CrearCorreo(command, options.Value);

        try
        {
            await emailSender.SendAsync(message, cancellationToken);
        }
        catch (EmailDeliveryException)
        {
            return EnvioNoDisponible;
        }

        return new EnviarMensajeContactoResponse(timeProvider.GetUtcNow());
    }

    private static MimeMessage CrearCorreo(EnviarMensajeContactoCommand command, EmailOptions settings)
    {
        var nombre = command.Nombre.Trim();
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sitio web Prens Tecnologi", settings.From));
        message.To.Add(MailboxAddress.Parse(settings.To));
        // Responder al correo le escribe directamente al visitante.
        message.ReplyTo.Add(new MailboxAddress(nombre, command.Correo.Trim()));
        message.Subject = $"Contacto web: {command.Interes.Trim()} · {nombre}";

        // Texto plano: el contenido del visitante nunca se interpreta como HTML.
        var body = new StringBuilder()
            .AppendLine($"Nombre: {nombre}")
            .AppendLine($"Correo: {command.Correo.Trim()}");
        if (!string.IsNullOrWhiteSpace(command.Empresa)) body.AppendLine($"Empresa: {command.Empresa.Trim()}");
        if (!string.IsNullOrWhiteSpace(command.Telefono)) body.AppendLine($"Teléfono: {command.Telefono.Trim()}");
        body.AppendLine($"Servicio de interés: {command.Interes.Trim()}")
            .AppendLine()
            .AppendLine(command.Mensaje.Trim());

        message.Body = new TextPart("plain") { Text = body.ToString() };
        return message;
    }
}

// a) Endpoint de Carter: solo traduce HTTP <-> comando.
public sealed class EnviarMensajeContactoEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/contacto", async (EnviarMensajeContactoCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);

                return result.Match(
                    respuesta => Results.Accepted(value: respuesta),
                    error => error.ToProblem());
            })
            .RequireCors(CorsPolicies.Frontend)
            .RequireRateLimiting(RateLimitPolicies.Contacto)
            .WithName("EnviarMensajeContacto")
            .WithTags("Contacto")
            .WithSummary("Envía el mensaje del formulario de contacto al correo de Prens Tecnologi")
            .Produces<EnviarMensajeContactoResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
