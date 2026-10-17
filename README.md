# Cursos en Línea · Web API

Web API en .NET 9 y C# 13 con **arquitectura vertical** (Vertical Slice Architecture): cada funcionalidad vive completa en su carpeta bajo `Features/`.

- **Carter** (Minimal APIs) para los endpoints de cada slice.
- **MediatR** (CQRS) para comandos, consultas y handlers.
- **FluentValidation** ejecutado como `IPipelineBehavior` antes de cada handler.
- **EF Core 9 + PostgreSQL** (Npgsql), Code-First con migraciones y nombres en snake_case.
- Errores con **patrón Result** y respuestas **ProblemDetails** (RFC 9457).

## Requisitos

- SDK de .NET 9
- PostgreSQL 15 o superior (o Docker)

## Cómo ejecutarlo

```bash
docker compose up -d                 # PostgreSQL en localhost:5432
dotnet tool restore                  # instala dotnet-ef 9
dotnet run --project src/CursosEnLinea.Api
```

En desarrollo la API aplica las migraciones al arrancar y publica OpenAPI en `http://localhost:5237/openapi/v1.json`.
Las peticiones de ejemplo están en `src/CursosEnLinea.Api/CursosEnLinea.Api.http`.

## Migraciones

```bash
dotnet ef migrations add <Nombre> --project src/CursosEnLinea.Api --output-dir Database/Migrations
dotnet ef database update --project src/CursosEnLinea.Api
```

En producción, defina la cadena de conexión con la variable de entorno `ConnectionStrings__CursosDb`
y aplique las migraciones desde el pipeline de despliegue (por ejemplo con `dotnet ef migrations bundle`).

## Agregar un nuevo slice

1. Cree `Features/<Módulo>/<Funcionalidad>/<Funcionalidad>.cs`.
2. Incluya ahí el request (comando o consulta), su validador, el handler, la respuesta y el endpoint (`ICarterModule`).
3. Si necesita una entidad nueva, agréguela en `Features/<Módulo>/` con su `IEntityTypeConfiguration` y genere la migración.

No hace falta registrar nada en `Program.cs`: Carter, MediatR y FluentValidation descubren los tipos por ensamblado.
