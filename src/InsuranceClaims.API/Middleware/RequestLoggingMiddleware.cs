using System.Diagnostics;

namespace InsuranceClaims.API.Middleware;

/// <summary>
/// Logs one Information line per completed request (method, path, status, duration).
/// Runs inside the CorrelationId scope. Bodies, query strings and headers are never logged,
/// so passwords, tokens and registration data stay out of the log.
/// Failed requests are logged by <see cref="ExceptionHandlingMiddleware"/>.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (CorrelationId {CorrelationId})",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            context.TraceIdentifier);
    }
}
