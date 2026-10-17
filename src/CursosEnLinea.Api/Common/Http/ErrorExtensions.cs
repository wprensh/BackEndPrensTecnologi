using CursosEnLinea.Api.Common.Results;

namespace CursosEnLinea.Api.Common.Http;

public static class ErrorExtensions
{
    /// <summary>Convierte un <see cref="Error"/> de negocio en una respuesta ProblemDetails (RFC 9457).</summary>
    public static IResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return TypedResults.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
