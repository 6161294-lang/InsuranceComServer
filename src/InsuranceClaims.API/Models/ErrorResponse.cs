namespace InsuranceClaims.API.Models;

/// <summary>Uniform error body returned by every failing request.</summary>
public class ErrorResponse
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }

    /// <summary>Field-level validation errors (400 only).</summary>
    public IDictionary<string, string[]>? Errors { get; set; }
}
