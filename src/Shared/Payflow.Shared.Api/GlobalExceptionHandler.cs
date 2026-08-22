using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Payflow.Shared.Api;

/// <summary>
/// Last-resort handler for exceptions that escape an endpoint unconverted (a downstream service
/// being unreachable, an unmapped bug). Business failures should already have been turned into a
/// <c>Result</c> and never reach here — see <see cref="ResultExtensions"/>. Registered per service
/// via <c>app.UseExceptionHandler()</c> alongside <c>services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c>.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            title = "An unexpected error occurred upstream.",
            status = StatusCodes.Status502BadGateway,
            traceId = httpContext.TraceIdentifier
        }, cancellationToken);

        return true;
    }
}
