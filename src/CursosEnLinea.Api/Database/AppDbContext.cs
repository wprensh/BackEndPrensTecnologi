using Microsoft.EntityFrameworkCore;

namespace CursosEnLinea.Api.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Features.Estudiantes.Estudiante> Estudiantes => Set<Features.Estudiantes.Estudiante>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cada slice aporta la configuración de sus entidades (IEntityTypeConfiguration) desde su propia carpeta.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
