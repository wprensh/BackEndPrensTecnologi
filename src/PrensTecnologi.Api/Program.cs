using Carter;
using PrensTecnologi.Api.Common.Behaviors;
using PrensTecnologi.Api.Common.Email;
using PrensTecnologi.Api.Common.Http;
using PrensTecnologi.Api.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var assembly = typeof(Program).Assembly;

// Base de datos: PostgreSQL con EF Core 9 (Code-First), tablas y columnas en snake_case.
// La cadena de conexión vive en ConnectionStrings:PrensDb.
builder.Services.AddDbContext<AppDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("PrensDb"))
        .UseSnakeCaseNamingConvention());

// CQRS: MediatR descubre los handlers de todos los slices y aplica la validación como pipeline.
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

builder.Services.AddCarter();

// Correo saliente (SMTP de Hotmail) y protección del formulario de contacto: CORS para el sitio y límite por IP.
builder.Services.AddEmail(builder.Configuration, builder.Environment);
builder.Services.AddWebPolicies(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Solo en desarrollo: crea o actualiza la base al arrancar. En producción, aplique las migraciones en el pipeline de despliegue.
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.MapCarter();

app.Run();

// Permite usar WebApplicationFactory<Program> en pruebas de integración.
public partial class Program;
