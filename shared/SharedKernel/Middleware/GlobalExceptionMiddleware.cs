using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedKernel.Middleware;

/// <summary>
/// Catches any unhandled exception that escapes controllers/handlers and returns
/// a consistent JSON error shape instead of leaking stack traces to the client.
/// Register with app.UseGlobalExceptionHandler() before UseAuthentication().
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            ctx.Response.StatusCode = 500;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                traceId = ctx.TraceIdentifier
            });
        }
    }
}
