using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Registers the global exception handler middleware.
    /// Call this before UseAuthentication() in Program.cs.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
