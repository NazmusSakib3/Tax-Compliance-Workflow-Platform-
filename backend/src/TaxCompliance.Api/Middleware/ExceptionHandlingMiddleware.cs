using System.Text.Json;
using TaxCompliance.Application.Common;

namespace TaxCompliance.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (EntityNotFoundException exception)
        {
            await WriteResponseAsync(httpContext, StatusCodes.Status404NotFound, exception.Message);
        }
        catch (AppValidationException exception)
        {
            await WriteResponseAsync(httpContext, StatusCodes.Status400BadRequest, exception.Message, exception.Errors);
        }
        catch (Exception exception)
        {
            var method = SanitizeForLog(httpContext.Request.Method);
            var path = SanitizeForLog(httpContext.Request.Path.ToString());
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", method, path);
            await WriteResponseAsync(httpContext, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static string SanitizeForLog(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, string message, IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            message,
            errors = errors ?? new Dictionary<string, string[]>()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
