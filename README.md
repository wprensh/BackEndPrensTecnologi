# Prens Tecnologi · Web API

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
dotnet run --project src/PrensTecnologi.Api
```

En desarrollo la API aplica las migraciones al arrancar y publica OpenAPI en `http://localhost:5237/openapi/v1.json`.
Las peticiones de ejemplo están en `src/PrensTecnologi.Api/PrensTecnologi.Api.http`.

## Migraciones

```bash
dotnet ef migrations add <Nombre> --project src/PrensTecnologi.Api --output-dir Database/Migrations
dotnet ef database update --project src/PrensTecnologi.Api
```

En producción, defina la cadena de conexión con la variable de entorno `ConnectionStrings__PrensDb`
y aplique las migraciones desde el pipeline de despliegue (por ejemplo con `dotnet ef migrations bundle`).

## Correo del formulario de contacto

`POST /api/contacto` (slice `Features/Contacto/EnviarMensajeContacto`) envía el mensaje del sitio a **wprensh@hotmail.com**
por SMTP con MailKit. Responder ese correo le escribe directamente al visitante (`Reply-To`).
Tiene CORS solo para los orígenes de `Cors:AllowedOrigins` y un límite de 5 envíos cada 10 minutos por IP.

La contraseña nunca va en `appsettings`:

```bash
# Desarrollo
dotnet user-secrets init --project src/PrensTecnologi.Api
dotnet user-secrets set "Email:Password" "<contraseña de aplicación>" --project src/PrensTecnologi.Api

# Producción
export Email__Password="<contraseña de aplicación>"
```

- Servidor por defecto: `smtp-mail.outlook.com:587` con STARTTLS. Hotmail exige una **contraseña de aplicación**
  (verificación en dos pasos activada en la cuenta Microsoft). Si Microsoft rechaza el inicio de sesión SMTP,
  cambie `Email:Host`, `Email:Port` y `Email:UserName` por los de un proveedor transaccional (Brevo, SendGrid…)
  manteniendo `Email:To` = wprensh@hotmail.com.
- En desarrollo, sin contraseña, los correos se guardan como `.eml` en `src/PrensTecnologi.Api/correos-dev/`.
- Fuera de desarrollo, si falta `Email:Password` la API arranca igual y `POST /api/contacto` responde 503.

## Despliegue en Render (Docker)

El repositorio incluye `Dockerfile` y `render.yaml` (Blueprint):

1. En https://dashboard.render.com elija **New > Blueprint** y conecte este repositorio (rama `main`).
2. Render crea el servicio web `prens-tecnologi-api` (Docker) y la base `prens-tecnologi-db` (PostgreSQL),
   y pasa la cadena de conexión a la API. Las migraciones se aplican al arrancar (`Database__MigrateOnStartup=true`).
3. Cuando pida `Email__Password`, pegue la contraseña de aplicación de wprensh@hotmail.com (se puede dejar vacía y agregarla después en *Environment*).
4. Al terminar, la API queda en `https://prens-tecnologi-api.onrender.com` (compruebe `/health`).

Probar la imagen localmente:

```bash
docker build -t prens-tecnologi-api .
docker run -p 8080:8080 -e ConnectionStrings__PrensDb="postgresql://prens:prens_dev@host.docker.internal:5432/prens_tecnologi" -e Database__MigrateOnStartup=true prens-tecnologi-api
```

Plan gratuito de Render: el servicio se duerme tras 15 minutos sin uso (la primera petición tarda cerca de un minuto)
y la base PostgreSQL gratuita vence a los 30 días.

## Agregar un nuevo slice

1. Cree `Features/<Módulo>/<Funcionalidad>/<Funcionalidad>.cs`.
2. Incluya ahí el request (comando o consulta), su validador, el handler, la respuesta y el endpoint (`ICarterModule`).
3. Si necesita una entidad nueva, agréguela en `Features/<Módulo>/` con su `IEntityTypeConfiguration` y genere la migración.

No hace falta registrar nada en `Program.cs`: Carter, MediatR y FluentValidation descubren los tipos por ensamblado.
