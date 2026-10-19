namespace InsuranceClaims.Core.Common;

/// <summary>
/// Thrown by the data layer when an optimistic-concurrency check fails
/// (another request changed the row since it was read). Keeps the Service layer
/// independent of EF Core while still reacting only to concurrency failures.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
