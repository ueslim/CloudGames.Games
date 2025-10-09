using System.Net;
using System.Text.Json;
using CloudGames.Games.Infrastructure.Metrics;

namespace CloudGames.Games.Api.Middleware;

/// <summary>
/// Simple global exception handler that logs exceptions and returns standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception with structured logging
        _logger.LogError(exception,
            "Unhandled exception in {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        // Métricas de erro
        ApplicationMetrics.Errors.WithLabels(exception.GetType().Name).Inc();

        // Return a simple error response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var errorResponse = new
        {
            erro = "Ocorreu um erro ao processar sua requisição",
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}

