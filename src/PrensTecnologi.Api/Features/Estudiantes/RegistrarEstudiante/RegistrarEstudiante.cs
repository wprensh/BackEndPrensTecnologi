using Carter;
using PrensTecnologi.Api.Common.Http;
using PrensTecnologi.Api.Common.Results;
using PrensTecnologi.Api.Database;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PrensTecnologi.Api.Features.Estudiantes.RegistrarEstudiante;

// e) Respuesta que recibe el cliente.
public sealed record RegistrarEstudianteResponse(
    Guid Id,
    string Nombres,
    string Apellidos,
    string Email,
    DateOnly FechaNacimiento,
    DateTimeOffset FechaRegistro);

// b) Comando (CQRS: escritura).
public sealed record RegistrarEstudianteCommand(
    string Nombres,
    string Apellidos,
    string Email,
    DateOnly FechaNacimiento) : IRequest<Result<RegistrarEstudianteResponse>>;

// c) Validador: lo ejecuta ValidationBehavior antes del handler.
public sealed class RegistrarEstudianteValidator : AbstractValidator<RegistrarEstudianteCommand>
{
    private const int EdadMinima = 14;

    public RegistrarEstudianteValidator(TimeProvider timeProvider)
    {
        RuleFor(c => c.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(Estudiante.NombresMaxLength);

        RuleFor(c => c.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(Estudiante.ApellidosMaxLength);

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(Estudiante.EmailMaxLength);

        RuleFor(c => c.FechaNacimiento)
            .Must(fecha => fecha <= HoyMenos(timeProvider, EdadMinima))
            .WithMessage($"El estudiante debe tener al menos {EdadMinima} años.")
            .Must(fecha => fecha >= HoyMenos(timeProvider, 120))
            .WithMessage("La fecha de nacimiento no es válida.");
    }

    private static DateOnly HoyMenos(TimeProvider timeProvider, int años) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddYears(-años);
}

// d) Handler: aplica las reglas de negocio y guarda con EF Core.
internal sealed class RegistrarEstudianteHandler(AppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<RegistrarEstudianteCommand, Result<RegistrarEstudianteResponse>>
{
    private const string UniqueViolation = "23505";

    public static readonly Error EmailDuplicado = Error.Conflict(
        "Estudiantes.EmailDuplicado",
        "Ya existe un estudiante registrado con ese correo.");

    public async Task<Result<RegistrarEstudianteResponse>> Handle(
        RegistrarEstudianteCommand command,
        CancellationToken cancellationToken)
    {
        var email = Estudiante.NormalizarEmail(command.Email);

        if (await db.Estudiantes.AnyAsync(e => e.Email == email, cancellationToken))
        {
            return EmailDuplicado;
        }

        var estudiante = Estudiante.Registrar(
            command.Nombres,
            command.Apellidos,
            email,
            command.FechaNacimiento,
            timeProvider.GetUtcNow());

        db.Estudiantes.Add(estudiante);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            // Dos registros simultáneos con el mismo correo: el índice único de PostgreSQL decide.
            return EmailDuplicado;
        }

        return new RegistrarEstudianteResponse(
            estudiante.Id,
            estudiante.Nombres,
            estudiante.Apellidos,
            estudiante.Email,
            estudiante.FechaNacimiento,
            estudiante.FechaRegistro);
    }
}

// a) Endpoint de Carter: solo traduce HTTP <-> comando.
public sealed class RegistrarEstudianteEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/estudiantes", async (RegistrarEstudianteCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);

                return result.Match(
                    estudiante => Results.Created($"/api/estudiantes/{estudiante.Id}", estudiante),
                    error => error.ToProblem());
            })
            .WithName("RegistrarEstudiante")
            .WithTags("Estudiantes")
            .WithSummary("Registra un nuevo estudiante")
            .Produces<RegistrarEstudianteResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
