using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pos.Application.Common.Exceptions;
using Pos.Domain.Exceptions;

namespace Pos.Api.Infrastructure;

/// <summary>Translates application and domain exceptions into RFC 7807 problem details.</summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            DomainException => (StatusCodes.Status400BadRequest, "Request could not be completed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (499, "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // Internal error details are never leaked to clients.
            Detail = status == StatusCodes.Status500InternalServerError ? "Please try again or contact support." : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validation)
            problem.Extensions["errors"] = validation.Errors;

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
