using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CursosEnLinea.Api.Features.Estudiantes;

public sealed class Estudiante
{
    public const int NombresMaxLength = 100;
    public const int ApellidosMaxLength = 100;
    public const int EmailMaxLength = 254;

    public Guid Id { get; private set; }
    public string Nombres { get; private set; } = string.Empty;
    public string Apellidos { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public DateOnly FechaNacimiento { get; private set; }
    public DateTimeOffset FechaRegistro { get; private set; }

    private Estudiante()
    {
        // Requerido por EF Core.
    }

    public static Estudiante Registrar(
        string nombres,
        string apellidos,
        string email,
        DateOnly fechaNacimiento,
        DateTimeOffset fechaRegistro) => new()
    {
        Id = Guid.CreateVersion7(fechaRegistro),
        Nombres = nombres.Trim(),
        Apellidos = apellidos.Trim(),
        Email = NormalizarEmail(email),
        FechaNacimiento = fechaNacimiento,
        FechaRegistro = fechaRegistro,
    };

    public static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}

internal sealed class EstudianteConfiguration : IEntityTypeConfiguration<Estudiante>
{
    public void Configure(EntityTypeBuilder<Estudiante> builder)
    {
        builder.ToTable("estudiantes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Nombres).HasMaxLength(Estudiante.NombresMaxLength).IsRequired();
        builder.Property(e => e.Apellidos).HasMaxLength(Estudiante.ApellidosMaxLength).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(Estudiante.EmailMaxLength).IsRequired();
        builder.Property(e => e.FechaNacimiento).IsRequired();
        builder.Property(e => e.FechaRegistro).IsRequired();

        builder.HasIndex(e => e.Email).IsUnique();
    }
}
