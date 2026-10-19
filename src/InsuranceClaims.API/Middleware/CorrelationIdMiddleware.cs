using System.Text.RegularExpressions;

namespace InsuranceClaims.API.Middleware;

/// <summary>
/// Gives every request a CorrelationId: taken from the X-Correlation-Id header when it is valid,
/// otherwise generated. It is stored in HttpContext.TraceIdentifier, echoed back in the response header,
/// and pushed into the logging scope so every log line of the request carries it.
/// </summary>
public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = incoming is not null && ValidCorrelationId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }

    // Accept only short, safe ids from clients (prevents log injection).
    [GeneratedRegex("^[a-zA-Z0-9-]{1,64}$")]
    private static partial Regex ValidCorrelationId();
}
