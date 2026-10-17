namespace PrensTecnologi.Api.Common.Results;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unavailable,
}

/// <summary>Error de negocio esperado. Las fallas inesperadas viajan como excepciones al manejador global.</summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    public static Error Unavailable(string code, string description) => new(code, description, ErrorType.Unavailable);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
}
