using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CursosEnLinea.Api.Common.Http;

/// <summary>
/// Último punto de control de errores: los fallos de validación y los cuerpos mal formados se responden
/// como 400, y cualquier otra excepción como 500 sin exponer detalles internos.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(
                validation.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation.Failed",
                Detail = "Uno o más campos no son válidos.",
            },
            BadHttpRequestException badRequest => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Request.Invalid",
                Detail = "El cuerpo de la solicitud no es un JSON válido para esta operación.",
            },
            _ => new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server.Error",
                Detail = "Ocurrió un error inesperado. Intente de nuevo más tarde.",
            },
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado en {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
