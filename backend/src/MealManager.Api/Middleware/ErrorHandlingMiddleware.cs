using System.Diagnostics;
using FluentValidation;
using MealManager.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Middleware;

/// <summary>Maps exceptions to the ApiErrorResponse contract (docs/API_CONVENTIONS.md §4).</summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, code, title, detail, errors) = exception switch
            {
                AppException app => (app.StatusCode, app.ErrorCode, app.ErrorCode, app.Message, app.Errors),
                ValidationException validation => (400, "VALIDATION_ERROR", "One or more validation failures occurred",
                    "Invalid request.", validation.Errors
                        .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => CharToLowerFirst(g.Key), g => g.Select(f => f.ErrorMessage).ToArray())),
                _ => (500, "INTERNAL_ERROR", "INTERNAL_ERROR", "An unexpected error occurred.", null),
            };

            if (status == 500)
            {
                logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            var body = new Dictionary<string, object?>
            {
                ["type"] = code,
                ["title"] = title,
                ["status"] = status,
                ["detail"] = detail,
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
            };
            if (errors is { Count: > 0 })
            {
                body["errors"] = errors;
            }

            await context.Response.WriteAsJsonAsync(body);
        }
    }

    private static string CharToLowerFirst(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
