using InsuranceClaims.API.Models;

namespace InsuranceClaims.API.Middleware;

/// <summary>
/// Outermost middleware: turns any unhandled exception into a uniform JSON error (500)
/// and logs it once at Error level. Exception details are never sent to the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private const int ClientClosedRequest = 499;

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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away - not a server error.
            _logger.LogDebug("Request {Method} {Path} was cancelled by the client (CorrelationId {CorrelationId})",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ClientClosedRequest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path} (CorrelationId {CorrelationId})",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Message = "אירעה שגיאה בלתי צפויה בשרת",
                CorrelationId = context.TraceIdentifier
            });
        }
    }
}
