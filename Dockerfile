# Imagen de la Web API para Render (o cualquier hosting de contenedores).
# Compilar:  docker build -t prens-tecnologi-api .
# Ejecutar:  docker run -p 8080:8080 -e ConnectionStrings__PrensDb="..." prens-tecnologi-api

# 1) Compilación con el SDK de .NET 9
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Primero solo el proyecto, para que la restauración de NuGet quede en caché entre builds.
COPY global.json PrensTecnologi.sln ./
COPY src/PrensTecnologi.Api/PrensTecnologi.Api.csproj src/PrensTecnologi.Api/
RUN dotnet restore src/PrensTecnologi.Api/PrensTecnologi.Api.csproj

COPY src/ src/
RUN dotnet publish src/PrensTecnologi.Api/PrensTecnologi.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# 2) Ejecución con el runtime de ASP.NET Core 9 (imagen más liviana, usuario sin privilegios)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PrensTecnologi.Api.dll"]
