namespace MealManager.Application.Exceptions;

/// <summary>Carries the API_CONVENTIONS error code + HTTP status; translated to ApiErrorResponse by the API middleware.</summary>
public class AppException(
    int statusCode,
    string errorCode,
    string message,
    IDictionary<string, string[]>? errors = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public string ErrorCode { get; } = errorCode;

    public IDictionary<string, string[]>? Errors { get; } = errors;
}

public sealed class NotFoundException(string message) : AppException(404, "NOT_FOUND", message);

public sealed class ForbiddenException(string message = "You do not have access to this resource.")
    : AppException(403, "FORBIDDEN", message);

public sealed class ConflictException(string message) : AppException(409, "CONFLICT", message);

public sealed class UnauthorizedException(string message = "Authentication required.")
    : AppException(401, "UNAUTHENTICATED", message);
