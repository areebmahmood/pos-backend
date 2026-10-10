namespace Pos.Application.Common.Exceptions;

/// <summary>Base type for expected application errors that map to an HTTP status code.</summary>
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundException(string message) : AppException(message)
{
    public static NotFoundException For(string entity, object key) => new($"{entity} '{key}' was not found.");
}

public sealed class ConflictException(string message) : AppException(message);

public sealed class ForbiddenException(string message = "You are not allowed to perform this action.") : AppException(message);

public sealed class UnauthorizedException(string message = "Invalid credentials.") : AppException(message);

public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : AppException("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
